using System.Collections.Immutable;

namespace Crossdeck.Core.Mapping.Input;

public sealed record InputEventBinding(
    ImmutableList<InputAction> Actions,
    ExpressionSpec? When = null);