using Crossdeck.Devices.WinCtrl.Frames;
using Xunit;

namespace Crossdeck.Devices.WinCtrl.Tests.Frames;

public sealed class NoFrameSourceTests
{
    [Fact]
    public void EncodeInit_Always_ReturnsNoPackets()
    {
        // Arrange
        var sut = new NoFrameSource();

        // Act
        var result = sut.EncodeInit();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void TakeFrame_Always_ReturnsNull()
    {
        // Arrange
        var sut = new NoFrameSource();

        // Act
        var result = sut.TakeFrame();

        // Assert
        Assert.Null(result);
    }
}
