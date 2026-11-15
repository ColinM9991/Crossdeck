using System.Buffers.Binary;
using Crossdeck.Devices.Moza.Channels;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Channels;

public sealed class SettingFrameTests
{
    [Fact]
    public void Encode_WhenGivenIdAndData_ProducesHeaderLengthCrcAndBody()
    {
        // Arrange
        var id = 42;
        byte[] data = [0x01, 0x02];

        // Act
        var frame = SettingFrame.Encode(id, data);

        // Assert
        Span<byte> idBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(idBytes, id);
        byte[] body = [.. idBytes, .. data];
        Assert.Equal(0xFF, frame[0]);
        Assert.Equal((uint)body.Length, BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(1)));
        Assert.Equal(System.IO.Hashing.Crc32.HashToUInt32(body),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(5)));
        Assert.Equal(body, frame[9..]);
    }

    [Fact]
    public void TryRead_WhenHeaderIsIncomplete_ReturnsIncomplete()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append(SettingFrame.Encode(1, [0x01])[..7]);

        // Act
        var result = SettingFrame.TryRead(queue, out _, out _);

        // Assert
        Assert.Equal(SettingReadResult.Incomplete, result);
    }

    [Fact]
    public void TryRead_WhenBodyIsIncomplete_ReturnsIncomplete()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append(SettingFrame.Encode(1, [0x01, 0x02])[..12]);

        // Act
        var result = SettingFrame.TryRead(queue, out _, out _);

        // Assert
        Assert.Equal(SettingReadResult.Incomplete, result);
    }

    [Fact]
    public void TryRead_WhenBodyIsShorterThanFourBytes_SkipsOneByte()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0xFF, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01]);

        // Act
        var result = SettingFrame.TryRead(queue, out _, out _);

        // Assert
        Assert.Equal(SettingReadResult.Skipped, result);
        Assert.Equal(9, queue.Length);
    }

    [Fact]
    public void TryRead_WhenCrcIsCorrupted_ReturnsBadCrcAndConsumesTheFrame()
    {
        // Arrange
        var queue = new ByteQueue();
        var frame = SettingFrame.Encode(1, [0x01]);
        frame[6] ^= 0xFF;
        queue.Append(frame);

        // Act
        var result = SettingFrame.TryRead(queue, out _, out _);

        // Assert
        Assert.Equal(SettingReadResult.BadCrc, result);
        Assert.Equal(0, queue.Length);
    }

    [Fact]
    public void TryRead_WhenFrameIsComplete_ReturnsFrameWithIdAndData()
    {
        // Arrange
        var queue = new ByteQueue();
        var id = -7;
        byte[] data = [0x09, 0x08];
        queue.Append(SettingFrame.Encode(id, data));

        // Act
        var result = SettingFrame.TryRead(queue, out var readId, out var readData);

        // Assert
        Assert.Equal(SettingReadResult.Frame, result);
        Assert.Equal(id, readId);
        Assert.Equal(data, readData);
        Assert.Equal(0, queue.Length);
    }
}
