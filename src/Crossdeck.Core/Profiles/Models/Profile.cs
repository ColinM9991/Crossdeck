using System.Collections.Immutable;
using Crossdeck.Core.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record Profile(
    Guid Id,
    string Name,
    Simulator Simulator,
    AircraftMatch Aircraft,
    ImmutableDictionary<string, VariableDefinition> Variables,
    ImmutableDictionary<string, DeviceSlot> Devices,
    ImmutableDictionary<string, OutputDefinition> Outputs);