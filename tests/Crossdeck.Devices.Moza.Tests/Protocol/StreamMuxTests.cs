using System.Buffers.Binary;
using Crossdeck.Devices.Moza.Protocol;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza.Tests.Protocol;

public sealed class StreamMuxTests
{
    private const long Now = 0;
    private const long Retransmit = 6_000_000;
    private const long Timeout = 100_000_000;

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenSyn1IsForUnknownService_IgnoresIt(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var outgoing = new List<byte[]>();
        var payload = Syn1Payload(StreamMux.TelemetryService + 1, 9090);

        // Act
        sut.OnTunnel(payload, Now, outgoing);

        // Assert
        Assert.Empty(outgoing);
        Assert.False(sut.HasConnections);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenSyn1IsForKnownService_RespondsWithSyn2ForTheAnnouncedPort(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var outgoing = new List<byte[]>();
        var payload = Syn1Payload(StreamMux.TelemetryService, 9090);

        // Act
        sut.OnTunnel(payload, Now, outgoing);

        // Assert
        var (magic, isn, inner) = StreamHarness.DecodeWire(Assert.Single(outgoing));
        Assert.Equal(Magic.Syn2, magic);
        Assert.Equal(9090, BinaryPrimitives.ReadUInt16BigEndian(inner.AsSpan(1)));
        Assert.Equal(9090, isn);
        Assert.True(sut.HasConnections);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenPreferredPortIsReserved_AllocatesTheNextFreePort(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var outgoing = new List<byte[]>();
        var payload = Syn1Payload(StreamMux.McduService, StreamMux.McduService + 1);

        // Act
        sut.OnTunnel(payload, Now, outgoing);

        // Assert
        var (_, isn, _) = StreamHarness.DecodeWire(Assert.Single(outgoing));
        Assert.Equal(StreamMux.McduService + 2, isn);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenSyn1VersionIsUnsupported_IgnoresIt(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var outgoing = new List<byte[]>();
        var payload = Syn1Payload(StreamMux.TelemetryService, 9090, version: 1);

        // Act
        sut.OnTunnel(payload, Now, outgoing);

        // Assert
        Assert.Empty(outgoing);
        Assert.False(sut.HasConnections);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenMuxIsClosing_IgnoresSyn1(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        sut.Closing = true;
        var outgoing = new List<byte[]>();
        var payload = Syn1Payload(StreamMux.TelemetryService, 9090);

        // Act
        sut.OnTunnel(payload, Now, outgoing);

        // Assert
        Assert.Empty(outgoing);
        Assert.False(sut.HasConnections);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenSyn1IsRepeated_ResendsExistingSyn2WithoutNewConnection(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var outgoing = new List<byte[]>();
        var payload = Syn1Payload(StreamMux.TelemetryService, 9090);
        sut.OnTunnel(payload, Now, outgoing);
        var firstIsn = StreamHarness.DecodeWire(outgoing[0]).Isn;
        outgoing.Clear();

        // Act
        sut.OnTunnel(payload, Now, outgoing);

        // Assert
        var (magic, isn, _) = StreamHarness.DecodeWire(Assert.Single(outgoing));
        Assert.Equal(Magic.Syn2, magic);
        Assert.Equal(firstIsn, isn);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenTransHasBadCrc_DropsItWithoutForwarding(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var connection = OpenConnection(sut, StreamMux.TelemetryService);
        var body = Wire.WithCrc([0x42]);
        body[^1] ^= 0xFF;
        var outgoing = new List<byte[]>();

        // Act
        sut.OnTunnel(TransPayload(connection.LocalPort, 2, body), Now, outgoing);

        // Assert
        Assert.Empty(outgoing);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenTransHasValidCrc_ReturnsAckForTheChunk(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var connection = OpenConnection(sut, StreamMux.TelemetryService);
        var outgoing = new List<byte[]>();

        // Act
        sut.OnTunnel(TransPayload(connection.LocalPort, 2, Wire.WithCrc([0x42])), Now, outgoing);

        // Assert
        var (magic, isn, _) = StreamHarness.DecodeWire(Assert.Single(outgoing));
        Assert.Equal(0xFC, magic);
        Assert.Equal(2, isn);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenAckArrives_RaisesOpenedEventWhenTheConnectionOpens(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var outgoing = new List<byte[]>();
        sut.OnTunnel(Syn1Payload(StreamMux.TelemetryService, 9090), Now, outgoing);
        var (_, syn2Isn, _) = StreamHarness.DecodeWire(outgoing[0]);

        StreamConnection? opened = null;
        sut.Opened += c => opened = c;

        // Act
        sut.OnTunnel(AckPayload(syn2Isn, syn2Isn), Now, outgoing);

        // Assert
        Assert.NotNull(opened);
        Assert.Equal(StreamState.Open, opened!.State);
    }

    [Theory, AutoNSubstituteData]
    public void OnTunnel_WhenBothSidesFinished_RemovesTheConnection(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var connection = OpenConnection(sut, StreamMux.TelemetryService);

        sut.OnTunnel(FinPayload(connection.LocalPort, 2), Now, new List<byte[]>());
        var outgoing = new List<byte[]>();
        sut.Next(Retransmit, outgoing);
        var finIsn = StreamHarness.DecodeWire(Assert.Single(outgoing)).Isn;

        // Act
        sut.OnTunnel(AckPayload(connection.LocalPort, finIsn), Now, new List<byte[]>());

        // Assert
        Assert.False(sut.HasConnections);
    }

    [Theory, AutoNSubstituteData]
    public void Next_WhenConnectionHasQueuedData_ForwardsTheFrame(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        var connection = OpenConnection(sut, StreamMux.TelemetryService);
        connection.Send([0x42]);
        var outgoing = new List<byte[]>();

        // Act
        sut.Next(Retransmit, outgoing);

        // Assert
        var (magic, _, inner) = StreamHarness.DecodeWire(Assert.Single(outgoing));
        Assert.Equal(Magic.Trans, magic);
        Assert.Equal([0x42], StripCrc(inner[6..]));
    }

    [Theory, AutoNSubstituteData]
    public void CheckTimeouts_WhenConnectionIsSilentForTenSeconds_Throws(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        OpenConnection(sut, StreamMux.TelemetryService);

        // Act
        var exception = Assert.Throws<FcdSessionLostException>(() => sut.CheckTimeouts(Timeout));

        // Assert
        Assert.Contains($"service {StreamMux.TelemetryService}", exception.Message);
    }

    [Theory, AutoNSubstituteData]
    public void FinishAll_WhenCalled_QueuesFinOnEveryConnection(ILogger logger)
    {
        // Arrange
        var sut = CreateMux(logger);
        OpenConnection(sut, StreamMux.TelemetryService);
        var outgoing = new List<byte[]>();

        // Act
        sut.FinishAll();
        sut.Next(Retransmit, outgoing);

        // Assert
        var (magic, _, _) = StreamHarness.DecodeWire(Assert.Single(outgoing));
        Assert.Equal(Magic.Fin, magic);
    }

    private static StreamMux CreateMux(ILogger logger) => new(TimeProvider.System, logger);

    private static StreamConnection OpenConnection(StreamMux sut, ushort service)
    {
        var outgoing = new List<byte[]>();
        sut.OnTunnel(Syn1Payload(service, 9090), Now, outgoing);

        var (_, syn2Isn, _) = StreamHarness.DecodeWire(outgoing[0]);
        StreamConnection? connection = null;
        sut.Opened += c => connection = c;
        sut.OnTunnel(AckPayload(syn2Isn, syn2Isn), Now, outgoing);
        Assert.NotNull(connection);
        return connection!;
    }

    private static byte[] StripCrc(byte[] body) => body[..^Wire.CrcSize];

    private static byte[] Syn1Payload(ushort service, ushort announcedPort, byte version = 2) =>
        StreamHarness.TunnelPayload(
            Wire.Request(service, Magic.Syn1, 1,
                [(byte)announcedPort, (byte)(announcedPort >> 8), unchecked((byte)~version), version]));

    private static byte[] AckPayload(ushort destination, ushort isn) =>
        StreamHarness.TunnelPayload(Wire.Ack(destination, isn));

    private static byte[] TransPayload(ushort destination, ushort isn, byte[] body) =>
        StreamHarness.TunnelPayload(Wire.Request(destination, Magic.Trans, isn, body));

    private static byte[] FinPayload(ushort destination, ushort isn) =>
        StreamHarness.TunnelPayload(Wire.Request(destination, Magic.Fin, isn, []));
}
