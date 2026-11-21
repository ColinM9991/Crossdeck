using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record InputDefinition(
    string Id,
    IReadOnlyDictionary<string, SimRef> Sources,
    IReadOnlyDictionary<string, InputEventBinding> Events,
    bool Sync = false);