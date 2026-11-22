using System.Collections.Immutable;
using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record DeviceSlot(
    DeviceModel Model,
    DeviceIdentity? Assigned,
    ImmutableDictionary<InputId, InputDefinition> Inputs,
    ImmutableDictionary<OutputId, OutputDefinition> Outputs,
    DisplayRole? Screen = null);