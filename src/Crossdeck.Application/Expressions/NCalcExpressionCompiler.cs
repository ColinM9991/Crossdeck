using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;

namespace Crossdeck.Application.Expressions;

internal sealed class NCalcExpressionCompiler : IExpressionCompiler
{
    public ValidationResult Validate(ExpressionSpec spec, ExpressionScope scope)
    {
        throw new NotImplementedException();
    }

    public IValueTransform Compile(ExpressionSpec spec, ExpressionScope scope)
    {
        throw new NotImplementedException();
    }
}