using System.Buffers.Binary;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class WireTests
{
    [Fact]
    public void WithCrc_WhenChunkIsGiven_AppendsLittleEndianCrc32()
    {
        // Arrange
        byte[] chunk = [0x01, 0x02, 0x03];

        // Act
        var result = Wire.WithCrc(chunk);

        // Assert
        var crc = System.IO.Hashing.Crc32.HashToUInt32(chunk);
        byte[] expected =
        [
            .. chunk,
            (byte)crc, (byte)(crc >> 8), (byte)(crc >> 16), (byte)(crc >> 24)
        ];
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CrcOk_WhenCrcMatches_ReturnsTrue()
    {
        // Arrange
        var body = Wire.WithCrc([0x01, 0x02, 0x03]);

        // Act
        var result = Wire.CrcOk(body);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CrcOk_WhenCrcIsCorrupted_ReturnsFalse()
    {
        // Arrange
        var body = Wire.WithCrc([0x01, 0x02, 0x03]);
        body[^1] ^= 0xFF;

        // Act
        var result = Wire.CrcOk(body);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CrcOk_WhenBodyIsShorterThanCrcSize_ReturnsFalse()
    {
        // Arrange
        byte[] body = [0x01, 0x02];

        // Act
        var result = Wire.CrcOk(body);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Request_WhenRequestIsBuilt_WrapsInnerBytesInATunnelFrame()
    {
        // Arrange
        byte[] chunk = [0x01];

        // Act
        var wire = Wire.Request(0x1234, Magic.Trans, 0x5678, chunk);

        // Assert
        var inner = DecodeTunnelPayload(wire);
        Assert.Equal(0x7C, inner[0]);
        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16BigEndian(inner.AsSpan(1)));
        Assert.Equal(Magic.Trans, inner[3]);
        Assert.Equal(0x5678, BinaryPrimitives.ReadUInt16LittleEndian(inner.AsSpan(4)));
        Assert.Equal(0x01, inner[6]);
    }

    [Fact]
    public void Ack_WhenAckIsBuilt_WrapsInnerBytesInATunnelFrame()
    {
        // Act
        var wire = Wire.Ack(0x1234, 0x5678);

        // Assert
        var inner = DecodeTunnelPayload(wire);
        Assert.Equal(0xFC, inner[0]);
        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16BigEndian(inner.AsSpan(1)));
        Assert.Equal(0x5678, BinaryPrimitives.ReadUInt16LittleEndian(inner.AsSpan(3)));
    }

    [Fact]
    public void IsTunnel_WhenFrameIsATunnel_ReturnsTrue()
    {
        // Arrange
        var frame = new SerialLinkFrame(0x43, 0x21, []);

        // Act
        var result = Wire.IsTunnel(frame);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTunnel_WhenFramePairIsNotDeviceToHost_ReturnsFalse()
    {
        // Arrange
        var frame = new SerialLinkFrame(0x43, 0x12, []);

        // Act
        var result = Wire.IsTunnel(frame);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTunnel_WhenCommandIsNotATunnelCommand_ReturnsFalse()
    {
        // Arrange
        var frame = new SerialLinkFrame(0x44, 0x21, []);

        // Act
        var result = Wire.IsTunnel(frame);

        // Assert
        Assert.False(result);
    }

    private static byte[] DecodeTunnelPayload(byte[] wire)
    {
        var frames = new List<SerialLinkFrame>();
        new SerialLinkDecoder().Decode(wire, frames);
        var frame = Assert.Single(frames);
        return frame.Payload;
    }
}
