using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceDriver : IAsyncDisposable
{
    DeviceIdentity Identity { get; }
    
    IReadOnlyList<DeviceControl> Controls { get; }
    
    event EventHandler<DeviceInputEvent> InputReceived;
    
    Task RunAsync(CancellationToken cancellationToken);
}