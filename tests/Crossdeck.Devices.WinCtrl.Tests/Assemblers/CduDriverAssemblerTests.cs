using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Assemblers;
using Crossdeck.Devices.WinCtrl.Drivers;

namespace Crossdeck.Devices.WinCtrl.Tests.Assemblers;

public sealed class CduDriverAssemblerTests
{
    [Fact]
    public void Kind_Always_IsCdu()
    {
        // Arrange
        var sut = new CduDriverAssembler();

        // Act / Assert
        Assert.Equal(WinCtrlKind.Cdu, sut.Kind);
    }

    [Theory, AutoNSubstituteData]
    public void Assemble_WhenTheDefinitionHasCduScreenSettings_ReturnsWinCtrlCduDeviceDriver(
        [Frozen] HidDeviceDriver hid,
        [Frozen] IHidTransport transport)
    {
        // Arrange
        var definition = WinCtrlDefinitions.ForPid(0xBB32)!;
        var sut = new CduDriverAssembler();

        // Act
        var result = sut.Assemble(definition, hid, transport);

        // Assert
        Assert.IsType<WinCtrlCduDeviceDriver>(result);
    }
}
