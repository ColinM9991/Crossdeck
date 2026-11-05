namespace Crossdeck.Core.Simulators.Models;

public abstract record SimEvent
{
    public sealed record AircraftChanged(string? AircraftId) : SimEvent;

    public sealed record StatusChanged(SimStatus Status) : SimEvent;

    public sealed record ValueChanged(SimRef Ref, SimValue Value) : SimEvent;
}

public sealed record SimRef(string Key);

public abstract record SimValue
{
    public sealed record String(string Value) : SimValue;
    
    public sealed record Number(double Value) : SimValue;
}

public abstract record SimCommand
{
    public sealed record Trigger(SimRef Action) : SimCommand;

    public sealed record Begin(SimRef Action) : SimCommand;

    public sealed record End(SimRef Action) : SimCommand;
    
    public sealed record Set(SimRef Target, SimValue Value) : SimCommand;
}

public abstract record SimStatus
{
    public sealed record Connected : SimStatus;

    public sealed record Disconnected : SimStatus;

    public sealed record Gone : SimStatus;
    
    public sealed record Ready : SimStatus;
}

public abstract record CommandResult
{
    public sealed record Success : CommandResult;

    public sealed record Failure : CommandResult;
    
    public sealed record Unsupported : CommandResult;
    
    public sealed record NoConnection : CommandResult;
    
    public sealed record NotReady : CommandResult;
}