using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceRegistry : IRunnable
{
    event EventHandler<DriverAddedEventArgs>? DriverAdded;

    event EventHandler<DriverRemovedEventArgs>? DriverRemoved;

    bool TryGet(DeviceIdentity identity, out IDeviceDriver? driver);
}