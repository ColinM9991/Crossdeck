namespace Crossdeck.Core.Devices;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly IEnumerable<IDeviceWatcher> _deviceWatchers;

    public DeviceRegistry(IEnumerable<IDeviceWatcher> deviceWatchers)
    {
        _deviceWatchers = deviceWatchers ?? throw new ArgumentNullException(nameof(deviceWatchers));
    }
    
    // TODO implement retries, fault tolerance
    // Scenarios:
    // A supervisor dies. Needs to be restarted
    // A supervisor dies. Trips RunAsync
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var supervisorTasks = _deviceWatchers.Select(w => SuperviseAsync(w, cancellationToken)).ToList();

        await Task.WhenAll(supervisorTasks);
    }

    private static async Task SuperviseAsync(IDeviceWatcher watcher, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await watcher.RunAsync(cts.Token);
    }
}