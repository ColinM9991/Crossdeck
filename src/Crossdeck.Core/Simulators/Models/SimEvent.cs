namespace Crossdeck.Core.Simulators.Models;

public closed record SimEvent
{
    public sealed record AircraftChanged(string? AircraftId) : SimEvent;

    public sealed record StatusChanged(SimStatus Status) : SimEvent;

    public sealed record ValueChanged(SimRef Ref, SimValue Value) : SimEvent;

    public sealed record ValuesChanged(IReadOnlyList<ValueChanged> Changes) : SimEvent;
}