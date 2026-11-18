using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Input;

public closed record InputAction
{
    public SimCommand? ToSimCommand() => this switch
    {
        Command command => command.Phase switch
        {
            CommandPhase.Trigger => new SimCommand.Trigger(command.Ref),
            CommandPhase.Begin => new SimCommand.Begin(command.Ref),
            CommandPhase.End => new SimCommand.End(command.Ref),
        },
        Set set => new SimCommand.Set(set.Ref, set.Value),
        Step => null,
    };

    public sealed record Command(
        SimRef Ref,
        CommandPhase Phase = CommandPhase.Trigger) : InputAction;

    public sealed record Set(SimRef Ref, SimValue Value) : InputAction;

    public sealed record Step(
        SimRef Ref,
        double Amount,
        double Min,
        double Max) : InputAction;
}