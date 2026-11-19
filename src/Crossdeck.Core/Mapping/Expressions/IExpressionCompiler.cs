namespace Crossdeck.Core.Mapping.Expressions;

public interface IExpressionCompiler
{
    ValidationResult Validate(ExpressionSpec spec, ExpressionScope scope);

    IValueTransform Compile(ExpressionSpec spec, ExpressionScope scope);
}