using Crossdeck.Core.Devices.Models;
using HidSharp.Reports;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class HidControlFactoryTests
{
    [Fact]
    public void Create_WhenUsageIsButton_ReturnsButtonControl()
    {
        // Arrange
        var sut = new HidControlFactory();
        var (usage, item) = Value(TestReportDescriptor.Button);

        // Act
        var result = sut.Create(usage, item);

        // Assert
        var button = Assert.IsType<ButtonControl>(result);
        Assert.Equal(new InputId(InputKind.Button, "1"), button.Id);
        Assert.Equal(1u, button.ButtonId);
        Assert.Equal("Button 1", button.Name);
    }

    [Fact]
    public void Create_WhenSameButtonIsCreatedTwice_ReturnsNumberedButtonControl()
    {
        // Arrange
        var sut = new HidControlFactory();
        var (usage, item) = Value(TestReportDescriptor.Button);
        sut.Create(usage, item);

        // Act
        var result = sut.Create(usage, item);

        // Assert
        var button = Assert.IsType<ButtonControl>(result);
        Assert.Equal(new InputId(InputKind.Button, "1#2"), button.Id);
        Assert.Equal("Button 1 (2)", button.Name);
    }

    [Fact]
    public void Create_WhenUsageIsHatSwitch_ReturnsHatControlWithPositionCount()
    {
        // Arrange
        var sut = new HidControlFactory();
        var (usage, item) = Value(Descriptor(0x01, 0x39, 0x04, 0x07, 0x00, 0x00));

        // Act
        var result = sut.Create(usage, item);

        // Assert
        var hat = Assert.IsType<HatControl>(result);
        Assert.Equal(new InputId(InputKind.Hat, "1"), hat.Id);
        Assert.Equal("Hat 1", hat.Name);
        Assert.Equal(8, hat.Positions);
    }

    [Fact]
    public void Create_WhenUsageIsDesktopAxis_ReturnsAxisControlWithPhysicalRange()
    {
        // Arrange
        var sut = new HidControlFactory();
        var (usage, item) = Value(Descriptor(0x01, 0x30, 0x08, 0xFF, 0x0A, 0x14));

        // Act
        var result = sut.Create(usage, item);

        // Assert
        var axis = Assert.IsType<AxisControl>(result);
        Assert.Equal(new InputId(InputKind.Axis, "x"), axis.Id);
        Assert.Equal("X axis", axis.Name);
        Assert.Equal(10.0, axis.Min);
        Assert.Equal(20.0, axis.Max);
    }

    [Fact]
    public void Create_WhenUsageIsDesktopWheel_ReturnsWheelAxisControlWithLogicalRange()
    {
        // Arrange
        var sut = new HidControlFactory();
        var (usage, item) = Value(Descriptor(0x01, 0x38, 0x08, 0xFF, 0x00, 0x00));

        // Act
        var result = sut.Create(usage, item);

        // Assert
        var axis = Assert.IsType<AxisControl>(result);
        Assert.Equal(new InputId(InputKind.Axis, "wheel"), axis.Id);
        Assert.Equal("Wheel axis", axis.Name);
        Assert.Equal(0.0, axis.Min);
        Assert.Equal(255.0, axis.Max);
    }

    [Theory]
    [InlineData(0x02_00B0, "Aileron axis")]
    [InlineData(0x02_00B8, "Elevator axis")]
    [InlineData(0x02_00BA, "Rudder axis")]
    [InlineData(0x02_00BB, "Throttle axis")]
    [InlineData(0x02_00BF, "Toe Brake axis")]
    public void Create_WhenUsageIsSimulationAxis_ReturnsNamedAxisControl(
        uint usage,
        string expectedName)
    {
        // Arrange
        var sut = new HidControlFactory();
        var (_, item) = Value(Descriptor((byte)(usage >> 16), (byte)usage, 0x08, 0xFF, 0x00, 0x00));

        // Act
        var result = sut.Create(usage, item);

        // Assert
        var axis = Assert.IsType<AxisControl>(result);
        Assert.Equal(expectedName, axis.Name);
    }

    [Fact]
    public void Create_WhenUsageIsUnknown_ReturnsNull()
    {
        // Arrange
        var sut = new HidControlFactory();
        var (_, item) = Value(TestReportDescriptor.Button);

        // Act
        var result = sut.Create(0x07_0001u, item);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_WhenButtonIdIsZero_ReturnsNull()
    {
        // Arrange
        var sut = new HidControlFactory();
        var (_, item) = Value(TestReportDescriptor.Button);

        // Act
        var result = sut.Create(0x09_0000u, item);

        // Assert
        Assert.Null(result);
    }

    private static byte[] Descriptor(
        byte usagePage,
        byte usage,
        byte reportSize,
        byte logicalMax,
        byte physicalMin,
        byte physicalMax)
    {
        byte[] physical = physicalMin == 0 && physicalMax == 0
            ? []
            : [0x35, physicalMin, 0x45, physicalMax];

        byte[] descriptor =
        [
            0x05, usagePage,
            0xA1, 0x01,
            0x09, usage,
            0x15, 0x00,
            0x25, logicalMax,
            .. physical,
            0x75, reportSize,
            0x95, 0x01,
            0x81, 0x02,
            0xC0,
        ];
        return descriptor;
    }

    private static (uint Usage, DataItem Item) Value(byte[] descriptor)
    {
        var reportDescriptor = new ReportDescriptor(descriptor);
        var value = reportDescriptor.DeviceItems[0].CreateDeviceItemInputParser().GetValue(0);
        return (value.Usages.First(), value.DataItem);
    }
}
