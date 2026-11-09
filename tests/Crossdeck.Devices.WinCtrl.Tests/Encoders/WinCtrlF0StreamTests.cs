using System.Buffers.Binary;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Testing;
using Xunit;

namespace Crossdeck.Devices.WinCtrl.Tests.Encoders;

public sealed class WinCtrlF0StreamTests
{
    [Theory, AutoNSubstituteData]
    public void Record_Always_WritesHeaderFieldsLittleEndian(ushort target, uint command, uint id, byte[] payload)
    {
        // Arrange
        var sut = new WinCtrlF0Stream(target);

        // Act
        var record = sut.Record(command, id, payload);

        // Assert
        Assert.Equal(target, BinaryPrimitives.ReadUInt16LittleEndian(record));
        Assert.Equal(command, BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(4)));
        Assert.Equal(id, BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(8)));
        Assert.Equal((uint)payload.Length, BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(13)));
    }

    [Theory, AutoNSubstituteData]
    public void Record_Always_AppendsPayloadAfterTheSeventeenByteHeader(ushort target, uint command, uint id, byte[] payload)
    {
        // Arrange
        var sut = new WinCtrlF0Stream(target);

        // Act
        var record = sut.Record(command, id, payload);

        // Assert
        Assert.Equal(17 + payload.Length, record.Length);
        Assert.Equal(payload, record[17..]);
    }

    [Fact]
    public void Packetise_WithStreamLargerThanOnePacket_SplitsIntoFiftySixByteChunks()
    {
        // Arrange
        var sut = new WinCtrlF0Stream(target: 1);
        var stream = new byte[56 + 10];

        // Act
        var packets = sut.Packetise(stream);

        // Assert
        Assert.Equal(2, packets.Length);
        Assert.Equal(56, packets[0][3]);
        Assert.Equal(10, packets[1][3]);
    }

    [Fact]
    public void Packetise_Always_IncrementsSequencePerPacket()
    {
        // Arrange
        var sut = new WinCtrlF0Stream(target: 1);
        var stream = new byte[56 + 1];

        // Act
        var packets = sut.Packetise(stream);

        // Assert
        Assert.Equal(1, packets[0][2]);
        Assert.Equal(2, packets[1][2]);
    }

    [Fact]
    public void PacketisePerRecord_Always_UsesTheSameSequenceForEveryRecordInTheCall()
    {
        // Arrange
        var sut = new WinCtrlF0Stream(target: 1);
        IReadOnlyList<byte[]> records = [new byte[4], new byte[4], new byte[4]];

        // Act
        var packets = sut.PacketisePerRecord(records);

        // Assert
        Assert.All(packets, p => Assert.Equal(1, p[2]));
    }

    [Fact]
    public void Reset_Always_RestartsSequenceAtOne()
    {
        // Arrange
        var sut = new WinCtrlF0Stream(target: 1);
        sut.Packetise(new byte[1]);
        sut.Packetise(new byte[1]);

        // Act
        sut.Reset();
        var packets = sut.Packetise(new byte[1]);

        // Assert
        Assert.Equal(1, packets[0][2]);
    }

    [Fact]
    public void Packetise_WhenSequenceReaches255_WrapsToOne()
    {
        // Arrange
        var sut = new WinCtrlF0Stream(target: 1);
        for (var i = 0; i < 254; i++) sut.Packetise(new byte[1]);

        // Act
        var atMax = sut.Packetise(new byte[1]);
        var afterWrap = sut.Packetise(new byte[1]);

        // Assert
        Assert.Equal(255, atMax[0][2]);
        Assert.Equal(1, afterWrap[0][2]);
    }
}
