using System.Buffers.Binary;
using System.IO.Hashing;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Channels;

internal static class SettingFrame
{
    public const byte Header = 0xFF;
    private const int HeaderSize = 9;

    public static byte[] Encode(int id, ReadOnlySpan<byte> data)
    {
        var body = new byte[4 + data.Length];
        BinaryPrimitives.WriteInt32LittleEndian(body, id);
        data.CopyTo(body.AsSpan(4));

        var frame = new byte[HeaderSize + body.Length];
        frame[0] = Header;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(1), (uint)body.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(5), Crc32.HashToUInt32(body));
        body.CopyTo(frame.AsSpan(HeaderSize));
        return frame;
    }

    public static SettingReadResult TryRead(ByteQueue queue, out int id, out byte[] data)
    {
        id = 0;
        data = [];

        if (queue.Length < HeaderSize)
            return SettingReadResult.Incomplete;

        var span = queue.Span;
        var size = BinaryPrimitives.ReadUInt32LittleEndian(span[1..]);
        if (size is < 4 or > NetworkPackage.MaxPayloadSize)
        {
            queue.Consume(1);
            return SettingReadResult.Skipped;
        }

        var total = HeaderSize + (int)size;
        if (queue.Length < total)
            return SettingReadResult.Incomplete;

        var result = ReadBody(span[..total], out id, out data);
        queue.Consume(total);
        return result;
    }

    private static SettingReadResult ReadBody(ReadOnlySpan<byte> frame, out int id, out byte[] data)
    {
        id = 0;
        data = [];

        var crc = BinaryPrimitives.ReadUInt32LittleEndian(frame[5..]);
        var body = frame[HeaderSize..];
        if (Crc32.HashToUInt32(body) != crc)
            return SettingReadResult.BadCrc;

        id = BinaryPrimitives.ReadInt32LittleEndian(body);
        data = [.. body[4..]];
        return SettingReadResult.Frame;
    }
}