using Crossdeck.Application.Expressions;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Profiles.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class InputEventValidator : AbstractValidator<KeyValuePair<string, InputEventBinding>>
{
    public InputEventValidator(Profile profile, IExpressionCompiler compiler, InputDefinition input)
    {
        RuleFor(x => x.Value.Actions)
            .NotEmpty()
            .WithMessage(x => $"Event '{x.Key}' has no actions")
            .WithSeverity(Severity.Warning)
            .WithState(_ => input.Id);

        RuleFor(x => x.Value.When)
            .MustBeValidExpression(
                compiler,
                _ => ExpressionScopes.InputGuard(input.Sources.Keys),
                _ => input.Id);

        RuleForEach(x => x.Value.Actions)
            .SetInheritanceValidator(validator =>
            {
                validator.Add(new CommandActionValidator(input.Id));
                validator.Add(new SetActionValidator(profile, compiler, input));
            });
    }
}
