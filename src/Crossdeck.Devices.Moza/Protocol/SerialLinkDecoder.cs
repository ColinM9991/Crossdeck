namespace Crossdeck.Devices.Moza.Protocol;

public sealed class SerialLinkDecoder
{
    private const int HeaderSize = 3;
    private const int MaxPayloadSize = 64;
    private const int ChecksumSize = 1;
    
    private readonly byte[] _buffer = new byte[HeaderSize + MaxPayloadSize + ChecksumSize];
    private bool _isInFrame;
    private bool _isEscapeSequence;
    private int _count;
    private int _sum;

    public void Decode(ReadOnlySpan<byte> data, List<SerialLinkFrame> frames)
    {
        foreach (var b in data)
        {
            var isSofByte = b == MozaFrameConstants.SerialLinkFrameId;
            if (_isEscapeSequence)
            {
                _isEscapeSequence = false;
                
                if (isSofByte)
                {
                    Take(b, 2, frames);
                    continue;
                }

                Start();
            }
            else if (isSofByte)
            {
                if (_isInFrame)
                {
                    _isEscapeSequence = true;
                    continue;
                }

                Start();
                continue;
            }

            if (!_isInFrame) continue;

            Take(b, 1, frames);
        }
    }

    private void Start()
    {
        _isInFrame = true;
        _count = 0;
        _sum = 0x0D + MozaFrameConstants.SerialLinkFrameId;
    }

    private void Take(byte b, int copies, List<SerialLinkFrame> frames)
    {
        if (_count == 0 && b > MaxPayloadSize)
        {
            _isInFrame = false;
            return;
        }

        var isChecksum = _count > 0 && _count == HeaderSize + _buffer[0];
        _buffer[_count++] = b;

        if (!isChecksum)
        {
            _sum += b * copies;
            return;
        }

        _isInFrame = false;
        
        var checksumMismatch = (byte)_sum != b;
        if (checksumMismatch)
            return;

        var payload = _buffer.AsSpan(HeaderSize, _buffer[0]).ToArray();
        frames.Add(new SerialLinkFrame(_buffer[1], _buffer[2], payload));
    }
}