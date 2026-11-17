using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Input;

public record InputMapping(
    DeviceIdentity Device,
    InputId Control,
    IReadOnlyDictionary<string, IReadOnlyList<InputAction>> Actions,
    bool Sync = false);

public closed record InputAction
{
    public sealed record Command(
        SimRef Ref,
        CommandPhase Phase = CommandPhase.Trigger) : InputAction;

    public sealed record Set(SimRef Ref, SimValue Value) : InputAction;
}

public enum CommandPhase
{
    Trigger,
    Begin,
    End
}