namespace Crossdeck.Core.Devices;

public interface IDeviceTransport : IAsyncDisposable
{
    DeviceDescriptor Device { get; }
    
    Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
    
    Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}