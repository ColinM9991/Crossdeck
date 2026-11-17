using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Input;

public closed record InputAction
{
    public SimCommand ToSimCommand() => this switch
    {
        Set set => new SimCommand.Set(set.Ref, set.Value),
        Command { Phase: CommandPhase.Trigger } command => new SimCommand.Trigger(command.Ref),
        Command { Phase: CommandPhase.Begin } command => new SimCommand.Begin(command.Ref),
        Command { Phase: CommandPhase.End } command => new SimCommand.End(command.Ref),
    };

    public sealed record Command(
        SimRef Ref,
        CommandPhase Phase = CommandPhase.Trigger) : InputAction;

    public sealed record Set(SimRef Ref, SimValue Value) : InputAction;
}