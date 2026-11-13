namespace Crossdeck.Devices.Moza.Protocol;

internal sealed class SerialLink(TimeProvider clock)
{
    private static readonly byte[] RootRequest = [0x7E, 0x00, 0x00, 0x12, 0x9D];
    private static readonly byte[] InitRequest = [0x7E, 0x01, 0x43, 0x12, 0x00, 0xE1];
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    private long _sentAt;

    public LinkState State { get; private set; } = LinkState.Idle;

    public byte[] Start(long now)
    {
        State = LinkState.RootSent;
        _sentAt = now;
        return RootRequest;
    }

    public byte[]? OnFrame(SerialLinkFrame f, long now)
    {
        var isRootReply = f is { Command: 0x80, Pair: 0x21, Payload.Length: 0 };

        switch (State)
        {
            case LinkState.RootSent when isRootReply:
                State = LinkState.InitSent;
                _sentAt = now;
                return InitRequest;

            case LinkState.InitSent when f.Pair == 0x21 && !isRootReply:
                State = LinkState.Linked;
                return null;

            default:
                return null;
        }
    }

    public LinkState OnTick(long now)
    {
        if (State is LinkState.RootSent or LinkState.InitSent
            && clock.GetElapsedTime(_sentAt, now) >= Timeout)
        {
            State = LinkState.Lost;
        }

        return State;
    }
}