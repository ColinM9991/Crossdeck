using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceDriver : IAsyncDisposable
{
    event EventHandler<DeviceInputEvent> InputReceived;
    
    DeviceIdentity Identity { get; }
    
    IReadOnlyList<DeviceControl> Controls { get; }
    
    Task RunAsync(CancellationToken cancellationToken);
}