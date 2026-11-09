using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;
using Xunit;

namespace Crossdeck.Devices.WinCtrl.Tests.Frames;

public sealed class SegmentFrameSourceTests
{
    private static SegmentFrameSource CreateSut() => new(new WinCtrlSegmentEncoder([]));

    [Fact]
    public void TakeFrame_WithNoPendingChanges_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.TakeFrame();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetText_Always_MarksTheFrameDirty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetText(new OutputId(OutputKind.Display, "spd"), "100");
        var result = sut.TakeFrame();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void SetFlag_Always_MarksTheFrameDirty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetFlag("spd", true);
        var result = sut.TakeFrame();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void TakeFrame_AfterConsuming_ReturnsNullUntilSetAgain()
    {
        // Arrange
        var sut = CreateSut();
        sut.SetText(new OutputId(OutputKind.Display, "spd"), "100");
        sut.TakeFrame();

        // Act
        var result = sut.TakeFrame();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EncodeInit_Always_ReturnsNoPackets()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.EncodeInit();

        // Assert
        Assert.Empty(result);
    }
}
