using Crossdeck.Application.Devices;
using Crossdeck.Application.Routing;
using Crossdeck.Application.Session;
using Crossdeck.Core.Devices;
using Crossdeck.Devices.WinCtrl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Crossdeck.Host.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHostServices_Always_RegistersASupervisorForEveryRunnable()
    {
        // Arrange
        var services = new ServiceCollection();
        var runners = new[]
        {
            typeof(InputDispatcher),
            typeof(SimValueRouter),
            typeof(DeviceRegistry),
            typeof(SimSessionSupervisor),
        };

        // Act
        services.AddHostServices();

        // Assert
        foreach (var runner in runners)
        {
            var hostedService = typeof(SupervisorService<>).MakeGenericType(runner);
            Assert.Contains(
                services,
                descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType == hostedService);
        }
    }

    [Fact]
    public void AddHostServices_Always_RegistersTheWinCtrlDriverFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHostServices();

        // Assert
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IDeviceDriverFactory)
                && descriptor.ImplementationType == typeof(WinCtrlDeviceDriverFactory));
    }
}
