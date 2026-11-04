using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceWatcher
{
    event EventHandler<DeviceArrivedEventArgs> DeviceArrived;

    event EventHandler<DeviceRemovedEventArgs> DeviceRemoved;

    IReadOnlyCollection<DeviceDescriptor> ConnectedDevices { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
