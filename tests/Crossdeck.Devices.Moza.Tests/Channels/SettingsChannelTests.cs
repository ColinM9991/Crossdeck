using Crossdeck.Devices.Moza.Channels;
using Crossdeck.Devices.Moza.Protocol;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza.Tests.Channels;

public sealed class SettingsChannelTests
{
    private const long SyncWindow = 40_000_000;
    private const long RetransmitStep = 6_000_000;

    [Theory, AutoNSubstituteData]
    public void Poll_WhenPreambleIsInvalid_ThrowsFcdSessionLostException(ILogger logger)
    {
        // Arrange
        var sut = CreateSut(logger);
        var connection = StreamHarness.OpenedConnection(StreamMux.SettingsService, 9060, 0);
        sut.Attach(connection);
        connection.Inbound.Append([0x07, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00]);

        // Act / Assert
        Assert.Throws<FcdSessionLostException>(() => sut.Poll(0));
    }

    [Theory, AutoNSubstituteData]
    public void Poll_WhenPreambleIsComplete_SendsTimeAndVersionFrames(ILogger logger)
    {
        // Arrange
        var sut = CreateSut(logger);
        var connection = StreamHarness.OpenedConnection(StreamMux.SettingsService, 9060, 0);
        sut.Attach(connection);
        connection.Inbound.Append([0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

        // Act
        sut.Poll(0);
        var frames = DrainFrames(connection, RetransmitStep);

        // Assert
        var ids = frames.Select(frame => frame.Id).ToArray();
        Assert.Equal([SettingsChannel.Time, SettingsChannel.ProtocolVersion], ids);
    }

    [Theory, AutoNSubstituteData]
    public void ShowMcduPage_WhenNotReady_DoesNotWriteDisplayMode(ILogger logger)
    {
        // Arrange
        var sut = CreateSut(logger);
        var connection = StreamHarness.OpenedConnection(StreamMux.SettingsService, 9060, 0);
        sut.Attach(connection);
        connection.Inbound.Append([0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        sut.Poll(0);

        // Act
        sut.ShowMcduPage();
        var frames = DrainFrames(connection, RetransmitStep);

        // Assert
        var ids = frames.Select(frame => frame.Id).ToArray();
        Assert.DoesNotContain(SettingsChannel.DisplayMode, ids);
    }

    [Theory, AutoNSubstituteData]
    public void ShowMcduPage_WhenReady_WritesMcduDisplayMode(ILogger logger)
    {
        // Arrange
        var sut = CreateSut(logger);
        var connection = StreamHarness.OpenedConnection(StreamMux.SettingsService, 9060, 0);
        sut.Attach(connection);
        connection.Inbound.Append([0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        sut.Poll(0);
        sut.OnTick(SyncWindow);
        DrainFrames(connection, RetransmitStep);

        // Act
        sut.ShowMcduPage();
        var frames = DrainFrames(connection, 2 * RetransmitStep);

        // Assert
        var setting = frames[^1];
        Assert.Equal(SettingsChannel.DisplayMode, setting.Id);
        Assert.Equal([SettingsChannel.McduDisplayMode], setting.Data);
    }

    [Theory, AutoNSubstituteData]
    public void RestoreDisplayMode_WhenPageWasShown_WritesBackThePreviousMode(ILogger logger)
    {
        // Arrange
        var sut = CreateSut(logger);
        var connection = StreamHarness.OpenedConnection(StreamMux.SettingsService, 9060, 0);
        sut.Attach(connection);
        connection.Inbound.Append([0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        sut.Poll(0);
        sut.OnTick(SyncWindow);
        connection.Inbound.Append(SettingFrame.Encode(SettingsChannel.DisplayMode, [0x05]));
        sut.Poll(SyncWindow);
        DrainFrames(connection, RetransmitStep);
        sut.ShowMcduPage();
        DrainFrames(connection, 2 * RetransmitStep);

        // Act
        sut.RestoreDisplayMode();
        var frames = DrainFrames(connection, 3 * RetransmitStep);

        // Assert
        var setting = frames[^1];
        Assert.Equal(SettingsChannel.DisplayMode, setting.Id);
        Assert.Equal([0x05], setting.Data);
    }

    private static SettingsChannel CreateSut(ILogger logger) => new(TimeProvider.System, logger);

    private static List<(int Id, byte[] Data)> DrainFrames(StreamConnection connection, long start)
    {
        var frames = StreamHarness.Outgoing(connection, start);
        return frames
            .Select(frame => ReadSettingFrame(frame))
            .ToList();
    }

    private static (int Id, byte[] Data) ReadSettingFrame(byte[] wire)
    {
        var inner = StreamHarness.DecodeWire(wire).Inner;
        var queue = new ByteQueue();
        queue.Append(inner[6..^Wire.CrcSize]);
        var result = SettingFrame.TryRead(queue, out var frameId, out var frameData);
        if (result != SettingReadResult.Frame)
            throw new InvalidOperationException($"frame did not decode: {result}");
        return (frameId, frameData);
    }
}
