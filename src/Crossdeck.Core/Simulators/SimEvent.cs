namespace Crossdeck.Core.Simulators;

public abstract record SimEvent
{
    public sealed record AircraftChanged(string? AircraftId) : SimEvent;

    public sealed record StatusChanged(SimStatus Status) : SimEvent;

    public sealed record ValueChanged(SimRef Ref, SimValue Value) : SimEvent;
}

public sealed record SimRef(string Scheme, string Key);

public abstract record SimValue
{
    public sealed record String(string Value) : SimValue;
    
    public sealed record Number(double Value) : SimValue;
}

public abstract record SimCommand
{
    public sealed record Trigger(SimRef Action);

    public sealed record Begin(SimRef Action);

    public sealed record End(SimRef Action);
    
    public sealed record Set(SimRef Target, SimValue Value);
}

public abstract record SimStatus
{
    public sealed record Connected : SimStatus;

    public sealed record Gone : SimStatus;
}