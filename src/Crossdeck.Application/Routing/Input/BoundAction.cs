using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing.Input;

internal closed record BoundAction
{
    public IEnumerable<SimRef> ReadRefs() => this switch
    {
        SetRef s => [s.Source],
        SetExpr s => [s.Ref],
        _ => [],
    };

    public sealed record Command(SimRef Ref, CommandPhase Phase) : BoundAction;

    public sealed record SetLiteral(SimRef Ref, SimValue Value) : BoundAction;

    public sealed record SetRef(SimRef Ref, SimRef Source) : BoundAction;

    public sealed record SetExpr(SimRef Ref, IValueTransform Transform) : BoundAction;
}