using System.IO.Ports;

namespace Crossdeck.Devices.Serial;

public sealed class SerialTransport : ISerialTransport
{
    private readonly SerialPort _serialPort;

    public SerialTransport(
        SerialPort serialPort)
    {
        _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
    }

    public string PortName => _serialPort.PortName;

    public void Open()
    {
        if (_serialPort.IsOpen)
        {
            return;
        }

        _serialPort.Open();
    }

    public ValueTask DisposeAsync()
    {
        _serialPort.Dispose();

        return ValueTask.CompletedTask;
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
        await _serialPort.BaseStream.WriteAsync(data, cancellationToken);

    public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        await _serialPort.BaseStream.ReadAsync(buffer, cancellationToken);
}