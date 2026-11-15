using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;
using HidSharp.Reports;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class HidReportDecoderTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture()
            .Customize(new AutoNSubstituteCustomization())
            .Customize(new ReportDescriptorCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(HidReportDecoder).GetConstructors());
    }

    [Fact]
    public void Controls_WhenDescriptorHasButton_ExposesButtonControl()
    {
        // Arrange
        var sut = new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button));

        // Act
        var controls = sut.Controls;

        // Assert
        var control = Assert.IsType<ButtonControl>(Assert.Single(controls));
        Assert.Equal(new InputId(InputKind.Button, "1"), control.Id);
        Assert.Equal("Button 1", control.Name);
    }

    [Fact]
    public void Controls_WhenDescriptorHasNoRecognisedUsage_ExposesNoControls()
    {
        // Arrange
        var sut = new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Fcd));

        // Act
        var controls = sut.Controls;

        // Assert
        Assert.Empty(controls);
    }

    [Fact]
    public void Decode_WhenButtonIsPressed_AddsChangeWithValueOne()
    {
        // Arrange
        var sut = new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button));
        var changes = new List<HidInputChange>();

        // Act
        sut.Decode([0x00, 0x01], changes);

        // Assert
        var change = Assert.Single(changes);
        Assert.Equal("Button 1", change.DeviceControl.Name);
        Assert.Equal(1.0, change.Value);
    }

    [Fact]
    public void Decode_WhenButtonIsReleased_AddsChangeWithValueZero()
    {
        // Arrange
        var sut = new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button));
        var changes = new List<HidInputChange>();
        sut.Decode([0x00, 0x01], changes);
        changes.Clear();

        // Act
        sut.Decode([0x00, 0x00], changes);

        // Assert
        var change = Assert.Single(changes);
        Assert.Equal("Button 1", change.DeviceControl.Name);
        Assert.Equal(0.0, change.Value);
    }

    [Fact]
    public void Decode_WhenTheSameReportIsDecodedTwice_AddsTheChangeOnlyOnce()
    {
        // Arrange
        var sut = new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button));
        var changes = new List<HidInputChange>();

        // Act
        sut.Decode([0x00, 0x01], changes);
        sut.Decode([0x00, 0x01], changes);

        // Assert
        Assert.Single(changes);
    }

    [Fact]
    public void Decode_WhenReportIdIsUnknown_AddsNoChanges()
    {
        // Arrange
        var sut = new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button));
        var changes = new List<HidInputChange>();

        // Act
        sut.Decode([0xFF, 0x01], changes);

        // Assert
        Assert.Empty(changes);
    }
}
