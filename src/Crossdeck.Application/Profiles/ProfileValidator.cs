using Crossdeck.Application.Profiles.Validation;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;
using FluentValidation;
using FluentValidation.Results;

namespace Crossdeck.Application.Profiles;

public sealed class ProfileValidator : IProfileValidator
{
    private readonly IValidator<Profile> _validator;

    public ProfileValidator(IValidator<Profile> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public IReadOnlyList<ProfileProblem> Validate(Profile profile) => [.. _validator.Validate(profile).Errors.Select(ToProblem)];

    private static ProfileProblem ToProblem(ValidationFailure failure) =>
        new(
            ProfilePath.FromPropertyName(failure.PropertyName),
            failure.ErrorMessage,
            failure.Severity == Severity.Error ? ProblemSeverity.Error : ProblemSeverity.Warning,
            failure.CustomState as string);
}
