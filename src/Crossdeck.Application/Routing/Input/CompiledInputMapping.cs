using System.Collections.Frozen;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing.Input;

internal sealed record CompiledInputBinding(
    FrozenDictionary<string, SimRef> Sources,
    FrozenDictionary<string, EventBinding> Events)
{
    public IEnumerable<SimRef> RequiredRefs
        => Sources
            .Values
            .Concat(Events.Values
                .SelectMany(e => e.Actions)
                .SelectMany(a => a.ReadRefs()));
}