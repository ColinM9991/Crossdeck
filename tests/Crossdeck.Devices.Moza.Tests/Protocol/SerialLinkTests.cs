using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class SerialLinkTests
{
    private const long TicksPerSecond = TimeSpan.TicksPerSecond;

    [Fact]
    public void Start_WhenCalled_ReturnsRootRequestAndEntersRootSent()
    {
        // Arrange
        var sut = new SerialLink(TimeProvider.System);

        // Act
        var result = sut.Start(0);

        // Assert
        Assert.Equal([0x7E, 0x00, 0x00, 0x12, 0x9D], result);
        Assert.Equal(LinkState.RootSent, sut.State);
    }

    [Fact]
    public void OnFrame_WhenRootReplyArrives_ReturnsInitRequestAndEntersInitSent()
    {
        // Arrange
        var sut = new SerialLink(TimeProvider.System);
        sut.Start(0);
        var rootReply = new SerialLinkFrame(0x80, 0x21, []);

        // Act
        var result = sut.OnFrame(rootReply, 10);

        // Assert
        Assert.Equal([0x7E, 0x01, 0x43, 0x12, 0x00, 0xE1], result);
        Assert.Equal(LinkState.InitSent, sut.State);
    }

    [Fact]
    public void OnFrame_WhenInitReplyArrives_ReturnsNullAndEntersLinked()
    {
        // Arrange
        var sut = new SerialLink(TimeProvider.System);
        sut.Start(0);
        sut.OnFrame(new SerialLinkFrame(0x80, 0x21, []), 10);
        var initReply = new SerialLinkFrame(0x43, 0x21, [0x00]);

        // Act
        var result = sut.OnFrame(initReply, 20);

        // Assert
        Assert.Null(result);
        Assert.Equal(LinkState.Linked, sut.State);
    }

    [Fact]
    public void OnFrame_WhenFramePairIsNotDeviceToHost_ReturnsNullAndKeepsState()
    {
        // Arrange
        var sut = new SerialLink(TimeProvider.System);
        sut.Start(0);
        sut.OnFrame(new SerialLinkFrame(0x80, 0x21, []), 10);
        var foreign = new SerialLinkFrame(0x43, 0x12, [0x00]);

        // Act
        var result = sut.OnFrame(foreign, 20);

        // Assert
        Assert.Null(result);
        Assert.Equal(LinkState.InitSent, sut.State);
    }

    [Fact]
    public void OnFrame_WhenLinkIsIdle_ReturnsNull()
    {
        // Arrange
        var sut = new SerialLink(TimeProvider.System);
        var frame = new SerialLinkFrame(0x80, 0x21, []);

        // Act
        var result = sut.OnFrame(frame, 0);

        // Assert
        Assert.Null(result);
        Assert.Equal(LinkState.Idle, sut.State);
    }

    [Fact]
    public void OnTick_WhenRootSentAndTimeoutElapsed_MovesToLost()
    {
        // Arrange
        var sut = new SerialLink(TimeProvider.System);
        sut.Start(0);

        // Act
        var result = sut.OnTick(3 * TicksPerSecond);

        // Assert
        Assert.Equal(LinkState.Lost, result);
    }

    [Fact]
    public void OnTick_WhenTimeoutHasNotElapsed_KeepsCurrentState()
    {
        // Arrange
        var sut = new SerialLink(TimeProvider.System);
        sut.Start(0);

        // Act
        var result = sut.OnTick(1 * TicksPerSecond);

        // Assert
        Assert.Equal(LinkState.RootSent, result);
    }
}
