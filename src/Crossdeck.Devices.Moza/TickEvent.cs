namespace Crossdeck.Devices.Moza;

internal sealed record TickEvent : SessionEvent
{
    private TickEvent()
    {
    }
    
    public static Lazy<TickEvent> Instance { get; } = new(() => new TickEvent());
}

internal sealed record BytesReceivedEvent(byte[] Payload) : SessionEvent;