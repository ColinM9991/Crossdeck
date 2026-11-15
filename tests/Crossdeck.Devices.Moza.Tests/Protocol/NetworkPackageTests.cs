using System.Buffers.Binary;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class NetworkPackageTests
{
    [Fact]
    public void Encode_WhenPayloadIsGiven_ReturnsIdLengthAndPayload()
    {
        // Act
        var result = NetworkPackage.Encode(0x12, [0x01, 0x02]);

        // Assert
        byte[] expected =
        [
            0x12, 0x02, 0x00, 0x00, 0x00,
            0x01, 0x02
        ];
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryRead_WhenPackageIsComplete_ReturnsIdAndPayloadAndConsumesTheQueue()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append(NetworkPackage.Encode(0x33, [0x0A, 0x0B]));

        // Act
        var read = NetworkPackage.TryRead(queue, out var id, out var payload);

        // Assert
        Assert.True(read);
        Assert.Equal(0x33, id);
        Assert.Equal([0x0A, 0x0B], payload);
        Assert.Equal(0, queue.Length);
    }

    [Fact]
    public void TryRead_WhenHeaderIsIncomplete_ReturnsFalseWithDefaults()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x12, 0x01, 0x00, 0x00]);

        // Act
        var read = NetworkPackage.TryRead(queue, out var id, out var payload);

        // Assert
        Assert.False(read);
        Assert.Equal(0, id);
        Assert.Empty(payload);
        Assert.Equal(4, queue.Length);
    }

    [Fact]
    public void TryRead_WhenPayloadIsIncomplete_ReturnsFalseAndKeepsTheQueue()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x12, 0x08, 0x00, 0x00, 0x00, 0x01]);

        // Act
        var read = NetworkPackage.TryRead(queue, out _, out _);

        // Assert
        Assert.False(read);
        Assert.Equal(6, queue.Length);
    }

    [Fact]
    public void TryRead_WhenPayloadSizeExceedsMaximum_ThrowsFcdSessionLostException()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x12, 0x02, 0x00, 0x00, 0x01]);

        // Act
        var exception = Assert.Throws<FcdSessionLostException>(
            () => NetworkPackage.TryRead(queue, out _, out _));

        // Assert
        Assert.Contains($"exceeds {NetworkPackage.MaxPayloadSize}", exception.Message);
    }
}
