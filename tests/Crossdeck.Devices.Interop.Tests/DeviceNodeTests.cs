using System.Runtime.Versioning;

namespace Crossdeck.Devices.Interop.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeviceNodeTests
{
    [Fact]
    public void ResolveInterfaceDevInst_WithUnregisteredInterfacePath_ReturnsNull()
    {
        // Arrange
        var devicePath = @"\\?\hid\vid_0000&pid_0000";

        // Act
        var result = DeviceNode.ResolveInterfaceDevInst(devicePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Walk_WithInvalidDevInst_YieldsStartingNodeOnly()
    {
        // Arrange
        const uint devInst = 0;

        // Act
        var result = DeviceNode.Walk(devInst, 10).ToList();

        // Assert
        Assert.Equal([devInst], result);
    }

    [Fact]
    public void GetDeviceId_WithInvalidDevInst_ReturnsNull()
    {
        // Arrange
        const uint devInst = 0;

        // Act
        var result = DeviceNode.GetDeviceId(devInst);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ReadDevNodeStringProperty_WithInvalidDevInst_ReturnsNull()
    {
        // Arrange
        const uint devInst = 0;

        // Act
        var result = DeviceNode.ReadDevNodeStringProperty(devInst, SetupDi.SpdrpDeviceSerial);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ReadPortName_WithInvalidDeviceInfoSet_ReturnsNull()
    {
        // Arrange
        var data = new SP_DEVINFO_DATA();

        // Act
        var result = DeviceNode.ReadPortName(nint.Zero, ref data);

        // Assert
        Assert.Null(result);
    }
}
