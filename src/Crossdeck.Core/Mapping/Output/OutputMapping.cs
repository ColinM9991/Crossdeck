using System.Collections.Immutable;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Output;

public sealed record OutputMapping(
    string Id,
    ImmutableDictionary<string, SimRef> Sources,
    OutputSink? Sink = null,
    ExpressionSpec? Transform = null,
    ExpressionSpec? When = null,
    OutputValue? Fallback = null);