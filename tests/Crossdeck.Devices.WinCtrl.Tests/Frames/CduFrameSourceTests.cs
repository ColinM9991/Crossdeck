using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Tests.Frames;

public sealed class CduFrameSourceTests
{
    [Theory, AutoNSubstituteData]
    public void TakeFrame_OnFirstCall_ReturnsEncodedWaitingForDataScreen(
        byte[][] encoded,
        [Frozen] IWinCtrlDisplayEncoder encoder,
        CduFrameSource sut)
    {
        // Arrange
        encoder.EncodeFrame(TextScreen.WaitingForData).Returns(encoded);

        // Act
        var result = sut.TakeFrame();

        // Assert
        Assert.Equal(encoded, result);
    }

    [Theory, AutoNSubstituteData]
    public void TakeFrame_AfterConsuming_ReturnsNullUntilShowIsCalledAgain(CduFrameSource sut)
    {
        // Arrange
        sut.TakeFrame();

        // Act
        var result = sut.TakeFrame();

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public void Show_Always_MakesTheGivenScreenAvailableToTakeFrame(
        TextScreen screen,
        byte[][] encoded,
        [Frozen] IWinCtrlDisplayEncoder encoder,
        CduFrameSource sut)
    {
        // Arrange
        encoder.EncodeFrame(screen).Returns(encoded);
        sut.TakeFrame();

        // Act
        sut.Show(screen);
        var result = sut.TakeFrame();

        // Assert
        Assert.Equal(encoded, result);
    }

    [Theory, AutoNSubstituteData]
    public void EncodeInit_Always_DelegatesToTheEncoder(
        byte[][] encoded,
        [Frozen] IWinCtrlDisplayEncoder encoder,
        CduFrameSource sut)
    {
        // Arrange
        encoder.EncodeInit().Returns(encoded);

        // Act
        var result = sut.EncodeInit();

        // Assert
        Assert.Equal(encoded, result);
    }
}
