using Crossdeck.Core.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record Profile(
    Guid Id,
    string Name,
    Simulator Simulator,
    AircraftMatch Aircraft,
    IReadOnlyDictionary<string, VariableDefinition> Variables,
    IReadOnlyDictionary<string, DeviceSlot> Devices,
    IReadOnlyDictionary<string, OutputDefinition> Outputs);