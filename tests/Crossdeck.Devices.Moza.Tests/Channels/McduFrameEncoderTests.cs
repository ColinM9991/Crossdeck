using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.Moza.Channels;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Channels;

public sealed class McduFrameEncoderTests
{
    [Fact]
    public void ChangedRows_WhenScreensAreEqual_ReturnsNoRows()
    {
        // Arrange
        var screen = Screen('A');

        // Act
        var result = McduFrameEncoder.ChangedRows(screen, Screen('A'));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ChangedRows_WhenOneRowDiffers_ReturnsThatRowOnly()
    {
        // Arrange
        var previous = Screen('A');
        var current = Screen('A');
        SetRow(current, 3, 'X');

        // Act
        var result = McduFrameEncoder.ChangedRows(previous, current);

        // Assert
        Assert.Equal([3], result);
    }

    [Fact]
    public void ChangedRows_WhenSeveralRowsDiffer_ReturnsEachChangedRow()
    {
        // Arrange
        var previous = Screen('A');
        var current = Screen('A');
        SetRow(current, 0, 'X');
        SetRow(current, 13, 'Y');

        // Act
        var result = McduFrameEncoder.ChangedRows(previous, current);

        // Assert
        Assert.Equal([0, 13], result);
    }

    [Fact]
    public void Encode_WhenKeyframeIsRequested_ProducesCompressedKeyframePackage()
    {
        // Arrange
        var screen = Screen('A');
        var rows = McduFrameEncoder.AllRows;

        // Act
        var package = McduFrameEncoder.Encode(7, screen, rows, keyframe: true);

        // Assert
        var (id, payload) = ReadPackage(package);
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 3, id);

        Assert.Equal(2, payload[0]);
        Assert.Equal(7u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(1)));
        Assert.Equal(0x0B, payload[5]);

        var body = Decompress(payload[6..]);
        Assert.Equal(rows.Count, body[0]);
    }

    [Fact]
    public void Encode_WhenDeltaIsRequested_ProducesUncompressedDeltaPackage()
    {
        // Arrange
        var screen = Screen('A');

        // Act
        var package = McduFrameEncoder.Encode(9, screen, [2], keyframe: false);

        // Assert
        var (id, payload) = ReadPackage(package);
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 2, id);
        Assert.Equal(0x03, payload[5]);

        var body = payload[6..];
        Assert.Equal(1, body[0]);
        Assert.Equal(2, body[1]);
    }

    [Fact]
    public void InitConfig_WhenCalled_ProducesInitConfigPackageWithFixedConfiguration()
    {
        // Act
        var (id, payload) = ReadPackage(McduFrameEncoder.InitConfig());

        // Assert
        Assert.Equal(McduFrameEncoder.ClientCapabilityPackage - 1, id);
        Assert.Equal(
            [0x02, 0x0E, 0x00, 0x18, 0x00, 0x02, 0x00, 0x00, 0x5B, 0x23, 0xEE, 0x02],
            payload);
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

    private static (byte Id, byte[] Payload) ReadPackage(byte[] package)
    {
        var queue = new ByteQueue();
        queue.Append(package);
        if (!NetworkPackage.TryRead(queue, out var id, out var payload))
            throw new InvalidOperationException("package did not decode");
        return (id, payload);
    }

    private static byte[] Decompress(byte[] payload)
    {
        using var compressed = new System.IO.MemoryStream(payload, 4, payload.Length - 4);
        using var zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionMode.Decompress);
        using var stream = new System.IO.MemoryStream();
        zlib.CopyTo(stream);
        return stream.ToArray();
    }
}
