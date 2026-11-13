namespace Crossdeck.Devices.Moza.Protocol;

internal sealed class StreamConnection
{
    public const byte HostVersion = 2;
    private const int MaxChunk = 54;
    private const int MaxRetries = 8;

    private static readonly TimeSpan Retransmit = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan Heartbeat = TimeSpan.FromMilliseconds(3300);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly Queue<(byte Magic, byte[] Chunk)> _outbox = new();

    private (ushort Isn, byte Magic, byte[] Wire)? _inFlight;
    private ushort _peerIsn;
    private ushort _nextIsn;
    private int _retries;

    private long _sentAt;
    private long _lastHeard;
    private long _lastSent;

    private bool _finQueued;
    private bool _finAcked;

    public StreamConnection(ushort service, ushort localPort, Syn syn1, long now)
    {
        Service = service;
        LocalPort = localPort;
        PeerPort = syn1.AnnouncedPort;
        Syn2 = Wire.Request(PeerPort, Magic.Syn2, LocalPort,
            [(byte)LocalPort, (byte)(LocalPort >> 8), unchecked((byte)~HostVersion), HostVersion]);
        _peerIsn = syn1.Isn;
        _nextIsn = (ushort)(localPort + 1);
        _lastHeard = now;
        _sentAt = now;
    }

    public ushort Service { get; }

    public ushort LocalPort { get; }

    public ushort PeerPort { get; }

    public byte[] Syn2 { get; }

    public StreamState State { get; private set; } = StreamState.AwaitingAck;

    public bool PeerFinished { get; private set; }

    public bool IsClosed => _finAcked && PeerFinished;

    public bool IsIdle => _inFlight is null && _outbox.Count == 0;

    public ByteQueue Inbound { get; } = new();

    public void Send(ReadOnlySpan<byte> app)
    {
        if (_finQueued)
            return;

        for (var i = 0; i < app.Length; i += MaxChunk)
            _outbox.Enqueue((Magic.Trans, app.Slice(i, Math.Min(MaxChunk, app.Length - i)).ToArray()));
    }

    public void Finish()
    {
        if (_finQueued)
            return;

        _finQueued = true;
        _outbox.Enqueue((Magic.Fin, []));
    }

    public byte[] OnRequest(byte magic, ushort isn, ReadOnlySpan<byte> chunk, long now)
    {
        if (isn == (ushort)(_peerIsn + 1))
            AcceptRequest(magic, isn, chunk, now);

        return Wire.Ack(PeerPort, _peerIsn);
    }

    public void OnAck(ushort isn, long now)
    {
        _lastHeard = now;

        if (State == StreamState.AwaitingAck)
            AcceptSyn2Ack(isn);
        else if (_inFlight?.Isn == isn)
            CompleteInFlight();
    }

    public byte[]? Next(long now, TimeProvider clock)
    {
        if (State == StreamState.AwaitingAck)
            return Resend(Syn2, now, clock);

        if (_inFlight is { } inFlight)
            return Resend(inFlight.Wire, now, clock);

        QueueHeartbeatIfIdle(now, clock);

        return _outbox.TryDequeue(out var next) ? Transmit(next.Magic, next.Chunk, now) : null;
    }

    public bool TimedOut(long now, TimeProvider clock) =>
        clock.GetElapsedTime(_lastHeard, now) >= Timeout;

    private void AcceptRequest(byte magic, ushort isn, ReadOnlySpan<byte> chunk, long now)
    {
        _peerIsn = isn;
        _lastHeard = now;

        switch (magic)
        {
            case Magic.Trans:
                Inbound.Append(chunk);
                break;
            case Magic.Fin:
                PeerFinished = true;
                Finish();
                break;
        }
    }

    private void AcceptSyn2Ack(ushort isn)
    {
        if (isn != LocalPort)
            return;

        State = StreamState.Open;
        _retries = 0;
    }

    private void CompleteInFlight()
    {
        if (_inFlight?.Magic == Magic.Fin)
            _finAcked = true;

        _inFlight = null;
        _retries = 0;
    }

    private byte[]? Resend(byte[] wire, long now, TimeProvider clock)
    {
        if (clock.GetElapsedTime(_sentAt, now) < Retransmit)
            return null;

        if (++_retries > MaxRetries)
            throw new FcdSessionLostException($"No ACK on service {Service} after {MaxRetries} retries.");

        _sentAt = now;
        return wire;
    }

    private void QueueHeartbeatIfIdle(long now, TimeProvider clock)
    {
        if (_outbox.Count > 0 || PeerFinished || _finQueued)
            return;

        if (clock.GetElapsedTime(Math.Max(_lastHeard, _lastSent), now) >= Heartbeat)
            _outbox.Enqueue((Magic.Trans, []));
    }

    private byte[] Transmit(byte magic, byte[] chunk, long now)
    {
        var isn = _nextIsn++;
        var body = magic == Magic.Trans ? Wire.WithCrc(chunk) : [];
        var wire = Wire.Request(PeerPort, magic, isn, body);

        _inFlight = (isn, magic, wire);
        _sentAt = now;
        _lastSent = now;
        return wire;
    }
}