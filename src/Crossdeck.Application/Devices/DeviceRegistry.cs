using System.Collections.Immutable;
using System.Threading.Channels;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Devices;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Devices;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private static readonly TimeSpan DriverRetryDelay = TimeSpan.FromSeconds(5);

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
    private readonly TimeSpan _driverRetryDelay;
    private readonly ILogger<DeviceRegistry> _logger;

    private volatile ImmutableDictionary<DeviceIdentity, IDeviceDriver> _snapshot =
        ImmutableDictionary<DeviceIdentity, IDeviceDriver>.Empty;

    public DeviceRegistry(
        IEnumerable<IDeviceWatcher> deviceWatchers,
        IEnumerable<IDeviceDriverFactory> deviceDriverFactories,
        IInputSink inputSink,
        ILogger<DeviceRegistry> logger,
        TimeSpan? driverRetryDelay = null)
    {
        _deviceWatchers = deviceWatchers ?? throw new ArgumentNullException(nameof(deviceWatchers));
        _deviceDriverFactories = [.. (deviceDriverFactories ?? throw new ArgumentNullException(nameof(deviceDriverFactories)))
            .OrderByDescending(x => x.Priority)];
        _inputSink = inputSink ?? throw new ArgumentNullException(nameof(inputSink));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _driverRetryDelay = driverRetryDelay ?? DriverRetryDelay;
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
                        DeviceChange.Removed r => StopAsync(r.Identity),
                        DeviceChange.Faulted f => RecoverAsync(f.Descriptor, cancellationToken)
                    };

                    await task;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to process device change {Change}", change);
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
        if (_deviceDrivers.ContainsKey(descriptor.Identity))
            return;

        var factory = _deviceDriverFactories.FirstOrDefault(f => f.CanCreate(descriptor));
        if (factory is null)
        {
            _logger.LogWarning("No driver factory matched {Identity}", descriptor.Identity);
            return;
        }

        var driver = await factory.CreateAsync(descriptor, cancellationToken);
        if (driver is null)
            return;

        var deviceCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var task = Task.Factory.StartNew(() => driver.RunAsync(deviceCancellationToken.Token),
            deviceCancellationToken.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

        var runningDriver = new RunningDriver(deviceCancellationToken, driver, task);
        _deviceDrivers.Add(driver.Identity, runningDriver);
        _snapshot = _snapshot.SetItem(driver.Identity, driver);
        driver.InputReceived += DriverOnInputReceived;

        _logger.LogInformation("Driver added {Identity}", driver.Identity);
        DriverAdded?.Invoke(this, new DriverAddedEventArgs(driver.Identity, driver));

        ObserveFault(runningDriver, descriptor);
    }

    private async Task StopAsync(DeviceIdentity identity)
    {
        if (!_deviceDrivers.Remove(identity, out var runningDriver))
            return;

        _snapshot = _snapshot.Remove(identity);
        _logger.LogInformation("Driver removed {Identity}", identity);
        DriverRemoved?.Invoke(this, new DriverRemovedEventArgs(runningDriver.Driver.Identity));

        await runningDriver.CancellationTokenSource.CancelAsync();
        try
        {
            await runningDriver.Task;
        }
        catch (Exception)
        {
            // A faulted driver is already logged by ObserveFault before it reaches here.
        }

        runningDriver.Driver.InputReceived -= DriverOnInputReceived;
        await runningDriver.Driver.DisposeAsync();
        runningDriver.CancellationTokenSource.Dispose();
    }

    private void ObserveFault(RunningDriver runningDriver, DeviceDescriptor descriptor)
    {
        runningDriver.Task.ContinueWith(_ =>
        {
            if (!runningDriver.Task.IsFaulted)
                return;

            foreach (var failure in runningDriver.Task.Exception!.InnerExceptions)
            {
                _logger.LogError(failure, "Driver for {Identity} faulted; restarting in {RetryDelay}",
                    runningDriver.Driver.Identity, _driverRetryDelay);
            }

            _deviceChanges.Writer.TryWrite(new DeviceChange.Faulted(descriptor));
        }, TaskScheduler.Default);
    }

    private async Task RecoverAsync(DeviceDescriptor descriptor, CancellationToken cancellationToken)
    {
        await StopAsync(descriptor.Identity);
        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            await Task.Delay(_driverRetryDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _logger.LogInformation("Restarting driver for {Identity}", descriptor.Identity);
        await StartAsync(descriptor, cancellationToken);
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

        public sealed record Faulted(DeviceDescriptor Descriptor) : DeviceChange;
    }

    private sealed record RunningDriver(
        CancellationTokenSource CancellationTokenSource,
        IDeviceDriver Driver,
        Task Task);
}