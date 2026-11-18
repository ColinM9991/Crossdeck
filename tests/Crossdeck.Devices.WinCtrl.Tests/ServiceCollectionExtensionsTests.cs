using Crossdeck.Core.Devices;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Assemblers;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.WinCtrl.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWinCtrlDevices_Always_RegistersTheAssemblersAndTheDriverFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IHidTransportFactory>());
        services.AddSingleton(Substitute.For<ILoggerFactory>());

        // Act
        services.AddWinCtrlDevices();
        using var provider = services.BuildServiceProvider();

        // Assert
        var assemblers = provider.GetServices<IWinCtrlDriverAssembler>().ToList();
        Assert.Contains(assemblers, assembler => assembler is CduDriverAssembler);
        Assert.Contains(assemblers, assembler => assembler is SegmentDriverAssembler);
        Assert.Contains(assemblers, assembler => assembler is IndicatorDriverAssembler);
        Assert.IsType<WinCtrlDeviceDriverFactory>(provider.GetRequiredService<IDeviceDriverFactory>());
    }
}
