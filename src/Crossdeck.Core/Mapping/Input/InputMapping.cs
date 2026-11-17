using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Mapping.Input;

public sealed record InputMapping(
    DeviceIdentity Device,
    InputId Control,
    IReadOnlyDictionary<string, IReadOnlyList<InputAction>> Actions,
    bool Sync = false);