using Crossdeck.Devices.Moza.Protocol;
using Crossdeck.Testing;
using Xunit;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class SerialLinkDecoderTests
{
    [Theory, AutoNSubstituteData]
    public void DecodeFrame_ReturnsFrame_WhenFrameIsComplete(
        SerialLinkDecoder decoder)
    {
        // Arrange
        var frames = new List<SerialLinkFrame>();

        var input = new byte[] { 0x7E, 0x05, 0x43, 0x12, 0x7C, 0x23, 0x3C, 0x01, 0x7E, 0x7E, 0xBD };

        // Act
        decoder.Decode(input, frames);

        // Assert
        Assert.Single(frames);
    }

    public static TheoryData<int> SplitPoints => new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Theory]
    [MemberData(nameof(SplitPoints))]
    public void Decode_ReturnsFrame_WhenFrameIsSplitAcrossReads(int split)
    {
        // Arrange
        var decoder = new SerialLinkDecoder();
        var frames = new List<SerialLinkFrame>();

        var input = new byte[] { 0x7E, 0x05, 0x43, 0x12, 0x7C, 0x23, 0x3C, 0x01, 0x7E, 0x7E, 0xBD };

        // Act
        decoder.Decode(input.AsSpan(0, split), frames);
        var afterFirstRead = frames.Count;
        decoder.Decode(input.AsSpan(split), frames);

        // Assert
        Assert.Equal(0, afterFirstRead);
        var frame = Assert.Single(frames);
        Assert.Equal(0x43, frame.Command);
        Assert.Equal(0x12, frame.Pair);
        Assert.Equal(new byte[] { 0x7C, 0x23, 0x3C, 0x01, 0x7E }, frame.Payload);
    }

    [Fact]
    public void Decode_ReturnsAllFrames_WhenOneReadHoldsSeveralFrames()
    {
        // Arrange
        var decoder = new SerialLinkDecoder();
        var frames = new List<SerialLinkFrame>();

        var input = new byte[]
        {
            0x7E, 0x00, 0x80, 0x21, 0x2C, // handshake reply, no payload
            0x7E, 0x05, 0x43, 0x12, 0x7C, 0x23, 0x3C, 0x01, 0x7E, 0x7E, 0xBD, // escaped payload byte
            0x7E, 0x01, 0x43, 0x12, 0x00, 0xE1, // init, 1-byte payload
        };

        // Act
        decoder.Decode(input, frames);

        // Assert
        Assert.Collection(frames,
            f =>
            {
                Assert.Equal(0x80, f.Command);
                Assert.Equal(0x21, f.Pair);
                Assert.Empty(f.Payload);
            },
            f =>
            {
                Assert.Equal(0x43, f.Command);
                Assert.Equal(0x12, f.Pair);
                Assert.Equal(new byte[] { 0x7C, 0x23, 0x3C, 0x01, 0x7E }, f.Payload);
            },
            f =>
            {
                Assert.Equal(0x43, f.Command);
                Assert.Equal(0x12, f.Pair);
                Assert.Equal(new byte[] { 0x00 }, f.Payload);
            });
    }

    [Fact]
    public void Decode_ReturnsFrame_WhenChecksumIsEscaped()
    {
        var decoder = new SerialLinkDecoder();
        var frames = new List<SerialLinkFrame>();

        var input = new byte[] { 0x7E, 0x01, 0x43, 0x12, 0x9D, 0x7E, 0x7E };

        decoder.Decode(input, frames);

        var frame = Assert.Single(frames);
        Assert.Equal(new byte[] { 0x9D }, frame.Payload);
    }

    [Fact]
    public void Decode_ReturnsNextFrame_WhenPreviousFrameIsTruncated()
    {
        var decoder = new SerialLinkDecoder();
        var frames = new List<SerialLinkFrame>();

        var input = new byte[]
        {
            0x7E, 0x05, 0x43, 0x12, 0x7C, // cut off after 1 of 5 payload bytes
            0x7E, 0x01, 0x43, 0x12, 0x00, 0xE1, // good frame
        };

        decoder.Decode(input, frames);

        var frame = Assert.Single(frames);
        Assert.Equal(0x43, frame.Command);
        Assert.Equal(new byte[] { 0x00 }, frame.Payload);
    }

    [Fact]
    public void Decode_ReturnsNextFrame_WhenPreviousChecksumIsWrong()
    {
        var decoder = new SerialLinkDecoder();
        var frames = new List<SerialLinkFrame>();

        var input = new byte[]
        {
            0x7E, 0x01, 0x43, 0x12, 0x00, 0xE2, // bad sum (should be E1)
            0x7E, 0x00, 0x80, 0x21, 0x2C, // good frame, same read
        };

        decoder.Decode(input, frames);

        var frame = Assert.Single(frames);
        Assert.Equal(0x80, frame.Command);
        Assert.Equal(0x21, frame.Pair);
    }

    [Fact]
    public void Decode_SkipsFrame_WhenLengthIsOver64()
    {
        var decoder = new SerialLinkDecoder();
        var frames = new List<SerialLinkFrame>();

        byte[] input =
        [
            0x7E, 0xFF, .. Enumerable.Repeat((byte)0x00, 70), // length 255, more bytes than the buffer holds
            0x7E, 0x00, 0x80, 0x21, 0x2C, // good frame
        ];

        decoder.Decode(input, frames); // must not throw

        var frame = Assert.Single(frames);
        Assert.Equal(0x80, frame.Command);
    }
}