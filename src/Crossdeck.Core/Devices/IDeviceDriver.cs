namespace Crossdeck.Core.Devices;

public interface IDeviceDriver : IAsyncDisposable
{
    DeviceIdentity Identity { get; }
    
    IReadOnlyList<DeviceControl> Controls { get; }
    
    Task RunAsync(CancellationToken cancellationToken);
}