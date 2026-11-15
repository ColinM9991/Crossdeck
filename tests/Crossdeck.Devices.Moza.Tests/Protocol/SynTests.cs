using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class SynTests
{
    [Fact]
    public void TryParse_WhenSyn1IsWellFormed_ReturnsParsedFields()
    {
        // Arrange
        byte[] tunnel =
        [
            0x7C,
            0x82, 0x23, // destination 9090
            Magic.Syn1,
            0x34, 0x12, // ISN 0x1234
            0x82, 0x23, // announced port 9090
            0xFD, // complement of version
            0x02, // version
        ];

        // Act
        var parsed = Syn.TryParse(tunnel, out var syn);

        // Assert
        Assert.True(parsed);
        Assert.Equal(0x1234, syn.Isn);
        Assert.Equal(9090, syn.AnnouncedPort);
        Assert.Equal(0x02, syn.Version);
    }

    [Fact]
    public void TryParse_WhenTunnelIsShorterThanTenBytes_ReturnsFalseWithDefault()
    {
        // Arrange
        byte[] tunnel = [0x7C, 0x82, 0x23, Magic.Syn1, 0x34, 0x12, 0x82, 0x23];

        // Act
        var parsed = Syn.TryParse(tunnel, out var syn);

        // Assert
        Assert.False(parsed);
        Assert.Equal(default, syn);
    }

    [Fact]
    public void TryParse_WhenChecksumIsWrong_ReturnsFalseWithDefault()
    {
        // Arrange
        byte[] tunnel =
        [
            0x7C,
            0x82, 0x23,
            Magic.Syn1,
            0x34, 0x12,
            0x82, 0x23,
            0x00,
            0x02,
        ];

        // Act
        var parsed = Syn.TryParse(tunnel, out var syn);

        // Assert
        Assert.False(parsed);
        Assert.Equal(default, syn);
    }
}
