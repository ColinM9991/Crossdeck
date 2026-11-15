using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

/// <summary>
/// Creates drivers for devices it can recognise.
/// </summary>
public interface IDeviceDriverFactory
{
    /// <summary>
    /// The priority of this factory. Higher values are considered first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines whether this factory can create a driver for the given device.
    /// </summary>
    /// <param name="deviceDescriptor">The descriptor of the device to create a driver for.</param>
    /// <returns><c>true</c> if a driver can be created for the device; otherwise, <c>false</c>.</returns>
    bool CanCreate(DeviceDescriptor deviceDescriptor);

    /// <summary>
    /// Creates a driver for the given device, or returns null when it cannot.
    /// </summary>
    /// <param name="deviceDescriptor">The descriptor of the device to create a driver for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A driver for the device, or null if one cannot be created.</returns>
    Task<IDeviceDriver?> CreateAsync(DeviceDescriptor deviceDescriptor, CancellationToken cancellationToken);
}