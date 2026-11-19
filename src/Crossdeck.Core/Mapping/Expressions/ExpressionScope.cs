namespace Crossdeck.Core.Mapping.Expressions;

public sealed record ExpressionScope(
    IReadOnlyList<string> Names,
    ExpressionKind Kind);