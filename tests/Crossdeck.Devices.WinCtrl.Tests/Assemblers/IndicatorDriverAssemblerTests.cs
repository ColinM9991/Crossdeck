using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Assemblers;
using Crossdeck.Devices.WinCtrl.Drivers;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.WinCtrl.Tests.Assemblers;

public sealed class IndicatorDriverAssemblerTests
{
    [Fact]
    public void Kind_Always_IsIndicator()
    {
        // Arrange
        var sut = new IndicatorDriverAssembler(Substitute.For<ILoggerFactory>());

        // Act / Assert
        Assert.Equal(WinCtrlKind.Indicator, sut.Kind);
    }

    [Theory, AutoNSubstituteData]
    public void Assemble_Always_ReturnsWinCtrlIndicatorDeviceDriver(
        [Frozen] HidDeviceDriver hid,
        [Frozen] IHidTransport transport,
        ILoggerFactory loggerFactory)
    {
        // Arrange
        var definition = WinCtrlDefinitions.ForPid(0xBB60)!;
        var sut = new IndicatorDriverAssembler(loggerFactory);

        // Act
        var result = sut.Assemble(definition, hid, transport);

        // Assert
        Assert.IsType<WinCtrlIndicatorDeviceDriver>(result);
    }
}
