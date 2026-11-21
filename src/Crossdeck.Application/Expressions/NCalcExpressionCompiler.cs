using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
using NCalc;

namespace Crossdeck.Application.Expressions;

internal sealed class NCalcExpressionCompiler : IExpressionCompiler
{
    public ValidationResult Validate(ExpressionSpec spec, ExpressionScope scope)
    {
        throw new NotImplementedException();
    }

    public IValueTransform Compile(ExpressionSpec spec, ExpressionScope scope)
    {
        if (spec is not ExpressionSpec.Raw raw)
            throw new InvalidOperationException();

        var expression = new Expression(raw.Expr);

        foreach (var scopeName in scope.Names) expression.Parameters.Add(scopeName, null);

        return new NCalcValueTransform(expression, scope);
    }
}