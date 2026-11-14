using System.Collections.Immutable;
using System.Threading.Channels;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Devices;

namespace Crossdeck.Application.Devices;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly Channel<DeviceChange> _deviceChanges = Channel.CreateUnbounded<DeviceChange>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly IInputSink _inputSink;
    private readonly IEnumerable<IDeviceWatcher> _deviceWatchers;
    private readonly IReadOnlyCollection<IDeviceDriverFactory> _deviceDriverFactories;
    private readonly Dictionary<DeviceIdentity, RunningDriver> _deviceDrivers = new();

    private volatile ImmutableDictionary<DeviceIdentity, IDeviceDriver> _snapshot =
        ImmutableDictionary<DeviceIdentity, IDeviceDriver>.Empty;

    public DeviceRegistry(
        IEnumerable<IDeviceWatcher> deviceWatchers,
        IEnumerable<IDeviceDriverFactory> deviceDriverFactories,
        IInputSink inputSink)
    {
        _deviceWatchers = deviceWatchers ?? throw new ArgumentNullException(nameof(deviceWatchers));
        _deviceDriverFactories = [.. deviceDriverFactories.OrderByDescending(x => x.Priority)];
        _inputSink = inputSink ?? throw new ArgumentNullException(nameof(inputSink));
    }

    public event EventHandler<DriverAddedEventArgs>? DriverAdded;

    public event EventHandler<DriverRemovedEventArgs>? DriverRemoved;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var supervisorTasks = _deviceWatchers.Select(w => SuperviseAsync(w, cancellationToken)).ToList();

        try
        {
            await foreach (var change in _deviceChanges.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var task = change switch
                    {
                        DeviceChange.Arrived a => StartAsync(a.Descriptor, cancellationToken),
                        DeviceChange.Removed r => StopAsync(r.Identity)
                    };

                    await task;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // TODO Logging
                }
            }
        }
        finally
        {
            foreach (var id in _deviceDrivers.Keys.ToList()) await StopAsync(id);
            await Task.WhenAll(supervisorTasks);
        }
    }

    public bool TryGet(DeviceIdentity identity, out IDeviceDriver? driver)
    {
        driver = null;

        return _snapshot is { } s
               && s.TryGetValue(identity, out driver);
    }

    private async Task StartAsync(DeviceDescriptor descriptor, CancellationToken cancellationToken)
    {
        var factory = _deviceDriverFactories.FirstOrDefault(f => f.CanCreate(descriptor));
        if (factory is null)
            return;

        var driver = await factory.CreateAsync(descriptor, cancellationToken);
        if (driver is null)
            return;

        var deviceCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var task = Task.Factory.StartNew(() => driver.RunAsync(deviceCancellationToken.Token),
            deviceCancellationToken.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

        _deviceDrivers.Add(driver.Identity, new RunningDriver(deviceCancellationToken, driver, task));
        _snapshot = _snapshot.SetItem(driver.Identity, driver);
        driver.InputReceived += DriverOnInputReceived;

        DriverAdded?.Invoke(this, new DriverAddedEventArgs(driver.Identity, driver));
    }

    private async Task StopAsync(DeviceIdentity identity)
    {
        if (!_deviceDrivers.Remove(identity, out var runningDriver))
            return;

        _snapshot = _snapshot.Remove(identity);
        DriverRemoved?.Invoke(this, new DriverRemovedEventArgs(runningDriver.Driver.Identity));

        await runningDriver.CancellationTokenSource.CancelAsync();
        try
        {
            await runningDriver.Task;
        }
        catch (OperationCanceledException)
        {
        }

        runningDriver.Driver.InputReceived -= DriverOnInputReceived;
        await runningDriver.Driver.DisposeAsync();
        runningDriver.CancellationTokenSource.Dispose();
    }

    private void DriverOnInputReceived(object? sender, DeviceInputEvent e)
        => _inputSink.Post(e);

    private async Task SuperviseAsync(IDeviceWatcher watcher, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        watcher.DeviceArrived += WatcherOnDeviceArrived;
        watcher.DeviceRemoved += WatcherOnDeviceRemoved;

        try
        {
            await watcher.RunAsync(cts.Token);
        }
        finally
        {
            watcher.DeviceArrived -= WatcherOnDeviceArrived;
            watcher.DeviceRemoved -= WatcherOnDeviceRemoved;
        }
    }

    private void WatcherOnDeviceRemoved(object? sender, DeviceRemovedEventArgs e)
        => _deviceChanges.Writer.TryWrite(new DeviceChange.Removed(e.Identity));

    private void WatcherOnDeviceArrived(object? sender, DeviceArrivedEventArgs e)
        => _deviceChanges.Writer.TryWrite(new DeviceChange.Arrived(e.Descriptor));

    internal closed record DeviceChange
    {
        public sealed record Arrived(DeviceDescriptor Descriptor) : DeviceChange;

        public sealed record Removed(DeviceIdentity Identity) : DeviceChange;
    }

    private sealed record RunningDriver(
        CancellationTokenSource CancellationTokenSource,
        IDeviceDriver Driver,
        Task Task);
}