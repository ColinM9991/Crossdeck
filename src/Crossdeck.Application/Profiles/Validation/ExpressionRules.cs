using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
using FluentValidation;
using FluentValidation.Results;

namespace Crossdeck.Application.Profiles.Validation;

public static class ExpressionRules
{
    public static IRuleBuilderOptionsConditions<T, TSpec> MustBeValidExpression<T, TSpec>(
        this IRuleBuilder<T, TSpec> ruleBuilder,
        IExpressionCompiler compiler,
        Func<T, ExpressionScope> scope,
        Func<T, string> bindingId)
        where TSpec : ExpressionSpec?
    {
        return ruleBuilder.Custom((spec, context) =>
        {
            if (spec is null)
                return;

            var instance = context.InstanceToValidate;
            if (compiler.Validate(spec, scope(instance)) is not ExpressionError error)
                return;

            context.AddFailure(new ValidationFailure(context.PropertyPath, error.Message)
            {
                CustomState = bindingId(instance)
            });
        });
    }
}
