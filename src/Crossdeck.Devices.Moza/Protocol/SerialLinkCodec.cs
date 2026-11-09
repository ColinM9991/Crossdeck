namespace Crossdeck.Devices.Moza.Protocol;

public sealed class SerialLinkCodec
{
    public byte[] Encode(byte command, byte pair, ReadOnlySpan<byte> payload)
    {
        var length = (byte)payload.Length;
        var frame = new List<byte>
        {
            MozaFrameConstants.SerialLinkFrameId,
        };

        var checksum = 0x0D + MozaFrameConstants.SerialLinkFrameId;

        Put(length);
        Put(command);
        Put(pair);
        foreach (var b in payload)
            Put(b);

        var sum = (byte)checksum;
        frame.Add(sum);

        if (sum == MozaFrameConstants.SerialLinkFrameId)
            frame.Add(sum);
        
        return [.. frame];

        void Put(byte b, bool shouldEscape = true)
        {
            while (true)
            {
                frame.Add(b);
                checksum += b;

                if (shouldEscape && b == MozaFrameConstants.SerialLinkFrameId)
                {
                    shouldEscape = false;
                    continue;
                }

                break;
            }
        }
    }

    public byte[] Tunnel(ReadOnlySpan<byte> inner) => Encode(0x43, 0x12, inner);
}