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
}
