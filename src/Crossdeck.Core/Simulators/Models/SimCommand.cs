namespace Crossdeck.Core.Simulators.Models;

public closed record SimCommand
{
    public sealed record Trigger(SimRef Action) : SimCommand;

    public sealed record Begin(SimRef Action) : SimCommand;

    public sealed record End(SimRef Action) : SimCommand;

    public sealed record Set(SimRef Target, SimValue Value) : SimCommand;
}