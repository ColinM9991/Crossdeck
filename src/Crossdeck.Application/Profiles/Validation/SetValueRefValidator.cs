using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Profiles.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class SetValueRefValidator : AbstractValidator<SetValue.Ref>
{
    public SetValueRefValidator(Profile profile, string bindingId)
    {
        RuleFor(x => x.Source).MustResolveIn(profile, bindingId);
    }
}
