using Crossdeck.Application.Devices;
using Crossdeck.Application.Routing;
using Crossdeck.Application.Session;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Session;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Crossdeck.Application.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSimulationServices_Always_ResolvesSameSimSessionSupervisorInstanceForAllItsInterfaces()
    {
        // Arrange
        var provider = new ServiceCollection().AddSimulationServices().BuildServiceProvider();

        // Act
        var concrete = provider.GetRequiredService<SimSessionSupervisor>();
        var activeSim = provider.GetRequiredService<IActiveSim>();
        var supervisor = provider.GetRequiredService<ISimSessionSupervisor>();

        // Assert
        Assert.Same(concrete, activeSim);
        Assert.Same(concrete, supervisor);
    }

    [Fact]
    public void AddSimulationServices_Always_ResolvesSameDeviceRegistryInstanceForIDeviceRegistry()
    {
        // Arrange
        var provider = new ServiceCollection().AddSimulationServices().BuildServiceProvider();

        // Act
        var concrete = provider.GetRequiredService<DeviceRegistry>();
        var registry = provider.GetRequiredService<IDeviceRegistry>();

        // Assert
        Assert.Same(concrete, registry);
    }

    [Fact]
    public void AddSimulationServices_Always_ResolvesSameInputDispatcherInstanceForAllItsInterfaces()
    {
        // Arrange
        var provider = new ServiceCollection().AddSimulationServices().BuildServiceProvider();

        // Act
        var concrete = provider.GetRequiredService<InputDispatcher>();
        var inputSink = provider.GetRequiredService<IInputSink>();
        var inputMonitor = provider.GetRequiredService<IInputMonitor>();
        var dispatcher = provider.GetRequiredService<IInputDispatcher>();

        // Assert
        Assert.Same(concrete, inputSink);
        Assert.Same(concrete, inputMonitor);
        Assert.Same(concrete, dispatcher);
    }
}
