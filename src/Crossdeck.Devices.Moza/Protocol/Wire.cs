using System.Buffers.Binary;
using System.IO.Hashing;

namespace Crossdeck.Devices.Moza.Protocol;

internal static class Wire
{
    public const byte RequestCommand = 0x7C;
    public const byte AckCommand = 0xFC;
    private const byte TunnelCommand = 0x43;
    private const byte DeviceToHostPair = 0x21;
    public const int CrcSize = 4;

    public static bool IsTunnel(SerialLinkFrame frame) =>
        (frame.Command & 0x7F) == TunnelCommand && frame.Pair == DeviceToHostPair;

    public static byte[] Request(ushort destination, byte magic, ushort isn, ReadOnlySpan<byte> body)
    {
        var inner = new byte[6 + body.Length];
        inner[0] = RequestCommand;
        BinaryPrimitives.WriteUInt16BigEndian(inner.AsSpan(1), destination);
        inner[3] = magic;
        BinaryPrimitives.WriteUInt16LittleEndian(inner.AsSpan(4), isn);
        body.CopyTo(inner.AsSpan(6));
        return SerialLinkEncoder.Tunnel(inner);
    }

    public static byte[] Ack(ushort destination, ushort isn)
    {
        Span<byte> inner = stackalloc byte[5];
        inner[0] = AckCommand;
        BinaryPrimitives.WriteUInt16BigEndian(inner[1..], destination);
        BinaryPrimitives.WriteUInt16LittleEndian(inner[3..], isn);
        return SerialLinkEncoder.Tunnel(inner);
    }

    public static byte[] WithCrc(ReadOnlySpan<byte> chunk)
    {
        var body = new byte[chunk.Length + CrcSize];
        chunk.CopyTo(body);
        BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(chunk.Length), Crc32.HashToUInt32(chunk));
        return body;
    }

    public static bool CrcOk(ReadOnlySpan<byte> body) =>
        body.Length >= CrcSize &&
        Crc32.HashToUInt32(body[..^CrcSize]) == BinaryPrimitives.ReadUInt32LittleEndian(body[^CrcSize..]);
}