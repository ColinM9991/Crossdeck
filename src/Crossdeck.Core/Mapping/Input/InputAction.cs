using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Input;

public closed record InputAction
{
    public sealed record Command(
        SimRef Ref,
        CommandPhase Phase = CommandPhase.Trigger) : InputAction;

    public sealed record Set(
        SimRef Ref,
        SetValue Value) : InputAction;
}