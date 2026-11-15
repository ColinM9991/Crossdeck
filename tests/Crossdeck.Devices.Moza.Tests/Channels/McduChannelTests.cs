using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.Moza.Channels;
using Crossdeck.Devices.Moza.Protocol;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza.Tests.Channels;

public sealed class McduChannelTests
{
    private const long RetransmitStep = 6_000_000;
    private const long TicksPerSecond = 10_000_000;

    [Fact]
    public void Attach_WhenCalled_SendsInitConfig()
    {
        // Arrange
        var sut = CreateSut();
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);

        // Act
        sut.Attach(connection);
        var wires = StreamHarness.Outgoing(connection, RetransmitStep);

        // Assert
        var (id, _) = Assert.Single(ReadPackages(wires));
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 1, id);
    }

    [Fact]
    public void TrySend_WhenItIsTheFirstFrame_SendsKeyframeWithoutShowingThePage()
    {
        // Arrange
        var (settings, settingsConnection) = ReadySettingsChannel();
        var sut = new McduChannel(settings);
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);
        sut.Attach(connection);
        StreamHarness.Outgoing(connection, RetransmitStep);
        SendClientCapability(connection, pageIndex: 7);
        sut.Poll();

        // Act
        sut.TrySend(Screen('A'));
        var wires = StreamHarness.Outgoing(connection, 2 * RetransmitStep);
        var settingWires = StreamHarness.Outgoing(settingsConnection, 3 * RetransmitStep);

        // Assert
        var (id, payload) = Assert.Single(ReadPackages(wires));
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 3, id);
        Assert.Equal(1u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(1)));
        Assert.Empty(ReadSettingFrames(settingWires));
    }

    [Fact]
    public void TrySend_WhenSecondFrameArrives_SendsDeltaAndShowsMcduPage()
    {
        // Arrange
        var (settings, settingsConnection) = ReadySettingsChannel();
        var sut = new McduChannel(settings);
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);
        sut.Attach(connection);
        StreamHarness.Outgoing(connection, RetransmitStep);
        SendClientCapability(connection, pageIndex: 7);
        sut.Poll();
        var previous = Screen('A');
        sut.TrySend(previous);
        StreamHarness.Outgoing(connection, 2 * RetransmitStep);
        var current = Screen('A');
        SetRow(current, 5, 'X');

        // Act
        sut.TrySend(current);
        var wires = StreamHarness.Outgoing(connection, 3 * RetransmitStep);
        var settingWires = StreamHarness.Outgoing(settingsConnection, 4 * RetransmitStep);

        // Assert
        var (id, payload) = Assert.Single(ReadPackages(wires));
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 2, id);
        Assert.Equal(1, payload[6]);
        Assert.Equal(5, payload[7]);

        var setting = Assert.Single(ReadSettingFrames(settingWires));
        Assert.Equal(SettingsChannel.DisplayMode, setting.Id);
        Assert.Equal([SettingsChannel.McduDisplayMode], setting.Data);
    }

    [Fact]
    public void TrySend_WhenSecondFrameIsTheSameScreen_ShowsPageWithoutSending()
    {
        // Arrange
        var (settings, settingsConnection) = ReadySettingsChannel();
        var sut = new McduChannel(settings);
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);
        sut.Attach(connection);
        StreamHarness.Outgoing(connection, RetransmitStep);
        SendClientCapability(connection, pageIndex: 7);
        sut.Poll();
        var screen = Screen('A');
        sut.TrySend(screen);
        StreamHarness.Outgoing(connection, 2 * RetransmitStep);

        // Act
        sut.TrySend(screen);
        var wires = StreamHarness.Outgoing(connection, 3 * RetransmitStep);
        var settingWires = StreamHarness.Outgoing(settingsConnection, 4 * RetransmitStep);

        // Assert
        Assert.Empty(ReadPackages(wires));

        var setting = Assert.Single(ReadSettingFrames(settingWires));
        Assert.Equal(SettingsChannel.DisplayMode, setting.Id);
        Assert.Equal([SettingsChannel.McduDisplayMode], setting.Data);
    }

    [Fact]
    public void TrySend_WhenScreenIsUnchanged_SendsNothing()
    {
        // Arrange
        var sut = CreateSut();
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);
        sut.Attach(connection);
        StreamHarness.Outgoing(connection, RetransmitStep);
        SendClientCapability(connection, pageIndex: 7);
        sut.Poll();
        var screen = Screen('A');
        sut.TrySend(screen);
        StreamHarness.Outgoing(connection, 2 * RetransmitStep);

        // Act
        sut.TrySend(screen);
        var frames = StreamHarness.Outgoing(connection, 3 * RetransmitStep);

        // Assert
        Assert.Empty(frames);
    }

    [Fact]
    public void TrySend_WhenMoreThanSixRowsChanged_SendsKeyframe()
    {
        // Arrange
        var sut = CreateSut();
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);
        sut.Attach(connection);
        StreamHarness.Outgoing(connection, RetransmitStep);
        SendClientCapability(connection, pageIndex: 7);
        sut.Poll();
        var previous = Screen('A');
        sut.TrySend(previous);
        StreamHarness.Outgoing(connection, 2 * RetransmitStep);
        var current = Screen('A');
        for (var row = 0; row < 7; row++)
            SetRow(current, row, 'X');

        // Act
        sut.TrySend(current);
        var wires = StreamHarness.Outgoing(connection, 3 * RetransmitStep);

        // Assert
        var (id, _) = Assert.Single(ReadPackages(wires));
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 3, id);
    }

    [Fact]
    public void TrySend_WhenPageIndexIsNotKnown_DoesNotSend()
    {
        // Arrange
        var sut = CreateSut();
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);
        sut.Attach(connection);
        StreamHarness.Outgoing(connection, RetransmitStep);

        // Act
        sut.TrySend(Screen('A'));
        var frames = StreamHarness.Outgoing(connection, 2 * RetransmitStep);

        // Assert
        Assert.Empty(frames);
    }

    [Fact]
    public void TrySend_WhenConnectionIsNotIdle_DoesNotSend()
    {
        // Arrange
        var sut = CreateSut();
        var connection = StreamHarness.OpenedConnection(StreamMux.McduService, 9060, 0);
        sut.Attach(connection);
        SendClientCapability(connection, pageIndex: 7);

        // Act
        sut.TrySend(Screen('A'));
        var wires = StreamHarness.Outgoing(connection, RetransmitStep);

        // Assert
        var (id, _) = Assert.Single(ReadPackages(wires));
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 1, id);
    }

    private static McduChannel CreateSut()
    {
        var settings = new SettingsChannel(TimeProvider.System, Substitute.For<ILogger>());
        return new McduChannel(settings);
    }

    private static (SettingsChannel Settings, StreamConnection Connection) ReadySettingsChannel()
    {
        var settings = new SettingsChannel(TimeProvider.System, Substitute.For<ILogger>());
        var connection = StreamHarness.OpenedConnection(StreamMux.SettingsService, 9060, 0);
        settings.Attach(connection);
        connection.Inbound.Append([0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        settings.Poll(0);
        settings.OnTick(4 * TicksPerSecond);
        StreamHarness.Outgoing(connection, RetransmitStep);
        return (settings, connection);
    }

    private static void SendClientCapability(StreamConnection connection, int pageIndex)
    {
        var package = NetworkPackage.Encode(McduFrameEncoder.ClientCapabilityPackage,
            [0x00, 0x00, 0x00, 0x00, (byte)pageIndex]);
        connection.Inbound.Append(package);
    }

    private static List<(byte Id, byte[] Payload)> ReadPackages(List<byte[]> wires)
    {
        var queue = new ByteQueue();
        foreach (var wire in wires)
        {
            var inner = StreamHarness.DecodeWire(wire).Inner;
            queue.Append(inner[6..^Wire.CrcSize]);
        }

        var packages = new List<(byte Id, byte[] Payload)>();
        while (NetworkPackage.TryRead(queue, out var id, out var payload))
            packages.Add((id, payload));
        return packages;
    }

    private static List<(int Id, byte[] Data)> ReadSettingFrames(List<byte[]> wires)
    {
        var queue = new ByteQueue();
        foreach (var wire in wires)
        {
            var inner = StreamHarness.DecodeWire(wire).Inner;
            queue.Append(inner[6..^Wire.CrcSize]);
        }

        var frames = new List<(int Id, byte[] Data)>();
        while (true)
        {
            var result = SettingFrame.TryRead(queue, out var id, out var data);
            if (result == SettingReadResult.Incomplete)
                break;
            if (result == SettingReadResult.Frame)
                frames.Add((id, data));
        }

        return frames;
    }

    private static TextScreen Screen(char character) =>
        new(
            [
                .. Enumerable.Repeat(
                    new TextCell(character, TextColour.White, TextColour.Black, TextSize.Large),
                    TextScreen.CellsCount)
            ]);

    private static void SetRow(TextScreen screen, int row, char character)
    {
        var cell = screen.Cells[row * TextScreen.Columns];
        screen.Cells[row * TextScreen.Columns] = cell with { Character = character };
    }
}
