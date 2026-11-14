using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceWatcher
{
    IReadOnlyCollection<DeviceDescriptor> ConnectedDevices { get; }

    event EventHandler<DeviceArrivedEventArgs> DeviceArrived;

    event EventHandler<DeviceRemovedEventArgs> DeviceRemoved;

    Task RunAsync(CancellationToken cancellationToken);
}
