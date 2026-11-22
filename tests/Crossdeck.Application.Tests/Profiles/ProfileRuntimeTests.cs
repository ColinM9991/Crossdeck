using Crossdeck.Application.Profiles;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Mapping.Output;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Routing;
using Crossdeck.Testing.Profiles;

namespace Crossdeck.Application.Tests.Profiles;

public sealed class ProfileRuntimeTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(ProfileRuntime).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithNoProblems_PassesEveryBinding(
        [Frozen] IProfileValidator validator,
        [Frozen] IInputDispatcher inputDispatcher,
        [Frozen] ISimValueRouter simValueRouter,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        validator.Validate(profile).Returns([]);

        // Act
        sut.Activate(profile);

        // Assert
        inputDispatcher.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<InputMapping>>(mappings => mappings.Count == 1));
        simValueRouter.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<OutputMapping>>(mappings => mappings.Count == 2));
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithAnErrorOnOneInput_SkipsOnlyThatInput(
        string path,
        string message,
        [Frozen] IProfileValidator validator,
        [Frozen] IInputDispatcher inputDispatcher,
        [Frozen] ISimValueRouter simValueRouter,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        validator.Validate(profile).Returns(
            [new ProfileProblem(path, message, ProblemSeverity.Error, ProfileBuilder.InputBindingId)]);

        // Act
        sut.Activate(profile);

        // Assert
        inputDispatcher.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<InputMapping>>(mappings => mappings.Count == 0));
        simValueRouter.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<OutputMapping>>(mappings => mappings.Count == 2));
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithAnErrorOnOneDeviceOutput_SkipsOnlyThatOutput(
        string path,
        string message,
        [Frozen] IProfileValidator validator,
        [Frozen] IInputDispatcher inputDispatcher,
        [Frozen] ISimValueRouter simValueRouter,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        validator.Validate(profile).Returns(
            [new ProfileProblem(path, message, ProblemSeverity.Error, ProfileBuilder.DeviceOutputBindingId)]);

        // Act
        sut.Activate(profile);

        // Assert
        inputDispatcher.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<InputMapping>>(mappings => mappings.Count == 1));
        simValueRouter.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<OutputMapping>>(mappings =>
                mappings.Count == 1 && mappings[0].Id == ProfileBuilder.RootOutputBindingId));
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithAnErrorOnOneRootOutput_SkipsOnlyThatOutput(
        string path,
        string message,
        [Frozen] IProfileValidator validator,
        [Frozen] ISimValueRouter simValueRouter,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        validator.Validate(profile).Returns(
            [new ProfileProblem(path, message, ProblemSeverity.Error, ProfileBuilder.RootOutputBindingId)]);

        // Act
        sut.Activate(profile);

        // Assert
        simValueRouter.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<OutputMapping>>(mappings =>
                mappings.Count == 1 && mappings[0].Id == ProfileBuilder.DeviceOutputBindingId));
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithAWarningOnABinding_KeepsIt(
        string path,
        string message,
        [Frozen] IProfileValidator validator,
        [Frozen] IInputDispatcher inputDispatcher,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        validator.Validate(profile).Returns(
            [new ProfileProblem(path, message, ProblemSeverity.Warning, ProfileBuilder.InputBindingId)]);

        // Act
        sut.Activate(profile);

        // Assert
        inputDispatcher.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<InputMapping>>(mappings => mappings.Count == 1));
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithAnErrorWithoutABindingId_StillActivatesEveryBinding(
        string path,
        string message,
        [Frozen] IProfileValidator validator,
        [Frozen] IInputDispatcher inputDispatcher,
        [Frozen] ISimValueRouter simValueRouter,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        validator.Validate(profile).Returns([new ProfileProblem(path, message, ProblemSeverity.Error)]);

        // Act
        sut.Activate(profile);

        // Assert
        Assert.Same(profile, sut.Active);
        inputDispatcher.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<InputMapping>>(mappings => mappings.Count == 1));
        simValueRouter.Received(1).SetBindings(
            Arg.Is<IReadOnlyList<OutputMapping>>(mappings => mappings.Count == 2));
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithProblems_ReturnsTheValidatorProblems(
        ProfileProblem first,
        ProfileProblem second,
        [Frozen] IProfileValidator validator,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        IReadOnlyList<ProfileProblem> problems = [first, second];
        validator.Validate(profile).Returns(problems);

        // Act
        var result = sut.Activate(profile);

        // Assert
        Assert.Same(problems, result);
    }

    [Theory, AutoNSubstituteData]
    public void Activate_WithAProfile_RaisesActiveChangedWithThatProfile(
        [Frozen] IProfileValidator validator,
        ProfileRuntime sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile();
        validator.Validate(profile).Returns([]);

        Profile? raised = null;
        sut.ActiveChanged += (_, active) => raised = active;

        // Act
        sut.Activate(profile);

        // Assert
        Assert.Same(profile, raised);
    }
}
