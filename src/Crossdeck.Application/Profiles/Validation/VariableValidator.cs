using System.Text.RegularExpressions;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed partial class VariableValidator : AbstractValidator<KeyValuePair<string, VariableDefinition>>
{
    public VariableValidator()
    {
        RuleFor(x => x.Key)
            .Must(name => NamePattern().IsMatch(name))
            .WithMessage(x => $"Variable name '{x.Key}' must start with a letter or _ and contain only letters, digits and _");

        RuleFor(x => x.Value.Initial)
            .Must(initial => initial is not SimValue.None)
            .WithMessage(x => $"Variable '{x.Key}' has no initial value");
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    internal static partial Regex NamePattern();
}
