using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

/// <summary>
/// Tracks the device drivers of connected devices.
/// </summary>
public interface IDeviceRegistry : IRunnable
{
    /// <summary>
    /// Occurs when a device driver is added.
    /// </summary>
    event EventHandler<DriverAddedEventArgs>? DriverAdded;

    /// <summary>
    /// Occurs when a device driver is removed.
    /// </summary>
    event EventHandler<DriverRemovedEventArgs>? DriverRemoved;

    /// <summary>
    /// Attempts to retrieve the driver for a connected device.
    /// </summary>
    /// <param name="identity">The identity of the device to look up.</param>
    /// <param name="driver">When this method returns, the driver for the device, or null if not found.</param>
    /// <returns><c>true</c> if a driver was found; otherwise, <c>false</c>.</returns>
    bool TryGet(DeviceIdentity identity, out IDeviceDriver? driver);
}