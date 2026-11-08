using System.Buffers.Binary;

namespace Crossdeck.Devices.WinCtrl.Encoders;

internal sealed class WinCtrlF0Stream(ushort target)
{
    private const int PacketSize = 56;
    private const int Offset = 4;
    private const byte ReportId = 0xF0;
    private const int HeaderSize = 17;

    private byte _sequence = 1;

    public byte[] Record(uint command, uint id, ReadOnlySpan<byte> payload)
    {
        var record = new byte[HeaderSize + payload.Length];
        record[0] = (byte)(target & 0xFF);
        record[1] = (byte)(target >> 8);

        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(4), command);
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(8), id);

        record[12] = 0x00;

        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(13), (uint)payload.Length);

        payload.CopyTo(record.AsSpan(HeaderSize));

        return record;
    }

    public byte[][] Packetise(ReadOnlySpan<byte> stream)
    {
        var numberOfPackets = (stream.Length + PacketSize - 1) / PacketSize;
        var packets = new byte[numberOfPackets][];

        for (var i = 0; i < numberOfPackets; i++)
        {
            var start = i * PacketSize;
            var length = Math.Min(PacketSize, stream.Length - start);

            var buffer = new byte[64];
            buffer[0] = ReportId;
            buffer[1] = 0x00;
            buffer[2] = _sequence;
            buffer[3] = (byte)length;

            stream.Slice(start, length).CopyTo(buffer.AsSpan(Offset));
            packets[i] = buffer;

            Advance();
        }

        return packets;
    }

    public byte[][] PacketisePerRecord(IReadOnlyList<byte[]> records)
    {
        var packets = new byte[records.Count][];

        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];

            var buffer = new byte[64];
            buffer[0] = ReportId;
            buffer[1] = 0x00;
            buffer[2] = _sequence;
            buffer[3] = (byte)record.Length;

            record.AsSpan().CopyTo(buffer.AsSpan(Offset));
            packets[i] = buffer;
        }

        Advance();
        return packets;
    }

    public void Reset() => _sequence = 1;

    private void Advance() => _sequence = _sequence == 255 ? (byte)1 : (byte)(_sequence + 1);
}