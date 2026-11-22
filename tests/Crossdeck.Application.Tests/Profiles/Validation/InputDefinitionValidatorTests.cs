using System.Collections.Immutable;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Application.Profiles.Validation;
using Crossdeck.Core.Simulators.Models;
using Crossdeck.Testing.Profiles;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Crossdeck.Application.Tests.Profiles.Validation;

public sealed class InputDefinitionValidatorTests
{
    [Theory, AutoNSubstituteData]
    public void Validate_WithAnEmptyBindingId_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile().WithInput(input => input with { Id = string.Empty });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Input}.Id")
            .WithErrorMessage("Binding id is empty")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithNoEvents_ReturnsAWarning(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithInput(input => input with { Events = ImmutableDictionary<string, InputEventBinding>.Empty });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Input}.Events")
            .WithSeverity(Severity.Warning)
            .WithCustomState(ProfileBuilder.InputBindingId);
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
            .WithInput(input => input with { Sources = input.Sources.Add(alias, new SimRef.Variable(variableName)) });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Input}.Sources/{alias}.Value.Name")
            .WithErrorMessage($"Variable '{variableName}' is not declared")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
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
            .WithInput(input => input with { Sources = input.Sources.Add(alias, new SimRef.Output(outputName)) });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Input}.Sources/{alias}.Value.Id")
            .WithErrorMessage($"Output '{outputName}' does not exist")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithADeclaredVariableAndExistingOutputAsSources_ReturnsNoErrors(
        string variableAlias,
        string outputAlias,
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithInput(input => input with
            {
                Sources = input.Sources
                    .Add(variableAlias, new SimRef.Variable(ProfileBuilder.VariableName))
                    .Add(outputAlias, new SimRef.Output(ProfileBuilder.RootOutputKey))
            });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnEventWithNoActions_ReturnsAWarning(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.ReleasedEvent, binding => binding with { Actions = [] });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Event(ProfileBuilder.ReleasedEvent)}.Actions")
            .WithErrorMessage($"Event '{ProfileBuilder.ReleasedEvent}' has no actions")
            .WithSeverity(Severity.Warning)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithACommandActionTargetingAVariable_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var action = new InputAction.Command(new SimRef.Variable(ProfileBuilder.VariableName));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Action(ProfileBuilder.PressedEvent, 0)}.Ref")
            .WithErrorMessage($"Only sim commands can be sent; 'var:{ProfileBuilder.VariableName}' is a variable")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithASetActionTargetingAComputedOutput_ReturnsAnError(
        SimValue value,
        ProfileRootValidator sut)
    {
        // Arrange
        var action = new InputAction.Set(new SimRef.Output(ProfileBuilder.RootOutputKey), new SetValue.Literal(value));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Action(ProfileBuilder.PressedEvent, 0)}.Ref")
            .WithErrorMessage($"Computed output '{ProfileBuilder.RootOutputKey}' is read-only")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithASetActionTargetingAnUndeclaredVariable_ReturnsAnError(
        string variableName,
        SimValue value,
        ProfileRootValidator sut)
    {
        // Arrange
        var action = new InputAction.Set(new SimRef.Variable(variableName), new SetValue.Literal(value));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Action(ProfileBuilder.PressedEvent, 0)}.Ref.Name")
            .WithErrorMessage($"Variable '{variableName}' is not declared")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithASetActionTargetingADeclaredVariable_ReturnsNoErrors(
        SimValue value,
        ProfileRootValidator sut)
    {
        // Arrange
        var action = new InputAction.Set(new SimRef.Variable(ProfileBuilder.VariableName), new SetValue.Literal(value));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithASetActionReadingAnUndeclaredVariable_ReturnsAnError(
        string variableName,
        ProfileRootValidator sut)
    {
        // Arrange
        var action = new InputAction.Set(
            new SimRef.Variable(ProfileBuilder.VariableName),
            new SetValue.Ref(new SimRef.Variable(variableName)));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Action(ProfileBuilder.PressedEvent, 0)}.Value.Source.Name")
            .WithErrorMessage($"Variable '{variableName}' is not declared")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithASetActionReadingAMissingOutput_ReturnsAnError(
        string outputName,
        ProfileRootValidator sut)
    {
        // Arrange
        var action = new InputAction.Set(
            new SimRef.Variable(ProfileBuilder.VariableName),
            new SetValue.Ref(new SimRef.Output(outputName)));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Action(ProfileBuilder.PressedEvent, 0)}.Value.Source.Id")
            .WithErrorMessage($"Output '{outputName}' does not exist")
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithASetActionExpressionTheCompilerRejects_ReturnsTheCompilerMessage(
        string expression,
        string message,
        [Frozen] IExpressionCompiler compiler,
        ProfileRootValidator sut)
    {
        // Arrange
        var spec = new ExpressionSpec.Raw(expression);
        var action = new InputAction.Set(new SimRef.Variable(ProfileBuilder.VariableName), new SetValue.Expr(spec));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        compiler.Validate(spec, Arg.Any<ExpressionScope>()).Returns(ValidationResult.Failure(message));

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Action(ProfileBuilder.PressedEvent, 0)}.Value.Spec")
            .WithErrorMessage(message)
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithASetActionExpression_ValidatesItInAValueScopeWithSelf(
        string expression,
        [Frozen] IExpressionCompiler compiler,
        ProfileRootValidator sut)
    {
        // Arrange
        var spec = new ExpressionSpec.Raw(expression);
        var action = new InputAction.Set(new SimRef.Variable(ProfileBuilder.VariableName), new SetValue.Expr(spec));
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { Actions = [action] });

        // Act
        sut.Validate(profile);

        // Assert
        compiler.Received(1).Validate(
            spec,
            Arg.Is<ExpressionScope>(scope =>
                scope.Kind == ExpressionKind.Value
                && scope.Names.SequenceEqual(new[] { ProfileBuilder.VariableName, "self" })));
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
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { When = spec });

        compiler.Validate(spec, Arg.Any<ExpressionScope>()).Returns(ValidationResult.Failure(message));

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Event(ProfileBuilder.PressedEvent)}.When")
            .WithErrorMessage(message)
            .WithSeverity(Severity.Error)
            .WithCustomState(ProfileBuilder.InputBindingId);
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
        var profile = ProfileBuilder.ValidProfile()
            .WithEvent(ProfileBuilder.PressedEvent, binding => binding with { When = spec });

        // Act
        sut.Validate(profile);

        // Assert
        compiler.Received(1).Validate(
            spec,
            Arg.Is<ExpressionScope>(scope =>
                scope.Kind == ExpressionKind.Guard
                && scope.Names.SequenceEqual(new[] { ProfileBuilder.VariableName })));
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithNoWhen_DoesNotAskTheCompilerToValidateIt(
        [Frozen] IExpressionCompiler compiler,
        ProfileRootValidator sut)
    {
        // Arrange
        var original = ProfileBuilder.ValidProfile();
        var originalWhen = original.Devices[ProfileBuilder.SlotKey].Inputs[ProfileBuilder.InputKey]
            .Events[ProfileBuilder.PressedEvent].When!;
        var profile = original.WithEvent(ProfileBuilder.PressedEvent, binding => binding with { When = null });

        // Act
        sut.Validate(profile);

        // Assert
        compiler.DidNotReceive().Validate(originalWhen, Arg.Any<ExpressionScope>());
    }
}
