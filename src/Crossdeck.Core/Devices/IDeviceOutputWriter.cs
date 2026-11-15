using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

/// <summary>
/// Writes output values to a device.
/// </summary>
public interface IDeviceOutputWriter
{
    /// <summary>
    /// Sets the value of an output.
    /// </summary>
    /// <param name="id">The identity of the output to set.</param>
    /// <param name="value">The value to apply to the output.</param>
    void Set(OutputId id, int value);

    /// <summary>
    /// Runs the writer, sending output values to the device until cancelled.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RunAsync(CancellationToken cancellationToken);
}