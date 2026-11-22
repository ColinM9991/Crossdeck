using Crossdeck.Application.Expressions;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Mapping.Input;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class SetValueExprValidator : AbstractValidator<SetValue.Expr>
{
    public SetValueExprValidator(IExpressionCompiler compiler, IEnumerable<string> sourceAliases, string bindingId)
    {
        var scope = ExpressionScopes.InputValue(sourceAliases);

        RuleFor(x => x.Spec).MustBeValidExpression(compiler, _ => scope, _ => bindingId);
    }
}
