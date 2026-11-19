using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Input;

public sealed record InputMapping(
    DeviceIdentity Device,
    InputId Control,
    IReadOnlyDictionary<string, SimRef> Sources,
    IReadOnlyDictionary<string, InputEventBinding> Actions,
    bool Sync = false);