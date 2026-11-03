using System.Threading.Channels;

namespace Crossdeck.Core.Devices;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly Channel<DeviceChange> _deviceChanges = Channel.CreateUnbounded<DeviceChange>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly IEnumerable<IDeviceWatcher> _deviceWatchers;
    private readonly IReadOnlyCollection<IDeviceDriverFactory> _deviceDriverFactories;

    private readonly Dictionary<DeviceIdentity, RunningDriver> _deviceDrivers = new();

    public DeviceRegistry(
        IEnumerable<IDeviceWatcher> deviceWatchers,
        IEnumerable<IDeviceDriverFactory> deviceDriverFactories)
    {
        _deviceWatchers = deviceWatchers ?? throw new ArgumentNullException(nameof(deviceWatchers));
        _deviceDriverFactories = deviceDriverFactories.OrderByDescending(x => x.Priority).ToList();
    }

    // TODO implement retries, fault tolerance
    // Scenarios:
    // A supervisor dies. Needs to be restarted
    // A supervisor dies. Trips RunAsync
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var supervisorTasks = _deviceWatchers.Select(w => SuperviseAsync(w, cancellationToken)).ToList();
        try
        {
            await foreach (var change in _deviceChanges.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    switch (change)
                    {
                        case DeviceChange.Arrived a: await StartAsync(a.Descriptor, cancellationToken); break;
                        case DeviceChange.Removed r: await StopAsync(r.Identity); break;
                    }
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

    private async Task StopAsync(DeviceIdentity identity)
    {
        if (!_deviceDrivers.Remove(identity, out var runningDriver))
            return;

        await runningDriver.CancellationTokenSource.CancelAsync();
        try
        {
            await runningDriver.Task;
        } 
        catch (OperationCanceledException) { }
        catch (Exception) { }

        await runningDriver.Driver.DisposeAsync();
        runningDriver.CancellationTokenSource.Dispose();
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
        var task = Task.Factory.StartNew(() => driver.RunAsync(deviceCancellationToken.Token), deviceCancellationToken.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        
        _deviceDrivers.Add(driver.Identity, new RunningDriver(deviceCancellationToken, driver, task));
    }

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
    {
        _deviceChanges.Writer.TryWrite(new DeviceChange.Removed(e.Identity));
    }

    private void WatcherOnDeviceArrived(object? sender, DeviceArrivedEventArgs e)
    {
        _deviceChanges.Writer.TryWrite(new DeviceChange.Arrived(e.Descriptor));
    }

    private sealed record RunningDriver(CancellationTokenSource CancellationTokenSource, IDeviceDriver Driver, Task Task);

    internal abstract record DeviceChange
    {
        public sealed record Arrived(DeviceDescriptor Descriptor) : DeviceChange;

        public sealed record Removed(DeviceIdentity Identity) : DeviceChange;
    }
}