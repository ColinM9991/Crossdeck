using System.Buffers.Binary;

namespace Crossdeck.Devices.Moza.Protocol;

internal readonly record struct Syn(ushort Isn, ushort AnnouncedPort, byte Version)
{
    public static bool TryParse(ReadOnlySpan<byte> tunnel, out Syn syn)
    {
        syn = default;
        if (tunnel.Length < 10 || tunnel[8] != (byte)~tunnel[9])
            return false;

        syn = new Syn(
            BinaryPrimitives.ReadUInt16LittleEndian(tunnel[4..]),
            BinaryPrimitives.ReadUInt16LittleEndian(tunnel[6..]),
            tunnel[9]);
        return true;
    }
}