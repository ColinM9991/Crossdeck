using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class ByteQueueTests
{
    [Fact]
    public void Append_WhenEmpty_IsANoOp()
    {
        // Arrange
        var queue = new ByteQueue();

        // Act
        queue.Append([]);

        // Assert
        Assert.Equal(0, queue.Length);
    }

    [Fact]
    public void Append_WhenDataIsAppended_IncreasesLengthAndExposesData()
    {
        // Arrange
        var queue = new ByteQueue();
        byte[] data = [0x01, 0x02, 0x03];

        // Act
        queue.Append(data);

        // Assert
        Assert.Equal(3, queue.Length);
        Assert.Equal(data, queue.Span.ToArray());
    }

    [Fact]
    public void Peek_WhenDataIsQueued_ReturnsFirstByte()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x7E, 0x00]);

        // Act
        var result = queue.Peek();

        // Assert
        Assert.Equal(0x7E, result);
    }

    [Fact]
    public void Consume_WhenCountIsWithinLength_RemovesThatManyBytes()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x01, 0x02, 0x03, 0x04]);

        // Act
        queue.Consume(2);

        // Assert
        Assert.Equal(2, queue.Length);
        Assert.Equal([0x03, 0x04], queue.Span.ToArray());
    }

    [Fact]
    public void Consume_WhenCountExceedsLength_ClearsTheQueue()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x01, 0x02]);

        // Act
        queue.Consume(10);

        // Assert
        Assert.Equal(0, queue.Length);
    }

    [Fact]
    public void Append_WhenDataExceedsInitialCapacity_GrowsTheBuffer()
    {
        // Arrange
        var queue = new ByteQueue();
        var data = Enumerable.Range(0, 300).Select(x => (byte)x).ToArray();

        // Act
        queue.Append(data);

        // Assert
        Assert.Equal(300, queue.Length);
        Assert.Equal(data, queue.Span.ToArray());
    }

    [Fact]
    public void Append_WhenDataSpansMultipleGrows_PreservesAllBytes()
    {
        // Arrange
        var queue = new ByteQueue();
        var first = Enumerable.Repeat((byte)0x11, 200).ToArray();
        var second = Enumerable.Repeat((byte)0x22, 200).ToArray();

        // Act
        queue.Append(first);
        queue.Append(second);

        // Assert
        byte[] expected = [.. first, .. second];
        Assert.Equal(400, queue.Length);
        Assert.Equal(expected, queue.Span.ToArray());
    }

    [Fact]
    public void Consume_ThenAppend_WhenBufferIsCompacted_PreservesRemainingData()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x01, 0x02, 0x03]);

        // Act
        queue.Consume(2);
        queue.Append([0x04, 0x05]);

        // Assert
        Assert.Equal(3, queue.Length);
        Assert.Equal([0x03, 0x04, 0x05], queue.Span.ToArray());
    }

    [Fact]
    public void Clear_WhenDataIsQueued_EmptiesTheQueue()
    {
        // Arrange
        var queue = new ByteQueue();
        queue.Append([0x01, 0x02]);

        // Act
        queue.Clear();

        // Assert
        Assert.Equal(0, queue.Length);
    }
}
