using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Models;
using Xunit;

namespace Crossdeck.Devices.WinCtrl.Tests.Encoders;

public sealed class WinCtrlSegmentEncoderTests
{
    private static SegmentPanel CreateSinglePanel()
    {
        var digit = new DigitSlot([
            new BitAddr(0, 1), new BitAddr(0, 2), new BitAddr(0, 4), new BitAddr(0, 8),
            new BitAddr(0, 16), new BitAddr(0, 32), new BitAddr(0, 64), new BitAddr(0, 128),
        ]);
        var window = new SegmentWindow([digit]);

        return new SegmentPanel(
            "test",
            Target: 1,
            BufferSize: 2,
            Windows: new Dictionary<string, SegmentWindow> { ["digit"] = window },
            Labels: new Dictionary<string, BitAddr[]>(),
            Framing: new SegmentFraming(PayloadLength: 6, WriteId: 1, CommitId: 2, CommitInOwnPacket: false));
    }

    [Fact]
    public void EncodeFrame_WhenPanelContentIsUnchanged_ReturnsNoPacketsOnSecondCall()
    {
        // Arrange
        var sut = new WinCtrlSegmentEncoder([CreateSinglePanel()]);
        var texts = new Dictionary<string, string> { ["digit"] = "1" };
        var flags = new HashSet<string>();
        var firstResult = sut.EncodeFrame(texts, flags);

        // Act
        var secondResult = sut.EncodeFrame(texts, flags);

        // Assert
        Assert.NotEmpty(firstResult);
        Assert.Empty(secondResult);
    }

    [Fact]
    public void EncodeFrame_WhenPanelContentChanges_ReturnsPacketsAgain()
    {
        // Arrange
        var sut = new WinCtrlSegmentEncoder([CreateSinglePanel()]);
        var flags = new HashSet<string>();
        sut.EncodeFrame(new Dictionary<string, string> { ["digit"] = "1" }, flags);

        // Act
        var result = sut.EncodeFrame(new Dictionary<string, string> { ["digit"] = "2" }, flags);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public void EncodeFrame_WhenTextIsTooLongForTheWindow_ThrowsArgumentException()
    {
        // Arrange
        var sut = new WinCtrlSegmentEncoder([CreateSinglePanel()]);
        var texts = new Dictionary<string, string> { ["digit"] = "12" };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => sut.EncodeFrame(texts, new HashSet<string>()));
    }
}
