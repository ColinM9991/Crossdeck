using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class VariableRefValidator : AbstractValidator<SimRef.Variable>
{
    public VariableRefValidator(Profile profile, string bindingId)
    {
        RuleFor(x => x.Name)
            .Must(name => profile.Variables.ContainsKey(name))
            .WithMessage(x => $"Variable '{x.Name}' is not declared")
            .WithState(_ => bindingId);
    }
}
