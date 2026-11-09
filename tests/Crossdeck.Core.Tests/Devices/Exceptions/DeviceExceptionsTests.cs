using Crossdeck.Core.Devices.Exceptions;

namespace Crossdeck.Core.Tests.Devices.Exceptions;

public sealed class DeviceExceptionsTests
{
    [Theory, AutoNSubstituteData]
    public void DeviceNotFoundException_Constructor_IncludesDevicePathInMessage(string devicePath)
    {
        // Act
        var result = new DeviceNotFoundException(devicePath);

        // Assert
        Assert.Equal($"Device not found at path: {devicePath}", result.Message);
    }

    [Theory, AutoNSubstituteData]
    public void DeviceNotUsableException_Constructor_IncludesDevicePathInMessage(string devicePath)
    {
        // Act
        var result = new DeviceNotUsableException(devicePath);

        // Assert
        Assert.Equal($"Device at path: {devicePath} is not usable", result.Message);
    }
}
