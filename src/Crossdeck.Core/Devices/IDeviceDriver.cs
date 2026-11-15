using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

/// <summary>
/// Represents a connected device and exposes its controls and input events.
/// </summary>
public interface IDeviceDriver : IAsyncDisposable
{
    /// <summary>
    /// The identity of the device.
    /// </summary>
    DeviceIdentity Identity { get; }

    /// <summary>
    /// The controls available on the device.
    /// </summary>
    IReadOnlyList<DeviceControl> Controls { get; }

    /// <summary>
    /// Occurs when the device reports an input event.
    /// </summary>
    event EventHandler<DeviceInputEvent> InputReceived;

    /// <summary>
    /// Runs the driver, reading input from the device until cancelled.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RunAsync(CancellationToken cancellationToken);
}