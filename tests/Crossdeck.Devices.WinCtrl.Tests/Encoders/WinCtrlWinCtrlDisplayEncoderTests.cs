using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.WinCtrl.Encoders;

namespace Crossdeck.Devices.WinCtrl.Tests.Encoders;

public sealed class WinCtrlWinCtrlDisplayEncoderTests
{
    [Theory, AutoNSubstituteData]
    public void Constructor_WithDefinitionMissingCduSettings_ThrowsArgumentException(
        WinCtrlDefinition definition,
        TextPalette palette)
    {
        // Arrange
        var missing = definition with { Cdu = null };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new WinCtrlWinCtrlDisplayEncoder(missing, palette));
    }

    [Theory, AutoNSubstituteData]
    public void EncodeInit_Always_ProducesSixtyFourByteSetupReportPackets(
        WinCtrlDefinition definition,
        TextPalette palette)
    {
        // Arrange
        var sut = new WinCtrlWinCtrlDisplayEncoder(definition, palette);

        // Act
        var packets = sut.EncodeInit();

        // Assert
        Assert.NotEmpty(packets);
        Assert.All(packets, p => Assert.Equal(64, p.Length));
        Assert.All(packets, p => Assert.Equal(0xF0, p[0]));
    }

    [Theory, AutoNSubstituteData]
    public void EncodeFrame_Always_ProducesSixtyFourByteScreenReportPackets(
        WinCtrlDefinition definition,
        TextPalette palette)
    {
        // Arrange
        var sut = new WinCtrlWinCtrlDisplayEncoder(definition, palette);

        // Act
        var packets = sut.EncodeFrame(TextScreen.WaitingForData);

        // Assert
        Assert.NotEmpty(packets);
        Assert.All(packets, p => Assert.Equal(64, p.Length));
        Assert.All(packets, p => Assert.Equal(0xF2, p[0]));
    }

    [Theory, AutoNSubstituteData]
    public void Reset_Always_RestartsThePacketSequence(
        WinCtrlDefinition definition,
        TextPalette palette)
    {
        // Arrange
        var sut = new WinCtrlWinCtrlDisplayEncoder(definition, palette);
        sut.EncodeInit();

        // Act
        sut.Reset();
        var packets = sut.EncodeInit();

        // Assert
        Assert.Equal(1, packets[0][2]);
    }
}
