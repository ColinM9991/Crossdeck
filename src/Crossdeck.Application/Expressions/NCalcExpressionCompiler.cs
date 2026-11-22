using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
using NCalc;
using NCalc.Exceptions;

namespace Crossdeck.Application.Expressions;

internal sealed class NCalcExpressionCompiler : IExpressionCompiler
{
    public ValidationResult Validate(ExpressionSpec spec, ExpressionScope scope)
    {
        if (spec is not ExpressionSpec.Raw raw)
            return ValidationResult.Failure("Invalid expression type");

        if (string.IsNullOrWhiteSpace(raw.Expr))
            return ValidationResult.Failure("Expression cannot be empty");

        var expression = new Expression(raw.Expr);
        if (expression.HasErrors())
            return ValidationResult.Failure(expression.Error.Message);

        var unknownParameters = expression.GetParameterNames()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => !scope.Names.Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (unknownParameters.Length > 0)
            return ValidationResult.Failure($"Unknown parameters: {string.Join(", ", unknownParameters)}");

        if (!TryEvaluate(expression, scope.Names, out var result))
            return ValidationResult.Failure("Invalid expression");

        if (scope.Kind is ExpressionKind.Guard && result is not bool)
            return ValidationResult.Failure("A guard expression must evaluate to a true/false value");

        return ValidationResult.Success();
    }

    public IValueTransform Compile(ExpressionSpec spec, ExpressionScope scope)
    {
        if (Validate(spec, scope) is ExpressionError err)
            throw new InvalidOperationException(err.Message);

        var expression = new Expression(((ExpressionSpec.Raw)spec).Expr);

        foreach (var parameterName in expression.GetParameterNames()) expression.Parameters[parameterName] = null;

        return new NCalcValueTransform(expression);
    }

    private static bool TryEvaluate(Expression expression, IEnumerable<string> names, out object? result)
    {
        result = null;
        foreach (var name in names)
            expression.Parameters[name] = 0d;

        try
        {
            result = expression.Evaluate();
            return true;
        }
        catch (NCalcFunctionNotFoundException)
        {
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}