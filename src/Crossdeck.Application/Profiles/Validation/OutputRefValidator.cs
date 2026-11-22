using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class OutputRefValidator : AbstractValidator<SimRef.Output>
{
    public OutputRefValidator(Profile profile, string bindingId)
    {
        RuleFor(x => x.Id)
            .Must(id => profile.Outputs.ContainsKey(id))
            .WithMessage(x => $"Output '{x.Id}' does not exist")
            .WithState(_ => bindingId);
    }
}
