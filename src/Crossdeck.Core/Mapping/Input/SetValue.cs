using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Input;

public closed record SetValue
{
    public sealed record Literal(SimValue Value) : SetValue;
    public sealed record Ref(SimRef Source) : SetValue;
    public sealed record Expr(ExpressionSpec Spec) : SetValue;
}