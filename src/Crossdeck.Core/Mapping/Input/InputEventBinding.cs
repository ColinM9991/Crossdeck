namespace Crossdeck.Core.Mapping.Input;

public sealed record InputEventBinding(
    IReadOnlyList<InputAction> Actions,
    ExpressionSpec? Expr = null,
    ExpressionSpec? When = null);