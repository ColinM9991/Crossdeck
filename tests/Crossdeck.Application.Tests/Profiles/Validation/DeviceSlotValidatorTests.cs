using Crossdeck.Application.Profiles.Validation;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Testing.Profiles;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Crossdeck.Application.Tests.Profiles.Validation;

public sealed class DeviceSlotValidatorTests
{
    [Theory, AutoNSubstituteData]
    public void Validate_WithAVidThatIsNotFourHexDigits_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithSlot(slot => slot with { Model = slot.Model with { Vid = "40G8" }, Assigned = null });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Slot}.Model.Vid")
            .WithErrorMessage("VID must be 4 hex digits")
            .WithSeverity(Severity.Error);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAPidThatIsNotFourHexDigits_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithSlot(slot => slot with { Model = slot.Model with { Pid = "BB3" } });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Slot}.Model.Pid")
            .WithErrorMessage("PID must be 4 hex digits")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithNoAssignedDevice_ReturnsAWarning(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithSlot(slot => slot with { Assigned = null });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Slot}.Assigned")
            .WithSeverity(Severity.Warning);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnAssignedDeviceFromAnotherVendor_ReturnsAnError(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithSlot(slot => slot with { Assigned = new DeviceIdentity(DeviceIdentitySource.Serial, 0x294B, 0xBB35, "serial-2") });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldHaveValidationErrorFor($"{ValidationPaths.Slot}.Assigned")
            .WithErrorMessage("Assigned device VID 294B does not match the slot model 4098")
            .WithSeverity(Severity.Error);
        Assert.Single(result.Errors);
    }

    [Theory, AutoNSubstituteData]
    public void Validate_WithAnAssignedDeviceWhoseVidIsInLowerCase_ReturnsNoErrors(
        ProfileRootValidator sut)
    {
        // Arrange
        var profile = ProfileBuilder.ValidProfile()
            .WithSlot(slot => slot with
            {
                Model = slot.Model with { Vid = "294b" },
                Assigned = new DeviceIdentity(DeviceIdentitySource.Serial, 0x294B, 0xBB35, "serial-2")
            });

        // Act
        var result = sut.TestValidate(profile);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
