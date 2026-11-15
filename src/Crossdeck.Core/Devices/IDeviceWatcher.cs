using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

/// <summary>
/// Monitors the set of connected devices.
/// </summary>
public interface IDeviceWatcher
{
    /// <summary>
    /// The devices currently connected.
    /// </summary>
    IReadOnlyCollection<DeviceDescriptor> ConnectedDevices { get; }

    /// <summary>
    /// Occurs when a device is connected.
    /// </summary>
    event EventHandler<DeviceArrivedEventArgs> DeviceArrived;

    /// <summary>
    /// Occurs when a device is removed.
    /// </summary>
    event EventHandler<DeviceRemovedEventArgs> DeviceRemoved;

    /// <summary>
    /// Runs the watcher, raising device events until cancelled.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RunAsync(CancellationToken cancellationToken);
}
