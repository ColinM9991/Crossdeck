using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public static class SimRefRules
{
    public static IRuleBuilderOptions<T, SimRef> MustResolveIn<T>(
        this IRuleBuilder<T, SimRef> ruleBuilder,
        Profile profile,
        string bindingId)
    {
        return ruleBuilder.SetInheritanceValidator(validator =>
        {
            validator.Add(new VariableRefValidator(profile, bindingId));
            validator.Add(new OutputRefValidator(profile, bindingId));
        });
    }
}
