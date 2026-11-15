using System.Buffers.Binary;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class StreamConnectionTests
{
    private const long Retransmit = 6_000_000;
    private const long Heartbeat = 33_000_000;
    private const long Timeout = 100_000_000;

    [Fact]
    public void Constructor_WhenCreated_ExposesPortsAndInitialState()
    {
        // Arrange
        var syn1 = new Syn(0x1234, 9090, StreamConnection.HostVersion);

        // Act
        var sut = new StreamConnection(9010, 9060, syn1, 0);

        // Assert
        Assert.Equal(9010, sut.Service);
        Assert.Equal(9060, sut.LocalPort);
        Assert.Equal(9090, sut.PeerPort);
        Assert.Equal(StreamState.AwaitingAck, sut.State);
        Assert.True(sut.IsIdle);
        Assert.Equal(0, sut.Inbound.Length);
    }

    [Fact]
    public void Syn2_WhenCreated_TargetsPeerPortAndCarriesLocalPortAndVersion()
    {
        // Act
        var sut = new StreamConnection(9010, 9060, new Syn(1, 9090, StreamConnection.HostVersion), 0);
        var (_, _, inner) = StreamHarness.DecodeWire(sut.Syn2);

        // Assert
        Assert.Equal(Magic.Syn2, inner[3]);
        Assert.Equal(9060, BinaryPrimitives.ReadUInt16LittleEndian(inner.AsSpan(4)));
        Assert.Equal(9090, BinaryPrimitives.ReadUInt16BigEndian(inner.AsSpan(1)));
        Assert.Equal(unchecked((byte)~StreamConnection.HostVersion), inner[8]);
        Assert.Equal(StreamConnection.HostVersion, inner[9]);
    }

    [Fact]
    public void Next_WhenSyn2IsNotYetDue_ReturnsNull()
    {
        // Arrange
        var sut = new StreamConnection(9010, 9060, new Syn(1, 9090, StreamConnection.HostVersion), 0);

        // Act
        var result = sut.Next(0, TimeProvider.System);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Next_WhenRetransmitIsDue_ResendsSyn2()
    {
        // Arrange
        var sut = new StreamConnection(9010, 9060, new Syn(1, 9090, StreamConnection.HostVersion), 0);

        // Act
        var wire = sut.Next(Retransmit, TimeProvider.System);

        // Assert
        var (magic, isn, _) = StreamHarness.DecodeWire(wire!);
        Assert.Equal(Magic.Syn2, magic);
        Assert.Equal(9060, isn);
    }

    [Fact]
    public void Next_WhenRetriesAreExhausted_ThrowsFcdSessionLostException()
    {
        // Arrange
        var sut = new StreamConnection(9010, 9060, new Syn(1, 9090, StreamConnection.HostVersion), 0);
        var now = 0L;

        // Act
        for (var i = 0; i < 8; i++)
        {
            now += Retransmit;
            sut.Next(now, TimeProvider.System);
        }

        // Assert
        now += Retransmit;
        Assert.Throws<FcdSessionLostException>(() => sut.Next(now, TimeProvider.System));
    }

    [Fact]
    public void OnAck_WhenSyn2IsAcked_OpensTheConnection()
    {
        // Arrange
        var sut = new StreamConnection(9010, 9060, new Syn(1, 9090, StreamConnection.HostVersion), 0);

        // Act
        sut.OnAck(9060, Retransmit);

        // Assert
        Assert.Equal(StreamState.Open, sut.State);
    }

    [Fact]
    public void Send_WhenDataExceedsChunkSize_SplitsIntoChunks()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);
        var data = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();

        // Act
        sut.Send(data);
        var frames = StreamHarness.Outgoing(sut, Retransmit);

        // Assert
        Assert.Equal(2, frames.Count);

        var first = StreamHarness.DecodeWire(frames[0]).Inner[6..];
        var second = StreamHarness.DecodeWire(frames[1]).Inner[6..];

        var firstChunk = StripCrc(first);
        var secondChunk = StripCrc(second);
        Assert.Equal(data[..54], firstChunk);
        Assert.Equal(data[54..], secondChunk);
    }

    [Fact]
    public void OnRequest_WhenTransArrivesWithExpectedIsn_AppendsChunkToInboundAndReturnsAck()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);
        byte[] chunk = [0x42];

        // Act
        var ack = sut.OnRequest(Magic.Trans, 2, chunk, Retransmit);

        // Assert
        Assert.Equal(1, sut.Inbound.Length);
        Assert.Equal(chunk, sut.Inbound.Span.ToArray());

        var (magic, isn, _) = StreamHarness.DecodeWire(ack);
        Assert.Equal(0xFC, magic);
        Assert.Equal(2, isn);
    }

    [Fact]
    public void OnRequest_WhenIsnIsUnexpected_IgnoresTheChunk()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);
        byte[] chunk = [0x42];

        // Act
        sut.OnRequest(Magic.Trans, 0, chunk, Retransmit);

        // Assert
        Assert.Equal(0, sut.Inbound.Length);
    }

    [Fact]
    public void OnRequest_WhenFinArrives_MarksPeerFinishedAndQueuesOwnFin()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);

        // Act
        sut.OnRequest(Magic.Fin, 2, [], Retransmit);
        var frames = StreamHarness.Outgoing(sut, 2 * Retransmit);

        // Assert
        Assert.True(sut.PeerFinished);

        var (magic, _, _) = StreamHarness.DecodeWire(frames[^1]);
        Assert.Equal(Magic.Fin, magic);
    }

    [Fact]
    public void IsClosed_WhenBothSidesFinishedAndFinIsAcked_ReturnsTrue()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);
        sut.Send([0x01]);
        sut.OnRequest(Magic.Fin, 2, [], Retransmit);
        sut.Finish();

        // Act
        var frames = StreamHarness.Outgoing(sut, 2 * Retransmit);

        // Assert
        Assert.True(sut.IsClosed);
        Assert.NotEmpty(frames);
    }

    [Fact]
    public void Next_WhenIdleForHeartbeatInterval_SendsEmptyTrans()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);

        // Act
        var wire = sut.Next(Heartbeat + Retransmit, TimeProvider.System);

        // Assert
        var (magic, _, inner) = StreamHarness.DecodeWire(wire!);
        Assert.Equal(Magic.Trans, magic);
        Assert.Equal(Wire.CrcSize, inner.Length - 6);
    }

    [Fact]
    public void TimedOut_WhenLastAckIsOlderThanTimeout_ReturnsTrue()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);

        // Act
        var result = sut.TimedOut(Timeout, TimeProvider.System);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TimedOut_WhenTimeoutHasNotElapsed_ReturnsFalse()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);

        // Act
        var result = sut.TimedOut(Timeout - 1, TimeProvider.System);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Finish_WhenCalledTwice_QueuesSingleFinFrame()
    {
        // Arrange
        var sut = StreamHarness.OpenedConnection(9010, 9060, 0);

        // Act
        sut.Finish();
        sut.Finish();
        var frames = StreamHarness.Outgoing(sut, Retransmit);

        // Assert
        var finFrames = frames.Count(wire => StreamHarness.DecodeWire(wire).Magic == Magic.Fin);
        Assert.Equal(1, finFrames);
    }

    private static byte[] StripCrc(byte[] body) => body[..^Wire.CrcSize];
}
