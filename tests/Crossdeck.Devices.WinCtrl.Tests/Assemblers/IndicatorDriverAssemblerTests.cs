using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Assemblers;
using Crossdeck.Devices.WinCtrl.Drivers;

namespace Crossdeck.Devices.WinCtrl.Tests.Assemblers;

public sealed class IndicatorDriverAssemblerTests
{
    [Fact]
    public void Kind_Always_IsIndicator()
    {
        // Arrange
        var sut = new IndicatorDriverAssembler();

        // Act / Assert
        Assert.Equal(WinCtrlKind.Indicator, sut.Kind);
    }

    [Theory, AutoNSubstituteData]
    public void Assemble_Always_ReturnsWinCtrlIndicatorDeviceDriver(
        [Frozen] HidDeviceDriver hid,
        [Frozen] IHidTransport transport)
    {
        // Arrange
        var definition = WinCtrlDefinitions.ForPid(0xBB60)!;
        var sut = new IndicatorDriverAssembler();

        // Act
        var result = sut.Assemble(definition, hid, transport);

        // Assert
        Assert.IsType<WinCtrlIndicatorDeviceDriver>(result);
    }
}
