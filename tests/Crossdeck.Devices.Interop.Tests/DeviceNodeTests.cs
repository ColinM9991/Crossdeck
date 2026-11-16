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

    [Fact]
    public void GetUsbParent_WithInvalidDevInst_ReturnsNull()
    {
        // Arrange
        const uint devInst = 0;

        // Act
        var result = DeviceNode.GetUsbParent(devInst);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUsbParentId_WithInvalidDevInst_ReturnsNull()
    {
        // Arrange
        const uint devInst = 0;

        // Act
        var result = DeviceNode.GetUsbParentId(devInst);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseUsbRootId_WithUsbRootId_ReturnsVendorProductAndSerial()
    {
        // Arrange
        var deviceId = @"USB\VID_346E&PID_1305\8B4238001422361051353730";

        // Act
        var result = DeviceNode.ParseUsbRootId(deviceId);

        // Assert
        Assert.NotNull(result);
        var (vendorId, productId, serial) = result.Value;

        Assert.Equal(0x346E, vendorId);
        Assert.Equal(0x1305, productId);
        Assert.Equal("8B4238001422361051353730", serial);
    }

    [Fact]
    public void ParseUsbRootId_WithoutSerialSegment_ReturnsNullSerial()
    {
        // Arrange
        var deviceId = @"USB\VID_346E&PID_1305";

        // Act
        var result = DeviceNode.ParseUsbRootId(deviceId);

        // Assert
        Assert.NotNull(result);
        var (vendorId, productId, serial) = result.Value;

        Assert.Equal(0x346E, vendorId);
        Assert.Equal(0x1305, productId);
        Assert.Null(serial);
    }

    [Fact]
    public void ParseUsbRootId_WithChildId_ReturnsNull()
    {
        // Arrange
        var deviceId = @"USB\VID_346E&PID_1305&MI_02\7&1175168319&0000";

        // Act
        var result = DeviceNode.ParseUsbRootId(deviceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseUsbRootId_WithNonUsbDeviceId_ReturnsNull()
    {
        // Arrange
        var deviceId = @"PCI\VEN_8086&DEV_A382\3&1158FFFF&0";

        // Act
        var result = DeviceNode.ParseUsbRootId(deviceId);

        // Assert
        Assert.Null(result);
    }
}
