using System.Buffers.Binary;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza.Protocol;

internal sealed class StreamMux(TimeProvider clock, ILogger logger)
{
    public const ushort TelemetryService = 9010;
    public const ushort SettingsService = 9020;
    public const ushort McduService = 9050;

    private const int AckLength = 5;
    private const int RequestHeaderLength = 6;

    private static readonly ushort[] Services = [TelemetryService, SettingsService, McduService];
    private static readonly ushort[] ReservedPorts = [TelemetryService, SettingsService, 9030, McduService, 9051];

    private readonly Dictionary<ushort, StreamConnection> _byLocal = [];
    private readonly Dictionary<ushort, StreamConnection> _byService = [];

    public event Action<StreamConnection>? Opened;

    public bool Closing { get; set; }

    public bool HasConnections => _byLocal.Count > 0;

    public void OnTunnel(ReadOnlySpan<byte> payload, long now, List<byte[]> outgoing)
    {
        if (payload.Length < AckLength)
            return;

        var destination = BinaryPrimitives.ReadUInt16BigEndian(payload[1..]);
        switch (payload[0])
        {
            case Wire.AckCommand:
                OnAck(destination, BinaryPrimitives.ReadUInt16LittleEndian(payload[3..]), now);
                break;
            case Wire.RequestCommand when payload.Length >= RequestHeaderLength:
                OnRequest(destination, payload, now, outgoing);
                break;
        }
    }

    public void Next(long now, List<byte[]> outgoing)
    {
        foreach (var connection in _byLocal.Values)
        {
            if (connection.Next(now, clock) is { } frame)
                outgoing.Add(frame);
        }
    }

    public void CheckTimeouts(long now)
    {
        var timedOut = _byLocal.Values.FirstOrDefault(c => c.TimedOut(now, clock));
        if (timedOut is not null)
            throw new FcdSessionLostException($"Reliable Stream on service {timedOut.Service} timed out.");
    }

    public void FinishAll()
    {
        foreach (var connection in _byLocal.Values)
            connection.Finish();
    }

    private void OnAck(ushort destination, ushort isn, long now)
    {
        if (!_byLocal.TryGetValue(destination, out var connection))
            return;

        var wasAwaitingAck = connection.State == StreamState.AwaitingAck;
        connection.OnAck(isn, now);

        if (wasAwaitingAck && connection.State == StreamState.Open)
            Opened?.Invoke(connection);

        RemoveIfClosed(connection);
    }

    private void OnRequest(ushort destination, ReadOnlySpan<byte> payload, long now, List<byte[]> outgoing)
    {
        var magic = payload[3];
        var isn = BinaryPrimitives.ReadUInt16LittleEndian(payload[4..]);

        if (magic == Magic.Syn1)
        {
            OnSyn1(destination, payload, now, outgoing);
            return;
        }

        if (magic is not (Magic.Trans or Magic.Fin) || !_byLocal.TryGetValue(destination, out var connection))
            return;

        var body = payload[RequestHeaderLength..];
        if (magic == Magic.Trans && !Wire.CrcOk(body))
        {
            logger.LogWarning("Dropped TRANS with bad CRC on service {Service}, ISN {Isn}", connection.Service, isn);
            return;
        }

        var chunk = magic == Magic.Trans ? body[..^Wire.CrcSize] : default;
        outgoing.Add(connection.OnRequest(magic, isn, chunk, now));
        RemoveIfClosed(connection);
    }

    private void OnSyn1(ushort service, ReadOnlySpan<byte> payload, long now, List<byte[]> outgoing)
    {
        if (!Syn.TryParse(payload, out var syn1))
        {
            logger.LogWarning("Dropped malformed SYN1 for port {Port}", service);
            return;
        }

        if (Closing || !Services.Contains(service))
            return;

        if (syn1.Version < StreamConnection.HostVersion)
        {
            logger.LogWarning("Ignored SYN1 for service {Service} with unsupported version {Version}", service,
                syn1.Version);
            return;
        }

        if (IsRepeatedSyn1(service, syn1, out var pending))
        {
            outgoing.Add(pending.Syn2);
            return;
        }

        var connection = new StreamConnection(service, AllocatePort(syn1.AnnouncedPort), syn1, now);
        _byLocal[connection.LocalPort] = connection;
        _byService[service] = connection;
        outgoing.Add(connection.Syn2);
    }

    private bool IsRepeatedSyn1(ushort service, Syn syn1, out StreamConnection existing)
    {
        if (!_byService.TryGetValue(service, out existing!))
            return false;

        if (existing.State == StreamState.AwaitingAck && existing.PeerPort == syn1.AnnouncedPort)
            return true;

        Remove(existing);
        return false;
    }

    private ushort AllocatePort(ushort preferred)
    {
        var port = preferred;
        while (ReservedPorts.Contains(port) || _byLocal.ContainsKey(port))
        {
            port++;
            if (port == preferred)
                throw new FcdSessionLostException("No free Reliable Stream port.");
        }

        return port;
    }

    private void RemoveIfClosed(StreamConnection connection)
    {
        if (connection.IsClosed)
            Remove(connection);
    }

    private void Remove(StreamConnection connection)
    {
        _byLocal.Remove(connection.LocalPort);
        if (_byService.TryGetValue(connection.Service, out var current) && ReferenceEquals(current, connection))
            _byService.Remove(connection.Service);
    }
}