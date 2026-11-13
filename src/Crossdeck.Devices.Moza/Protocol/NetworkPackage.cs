using System.Buffers.Binary;

namespace Crossdeck.Devices.Moza.Protocol;

internal static class NetworkPackage
{
    public const int HeaderSize = 5;
    public const int MaxPayloadSize = 16 * 1024 * 1024;

    public static byte[] Encode(byte id, ReadOnlySpan<byte> payload)
    {
        var package = new byte[HeaderSize + payload.Length];
        package[0] = id;
        BinaryPrimitives.WriteUInt32LittleEndian(package.AsSpan(1), (uint)payload.Length);
        payload.CopyTo(package.AsSpan(HeaderSize));
        return package;
    }

    public static bool TryRead(ByteQueue queue, out byte id, out byte[] payload)
    {
        id = 0;
        payload = [];
        if (queue.Length < HeaderSize)
            return false;

        var size = BinaryPrimitives.ReadUInt32LittleEndian(queue.Span[1..]);
        if (size > MaxPayloadSize)
            throw new FcdSessionLostException($"NetworkPackage size {size} exceeds {MaxPayloadSize} bytes.");

        var total = HeaderSize + (int)size;
        if (queue.Length < total)
            return false;

        id = queue.Peek();
        payload = queue.Span[HeaderSize..total].ToArray();
        queue.Consume(total);
        return true;
    }
}