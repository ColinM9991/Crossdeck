namespace Crossdeck.Core.Mapping.Expressions;

public sealed record ExpressionSuccess;

public sealed record ExpressionError(
    string Message,
    int? Position = null);

public union ValidationResult(
    ExpressionSuccess,
    IReadOnlyCollection<ExpressionError>);