namespace Crossdeck.Devices.Moza.Protocol;

internal sealed class ByteQueue
{
    private byte[] _buffer = new byte[256];
    private int _start;
    private int _end;

    public int Length => _end - _start;

    public ReadOnlySpan<byte> Span => _buffer.AsSpan(_start, Length);

    public byte Peek() => _buffer[_start];

    public void Append(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return;

        EnsureSpace(data.Length);
        data.CopyTo(_buffer.AsSpan(_end));
        _end += data.Length;
    }

    public void Consume(int count)
    {
        _start += Math.Min(count, Length);
        if (_start == _end)
            Clear();
    }

    public void Clear()
    {
        _start = 0;
        _end = 0;
    }

    private void EnsureSpace(int count)
    {
        if (_end + count <= _buffer.Length)
            return;

        var length = Length;
        var target = length + count > _buffer.Length
            ? new byte[Math.Max(_buffer.Length * 2, length + count)]
            : _buffer;

        _buffer.AsSpan(_start, length).CopyTo(target);
        _buffer = target;
        _start = 0;
        _end = length;
    }
}