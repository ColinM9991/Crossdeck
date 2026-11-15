using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Hid;

/// <summary>
/// Provides transport for HID reports to a device.
/// </summary>
public interface IHidTransport : IDeviceTransport
{
    /// <summary>
    /// The maximum length of an input report, in bytes.
    /// </summary>
    int MaxInputReportLength { get; }

    /// <summary>
    /// Sends a feature report to the device.
    /// </summary>
    /// <param name="report">The feature report to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SetFeatureAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a feature report from the device.
    /// </summary>
    /// <param name="report">The buffer to read the feature report into.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of bytes read.</returns>
    Task<int> GetFeatureAsync(Memory<byte> report, CancellationToken cancellationToken);
}