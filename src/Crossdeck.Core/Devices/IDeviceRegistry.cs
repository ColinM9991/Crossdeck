using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceRegistry : IRunnable
{
    bool TryGet(DeviceIdentity identity, out IDeviceDriver? driver);

    event EventHandler<DriverAddedEventArgs>? DriverAdded;

    event EventHandler<DriverRemovedEventArgs>? DriverRemoved;
}