using System.Collections.Immutable;
using Crossdeck.Application.Profiles.Validation;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using Crossdeck.Testing.Profiles;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Crossdeck.Application.Tests.Profiles.Validation;

public sealed class ProfileRootValidatorTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(ProfileRootValidator).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAValidProfile_ReturnsNoErrors(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnEmptyProfileId_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile() with { Id = Guid.Empty };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor("Id")
            .WithErrorMessage("Profile id is empty")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnEmptyName_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile() with { Name = string.Empty };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor("Name")
            .WithErrorMessage("Profile name is empty")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithNoAircraftMatches_ReturnsAWarning(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile() with { Aircraft = new AircraftMatch([]) };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor("Aircraft.Matches")
            .WithSeverity(Severity.Warning);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithEmptyCollections_ReturnsNoErrors(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile() with
        {
            Variables = ImmutableDictionary<string, VariableDefinition>.Empty,
            Devices = ImmutableDictionary<string, DeviceSlot>.Empty,
            Outputs = ImmutableDictionary<string, OutputDefinition>.Empty
        };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnInvalidVariableName_ReturnsAnError(
        VariableDefinition variable,
        ProfileRootValidator sut)
    {
        // Arrange
        const string name = "1 bad name";
        var profile = ProfileBuilder.ValidProfile();
        profile = profile with { Variables = profile.Variables.Add(name, variable with { Initial = new SimValue.Number(1) }) };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"Variables/{name}.Key")
            .WithErrorMessage($"Variable name '{name}' must start with a letter or _ and contain only letters, digits and _")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAVariableWithoutAnInitialValue_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        profile = profile with
        {
            Variables = profile.Variables.SetItem(ProfileBuilder.VariableName, new VariableDefinition(new SimValue.None()))
        };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"Variables/{ProfileBuilder.VariableName}.Value.Initial")
            .WithErrorMessage($"Variable '{ProfileBuilder.VariableName}' has no initial value")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnInvalidRootOutputName_ReturnsAnErrorForThatBinding(
        ProfileRootValidator sut)
    {
        // Arrange
        const string name = "bus powered";
        var profile = ProfileBuilder.ValidProfile();
        profile = profile with { Outputs = profile.Outputs.Add(name, ProfileBuilder.ComputedOutput("binding-spaced")) };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"Outputs/{name}")
            .WithErrorMessage($"Output name '{name}' must start with a letter or _ and contain only letters, digits and _")
            .WithSeverity(Severity.Error)
            .WithCustomState("binding-spaced");
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithARootOutputReadingItself_ReturnsACycleError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithRootOutput(_ => ProfileBuilder.ComputedOutput(ProfileBuilder.RootOutputBindingId, ProfileBuilder.RootOutputKey));

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"Outputs/{ProfileBuilder.RootOutputKey}")
            .WithErrorMessage($"Output '{ProfileBuilder.RootOutputKey}' is part of a cycle: {ProfileBuilder.RootOutputKey} → {ProfileBuilder.RootOutputKey}")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.RootOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithTwoRootOutputsReadingEachOther_ReturnsACycleErrorOnEach(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile() with
        {
            Outputs = ImmutableDictionary<string, OutputDefinition>.Empty
                .Add("a", ProfileBuilder.ComputedOutput("binding-a", "b"))
                .Add("b", ProfileBuilder.ComputedOutput("binding-b", "a"))
        };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor("Outputs/a")
            .WithErrorMessage("Output 'a' is part of a cycle: a → b → a")
            .WithCustomState("binding-a");
        result.ShouldHaveValidationErrorFor("Outputs/b")
            .WithErrorMessage("Output 'b' is part of a cycle: b → a → b")
            .WithCustomState("binding-b");
        Assert.Equal(2, result.Errors.Count);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAChainOfRootOutputsAndNoCycle_ReturnsNoErrors(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile() with
        {
            Outputs = ImmutableDictionary<string, OutputDefinition>.Empty
                .Add("a", ProfileBuilder.ComputedOutput("binding-a", "b"))
                .Add("b", ProfileBuilder.ComputedOutput("binding-b", "c"))
                .Add("c", ProfileBuilder.ComputedOutput("binding-c"))
        };

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnInputAndRootOutputSharingABindingId_ReturnsAnErrorOnTheRepeat(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithRootOutput(output => output with { Id = ProfileBuilder.InputBindingId });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor(ValidationPaths.Input[..^".Value".Length])
            .WithErrorMessage($"Binding id '{ProfileBuilder.InputBindingId}' is used more than once")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithADeviceOutputRepeatingAnInputBindingId_ReturnsAnErrorOnTheDeviceOutput(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithDeviceOutput(output => output with { Id = ProfileBuilder.InputBindingId });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor(ValidationPaths.DeviceOutput[..^".Value".Length])
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }
}
