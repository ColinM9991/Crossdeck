using System.Collections.Immutable;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record InputDefinition(
    string Id,
    ImmutableDictionary<string, SimRef> Sources,
    ImmutableDictionary<string, InputEventBinding> Events,
    bool Sync = false);