using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class SerialLinkCodecTests
{
    [Theory, AutoNSubstituteData]
    public void Encode_WhenCalled_ReturnsExpectedValue(
        SerialLinkCodec codec)
    {
        // Arrange
        const int command = 0x43;
        const int pair = 0x12;
        var payload = new byte[] { 0x7C, 0x23, 0x3C, 0x01, 0x7E };
        var expected = new byte[] { 0x7E, 0x05, 0x43, 0x12, 0x7C, 0x23, 0x3C, 0x01, 0x7E, 0x7E, 0xBD };

        // Act
        var result = codec.Encode(command, pair, payload);

        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory, AutoNSubstituteData]
    public void Encode_EscapesChecksum_WhenChecksumIs7E(
        SerialLinkCodec codec)
    {
        // Arrange
        var payload = new byte[] { 0x9D };
        var expected = new byte[] { 0x7E, 0x01, 0x43, 0x12, 0x9D, 0x7E, 0x7E };

        // Act
        var result = codec.Encode(0x43, 0x12, payload);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_EscapesPayloadAndChecksum_WhenBothAre7E(
        SerialLinkCodec codec)
    {
        // Arrange
        var payload = new byte[] { 0x7E, 0xA0 };
        var expected = new byte[] { 0x7E, 0x02, 0x43, 0x12, 0x7E, 0x7E, 0xA0, 0x7E, 0x7E };

        // Act
        var result = codec.Encode(0x43, 0x12, payload);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WhenPayloadIsEmpty_ReturnsFrameWithZeroLength(
        SerialLinkCodec codec)
    {
        // Arrange
        var expected = new byte[] { 0x7E, 0x00, 0x43, 0x12, 0xE0 };

        // Act
        var result = codec.Encode(0x43, 0x12, []);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WhenPayloadIs64Bytes_ReturnsFrameWithLengthField64(
        SerialLinkCodec codec)
    {
        // Arrange
        var payload = new byte[64];
        var expected = new byte[] { 0x7E, 0x40, 0x43, 0x12 }.Concat(payload).Append((byte)0x20).ToArray();

        // Act
        var result = codec.Encode(0x43, 0x12, payload);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WhenPayloadContainsMultipleSofBytes_ReturnsEachByteEscaped(
        SerialLinkCodec codec)
    {
        // Arrange
        var payload = new byte[] { 0x7E, 0x7E, 0x7E };
        var expected = new byte[] { 0x7E, 0x03, 0x43, 0x12, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0xD7 };

        // Act
        var result = codec.Encode(0x43, 0x12, payload);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WhenSumExceedsByteRange_ReturnsLowEightBitsOfChecksum(
        SerialLinkCodec codec)
    {
        // Arrange
        var payload = Enumerable.Repeat((byte)0xFF, 10).ToArray();
        var expected = new byte[] { 0x7E, 0x0A, 0x43, 0x12 }.Concat(payload).Append((byte)0xE0).ToArray();

        // Act
        var result = codec.Encode(0x43, 0x12, payload);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WhenPayloadExceeds64Bytes_ThrowsArgumentOutOfRangeException(
        SerialLinkCodec codec)
    {
        // Arrange
        var payload = new byte[65];

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(0x43, 0x12, payload));
    }
}
