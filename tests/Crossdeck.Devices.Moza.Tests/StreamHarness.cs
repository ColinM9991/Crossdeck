using System.Buffers.Binary;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests;

internal static class StreamHarness
{
    private const long RetransmitStep = 6_000_000;
    private const int MaxFrames = 64;

    public static StreamConnection OpenedConnection(
        ushort service,
        ushort localPort,
        long now)
    {
        var connection = new StreamConnection(service, localPort, new Syn(1, 9090, StreamConnection.HostVersion), now);
        connection.OnAck(localPort, now);
        return connection;
    }

    public static List<byte[]> Outgoing(
        StreamConnection connection,
        long start,
        TimeProvider timeProvider)
    {
        var frames = new List<byte[]>();
        var now = start;

        for (var i = 0; i < MaxFrames; i++)
        {
            var wire = connection.Next(now, timeProvider);
            if (wire is null)
            {
                now += RetransmitStep;
                wire = connection.Next(now, timeProvider);
                if (wire is null)
                    break;
            }

            var (magic, isn, inner) = DecodeWire(wire);
            if (magic == Magic.Trans && inner.Length - 6 == Wire.CrcSize)
                break;

            connection.OnAck(isn, now);
            frames.Add(wire);
            now += RetransmitStep;
        }

        return frames;
    }

    public static (byte Magic, ushort Isn, byte[] Inner) DecodeWire(byte[] wire)
    {
        var inner = TunnelPayload(wire);
        var isAck = inner[0] == Wire.AckCommand;
        var magic = isAck ? Wire.AckCommand : inner[3];
        var isn = isAck
            ? BinaryPrimitives.ReadUInt16LittleEndian(inner.AsSpan(3))
            : BinaryPrimitives.ReadUInt16LittleEndian(inner.AsSpan(4));
        return (magic, isn, inner);
    }

    public static byte[] TunnelPayload(byte[] wire)
    {
        var frames = new List<SerialLinkFrame>();
        new SerialLinkDecoder().Decode(wire, frames);
        var frame = Assert.Single(frames);
        return frame.Payload;
    }
}
