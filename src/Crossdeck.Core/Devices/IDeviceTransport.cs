namespace Crossdeck.Core.Devices;

/// <summary>
/// Provides bidirectional byte transfer to a device.
/// </summary>
public interface IDeviceTransport : IAsyncDisposable
{
    /// <summary>
    /// Writes data to the device.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Reads data from the device.
    /// </summary>
    /// <param name="buffer">The buffer to read the data into.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of bytes read.</returns>
    Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}