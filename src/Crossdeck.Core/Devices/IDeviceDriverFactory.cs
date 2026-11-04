using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceDriverFactory
{
    int Priority { get; }
    
    bool CanCreate(DeviceDescriptor deviceDescriptor);
    
    Task<IDeviceDriver?> CreateAsync(DeviceDescriptor deviceDescriptor, CancellationToken cancellationToken);
}