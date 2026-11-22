using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class SourceValidator : AbstractValidator<KeyValuePair<string, SimRef>>
{
    public SourceValidator(Profile profile, string bindingId)
    {
        RuleFor(x => x.Value).MustResolveIn(profile, bindingId);
    }
}
