namespace Crossdeck.Core.Mapping.Expressions;

public union ValidationResult(
    ExpressionSuccess,
    ExpressionError)
{
    public static ValidationResult Failure(string errorMessage) => new ExpressionError(errorMessage);

    public static ValidationResult Success() => new ExpressionSuccess();
}
public sealed record ExpressionSuccess;

public sealed record ExpressionError(
    string Message,
    int? Position = null);
