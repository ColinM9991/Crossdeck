using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Models;

namespace Crossdeck.Devices.WinCtrl.Tests.Encoders;

public sealed class WinCtrlWinCtrlDisplayEncoderTests
{
    private static WinCtrlDefinition CreateCduDefinition() =>
        new("test", 1, new HashSet<ushort>(), [], WinCtrlKind.Cdu, new Dictionary<ushort, DisplayRole>(),
            Cdu: new CduScreenSettings(Left: 0, Top: 0, LargeFontId: 1, SmallFontId: 2), Displays: []);

    [Fact]
    public void Constructor_WithDefinitionMissingCduSettings_ThrowsArgumentException()
    {
        // Arrange
        var definition = new WinCtrlDefinition("test", 1, new HashSet<ushort>(), [], WinCtrlKind.Segment,
            new Dictionary<ushort, DisplayRole>(), Displays: []);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new WinCtrlWinCtrlDisplayEncoder(definition, TextPalette.Default));
    }

    [Fact]
    public void EncodeInit_Always_ProducesSixtyFourByteSetupReportPackets()
    {
        // Arrange
        var sut = new WinCtrlWinCtrlDisplayEncoder(CreateCduDefinition(), TextPalette.Default);

        // Act
        var packets = sut.EncodeInit();

        // Assert
        Assert.NotEmpty(packets);
        Assert.All(packets, p => Assert.Equal(64, p.Length));
        Assert.All(packets, p => Assert.Equal(0xF0, p[0]));
    }

    [Fact]
    public void EncodeFrame_Always_ProducesSixtyFourByteScreenReportPackets()
    {
        // Arrange
        var sut = new WinCtrlWinCtrlDisplayEncoder(CreateCduDefinition(), TextPalette.Default);

        // Act
        var packets = sut.EncodeFrame(TextScreen.WaitingForData);

        // Assert
        Assert.NotEmpty(packets);
        Assert.All(packets, p => Assert.Equal(64, p.Length));
        Assert.All(packets, p => Assert.Equal(0xF2, p[0]));
    }

    [Fact]
    public void Reset_Always_RestartsThePacketSequence()
    {
        // Arrange
        var sut = new WinCtrlWinCtrlDisplayEncoder(CreateCduDefinition(), TextPalette.Default);
        sut.EncodeInit();

        // Act
        sut.Reset();
        var packets = sut.EncodeInit();

        // Assert
        Assert.Equal(1, packets[0][2]);
    }
}
