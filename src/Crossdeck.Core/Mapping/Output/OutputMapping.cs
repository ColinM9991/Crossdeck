using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping;

public sealed record OutputMapping(
    IReadOnlyDictionary<string, SimRef> Sources,
    OutputSink Sink);