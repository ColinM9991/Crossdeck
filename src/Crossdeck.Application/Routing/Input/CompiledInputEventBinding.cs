using Crossdeck.Core.Mapping.Expressions;

namespace Crossdeck.Application.Routing.Input;

internal sealed record EventBinding(
    IReadOnlyList<BoundAction> Actions,
    IValueTransform? Guard = null);