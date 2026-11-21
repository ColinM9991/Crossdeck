using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record DeviceSlot(
    DeviceIdentity? Assigned,
    IReadOnlyDictionary<InputId, InputDefinition> Inputs,
    IReadOnlyDictionary<OutputId, OutputDefinition> Outputs,
    DisplayRole? Screen = null);