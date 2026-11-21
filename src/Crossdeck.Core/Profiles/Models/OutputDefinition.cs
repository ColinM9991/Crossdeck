using Crossdeck.Core.Mapping;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record OutputDefinition(
    string Id,
    IReadOnlyDictionary<string, SimRef> Sources,
    ExpressionSpec? Transform = null,
    ExpressionSpec? When = null,
    OutputValue? Fallback = null);