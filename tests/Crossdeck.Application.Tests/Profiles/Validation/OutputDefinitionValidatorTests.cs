using System.Collections.Immutable;
using Crossdeck.Application.Profiles.Validation;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Simulators.Models;
using Crossdeck.Testing.Profiles;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Crossdeck.Application.Tests.Profiles.Validation;

public sealed class OutputDefinitionValidatorTests
{
    [Theory, AutoNSubstituteData]
    public void Validate_WithADeviceOutputWithAnEmptyBindingId_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile().WithDeviceOutput(output => output with { Id = string.Empty });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.DeviceOutput}.Id")
            .WithErrorMessage("Binding id is empty")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithARootOutputWithAnEmptyBindingId_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile().WithRootOutput(output => output with { Id = string.Empty });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.RootOutput}.Id")
            .WithErrorMessage("Binding id is empty");
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithADeviceOutputWithNoSources_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithDeviceOutput(output => output with { Sources = ImmutableDictionary<string, SimRef>.Empty });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.DeviceOutput}.Sources")
            .WithErrorMessage("Output has no sources")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.DeviceOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithARootOutputWithNoSources_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithRootOutput(output => output with { Sources = ImmutableDictionary<string, SimRef>.Empty });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.RootOutput}.Sources")
            .WithCustomState(ProfileBuilder.RootOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnUndeclaredVariableSource_ReturnsAnError(
        string alias,
        string variableName,
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithDeviceOutput(output => output with { Sources = output.Sources.Add(alias, new SimRef.Variable(variableName)) });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.DeviceOutput}.Sources/{alias}.Value.Name")
            .WithErrorMessage($"Variable '{variableName}' is not declared")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.DeviceOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAMissingOutputSource_ReturnsAnError(
        string alias,
        string outputName,
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithRootOutput(output => output with { Sources = output.Sources.Add(alias, new SimRef.Output(outputName)) });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.RootOutput}.Sources/{alias}.Value.Id")
            .WithErrorMessage($"Output '{outputName}' does not exist")
            .WithCustomState(ProfileBuilder.RootOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithATransformTheCompilerRejects_ReturnsTheCompilerMessage(
        string expression,
        string message,
        [Frozen] IExpressionCompiler compiler,
        ProfileRootValidator sut)
    {
        // Arrange
        var spec = new ExpressionSpec.Raw(expression);
        var profile = ProfileBuilder.ValidProfile().WithDeviceOutput(output => output with { Transform = spec });

        compiler.Validate(spec, Arg.Any<ExpressionScope>()).Returns(ValidationResult.Failure(message));

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.DeviceOutput}.Transform")
            .WithErrorMessage(message)
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.DeviceOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithATransform_ValidatesItInAValueScopeOverTheSourceAliases(
        string expression,
        [Frozen] IExpressionCompiler compiler,
        ProfileRootValidator sut)
    {
        // Arrange
        var spec = new ExpressionSpec.Raw(expression);
        var profile = ProfileBuilder.ValidProfile().WithDeviceOutput(output => output with { Transform = spec });

        // Act
        sut.Validate(profile);

        // Assert
        compiler.Received(1).Validate(
            spec,
            Arg.Is<ExpressionScope>(scope =>
                scope.Kind == ExpressionKind.Value
                && scope.Names.SequenceEqual(new[] { ProfileBuilder.SourceAlias })));
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAWhenTheCompilerRejects_ReturnsTheCompilerMessage(
        string expression,
        string message,
        [Frozen] IExpressionCompiler compiler,
        ProfileRootValidator sut)
    {
        // Arrange
        var spec = new ExpressionSpec.Raw(expression);
        var profile = ProfileBuilder.ValidProfile().WithRootOutput(output => output with { When = spec });

        compiler.Validate(spec, Arg.Any<ExpressionScope>()).Returns(ValidationResult.Failure(message));

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.RootOutput}.When")
            .WithErrorMessage(message)
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.RootOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAWhen_ValidatesItInAGuardScopeOverTheSourceAliases(
        string expression,
        [Frozen] IExpressionCompiler compiler,
        ProfileRootValidator sut)
    {
        // Arrange
        var spec = new ExpressionSpec.Raw(expression);
        var profile = ProfileBuilder.ValidProfile().WithRootOutput(output => output with { When = spec });

        // Act
        sut.Validate(profile);

        // Assert
        compiler.Received(1).Validate(
            spec,
            Arg.Is<ExpressionScope>(scope =>
                scope.Kind == ExpressionKind.Guard
                && scope.Names.SequenceEqual(new[] { ProfileBuilder.SourceAlias })));
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithSeveralSourcesAndNoTransform_ReturnsAnError(
        string alias,
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithDeviceOutput(output => output with
            {
                Sources = output.Sources.Add(alias, new SimRef.Variable(ProfileBuilder.VariableName)),
                Transform = null
            });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.DeviceOutput}.Transform")
            .WithErrorMessage("Output has 2 sources but no transform to combine them")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.DeviceOutputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithOneSourceAndNoTransform_ReturnsNoErrors(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile().WithDeviceOutput(output => output with { Transform = null });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
