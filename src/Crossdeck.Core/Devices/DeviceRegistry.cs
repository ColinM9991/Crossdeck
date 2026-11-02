namespace Crossdeck.Core.Devices;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly IEnumerable<IDeviceWatcher> _deviceWatchers;

    public DeviceRegistry(IEnumerable<IDeviceWatcher> deviceWatchers)
    {
        _deviceWatchers = deviceWatchers ?? throw new ArgumentNullException(nameof(deviceWatchers));
    }
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var supervisorTasks = _deviceWatchers.Select(w => SuperviseAsync(w, cancellationToken)).ToList();

        await Task.WhenAll(supervisorTasks);
    }

    private static async Task SuperviseAsync(IDeviceWatcher watcher, CancellationToken cancellationToken)
    {
        watcher.DeviceArrived += (sender, args) =>
        {
            Console.WriteLine($"Device arrived {args.Descriptor.ProductName}");
        };

        watcher.DeviceRemoved += (sender, args) =>
        {
            Console.WriteLine($"Device removed {args.Identity.StableId}");
        };
        
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await watcher.RunAsync(cts.Token);
    }
}