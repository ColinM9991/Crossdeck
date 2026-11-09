using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Devices.WinCtrl;
using Crossdeck.Devices.WinCtrl.Encoders;

namespace Crossdeck.Devices.WinCtrl.Tests.Encoders;

public sealed class WinCtrlHidReportEncoderTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(WinCtrlHidReportEncoder).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WithNoMatchingStateEntry_ReturnsNoReports(
        WinCtrlDefinition definition,
        WinCtrlLight light,
        OutputId otherId,
        int otherValue)
    {
        // Arrange
        definition = definition with { Lights = [light] };
        var sut = new WinCtrlHidReportEncoder(definition);
        var state = new Dictionary<OutputId, int> { [otherId] = otherValue };

        // Act
        var result = sut.Encode(state);

        // Assert
        Assert.Empty(result);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WithNewValueForLight_ReturnsReportWithExpectedByteLayout(
        WinCtrlDefinition definition,
        WinCtrlLight light,
        byte value)
    {
        // Arrange
        definition = definition with { Lights = [light] };
        var sut = new WinCtrlHidReportEncoder(definition);
        var state = new Dictionary<OutputId, int> { [light.Id] = value };

        // Act
        var result = sut.Encode(state);

        // Assert
        var report = Assert.Single(result);
        Assert.Equal(HidReportType.Output, report.Type);
        Assert.Equal(0x02, report.Data[0]);
        Assert.Equal((byte)(light.Target & 0xFF), report.Data[1]);
        Assert.Equal((byte)(light.Target >> 8), report.Data[2]);
        Assert.Equal(0x03, report.Data[5]);
        Assert.Equal(0x49, report.Data[6]);
        Assert.Equal(light.Light, report.Data[7]);
        Assert.Equal(value, report.Data[8]);
    }

    [Theory, AutoNSubstituteData]
    public void Encode_WhenValueUnchangedSinceLastSend_ReturnsNoReportForThatLight(
        WinCtrlDefinition definition,
        WinCtrlLight light,
        byte value)
    {
        // Arrange
        definition = definition with { Lights = [light] };
        var sut = new WinCtrlHidReportEncoder(definition);
        var state = new Dictionary<OutputId, int> { [light.Id] = value };
        sut.Encode(state);

        // Act
        var result = sut.Encode(state);

        // Assert
        Assert.Empty(result);
    }

    [Theory, AutoNSubstituteData]
    public void Reset_Always_ForgetsPreviouslySentValues(
        WinCtrlDefinition definition,
        WinCtrlLight light,
        byte value)
    {
        // Arrange
        definition = definition with { Lights = [light] };
        var sut = new WinCtrlHidReportEncoder(definition);
        var state = new Dictionary<OutputId, int> { [light.Id] = value };
        sut.Encode(state);

        // Act
        sut.Reset();
        var result = sut.Encode(state);

        // Assert
        Assert.Single(result);
    }
}
