using Crossdeck.Application.Profiles;
using Crossdeck.Core.Profiles.Models;
using FluentValidation;
using FluentValidation.Results;

namespace Crossdeck.Application.Tests.Profiles.Validation;

public sealed class ProfileValidatorTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(ProfileValidator).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithNoFailures_ReturnsNoProblems(
        Profile profile,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        validator.Validate(profile).Returns(new ValidationResult());

        // Act
        var problems = sut.Validate(profile);

        // Assert
        Assert.Empty(problems);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAFailure_ReturnsItsPathAndMessage(
        Profile profile,
        string message,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        StubFailures(validator, profile, new ValidationFailure("Aircraft.Matches", message));

        // Act
        var problems = sut.Validate(profile);

        // Assert
        var problem = Assert.Single(problems);
        Assert.Equal("/aircraft/matches", problem.Path);
        Assert.Equal(message, problem.Message);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnErrorSeverityFailure_ReturnsAnErrorProblem(
        Profile profile,
        string message,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        StubFailures(validator, profile, new ValidationFailure("Name", message) { Severity = Severity.Error });

        // Act
        var problems = sut.Validate(profile);

        // Assert
        Assert.Equal(ProblemSeverity.Error, Assert.Single(problems).Severity);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAWarningSeverityFailure_ReturnsAWarningProblem(
        Profile profile,
        string message,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        StubFailures(validator, profile, new ValidationFailure("Name", message) { Severity = Severity.Warning });

        // Act
        var problems = sut.Validate(profile);

        // Assert
        Assert.Equal(ProblemSeverity.Warning, Assert.Single(problems).Severity);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnInfoSeverityFailure_ReturnsAWarningProblem(
        Profile profile,
        string message,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        StubFailures(validator, profile, new ValidationFailure("Name", message) { Severity = Severity.Info });

        // Act
        var problems = sut.Validate(profile);

        // Assert
        Assert.Equal(ProblemSeverity.Warning, Assert.Single(problems).Severity);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAStringCustomState_ReturnsItAsTheBindingId(
        Profile profile,
        string message,
        string bindingId,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        StubFailures(validator, profile, new ValidationFailure("Name", message) { CustomState = bindingId });

        // Act
        var problems = sut.Validate(profile);

        // Assert
        Assert.Equal(bindingId, Assert.Single(problems).BindingId);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithANonStringCustomState_ReturnsNoBindingId(
        Profile profile,
        string message,
        int state,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        StubFailures(validator, profile, new ValidationFailure("Name", message) { CustomState = state });

        // Act
        var problems = sut.Validate(profile);

        // Assert
        Assert.Null(Assert.Single(problems).BindingId);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithSeveralFailures_ReturnsThemInOrder(
        Profile profile,
        string firstMessage,
        string secondMessage,
        [Frozen] IValidator<Profile> validator,
        ProfileValidator sut)
    {
        // Arrange
        StubFailures(
            validator,
            profile,
            new ValidationFailure("Name", firstMessage),
            new ValidationFailure("Id", secondMessage));

        // Act
        var problems = sut.Validate(profile);

        // Assert
        Assert.Equal([firstMessage, secondMessage], problems.Select(p => p.Message));
    }

    private static void StubFailures(IValidator<Profile> validator, Profile profile, params ValidationFailure[] failures) => validator.Validate(profile).Returns(new FluentValidation.Results.ValidationResult(failures));
}
