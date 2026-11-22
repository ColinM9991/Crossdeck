using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class SetActionValidator : AbstractValidator<InputAction.Set>
{
    public SetActionValidator(
        Profile profile,
        IExpressionCompiler compiler,
        InputDefinition input)
    {
        RuleFor(x => x.Ref)
            .Must(simRef => simRef is not SimRef.Output)
            .WithMessage(x => $"Computed output '{x.Ref.Key[4..]}' is read-only")
            .WithState(_ => input.Id);

        RuleFor(x => x.Ref)
            .SetInheritanceValidator(validator =>
                validator.Add(new VariableRefValidator(profile, input.Id)));

        RuleFor(x => x.Value)
            .SetInheritanceValidator(validator =>
            {
                validator.Add(new SetValueRefValidator(profile, input.Id));
                validator.Add(new SetValueExprValidator(compiler, input.Sources.Keys, input.Id));
            });
    }
}
