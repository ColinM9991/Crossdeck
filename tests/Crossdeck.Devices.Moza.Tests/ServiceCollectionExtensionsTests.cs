using Crossdeck.Core.Devices;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMozaServices_Always_RegistersMozaDeviceDriverFactoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMozaServices();

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IDeviceDriverFactory));
        var actualImplementation = descriptor.ImplementationType ?? descriptor.ImplementationInstance;
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(MozaDeviceDriverFactory), actualImplementation);
    }

    [Theory, AutoNSubstituteData]
    public void AddMozaServices_Always_RegistersTimeProviderAsSingleton(
        TimeProvider timeProvider)
    {
        // Arrange
        var services = new ServiceCollection();
        services.TryAddSingleton(timeProvider);

        // Act
        services.AddMozaServices();

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(TimeProvider));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Same(timeProvider, descriptor.ImplementationInstance);
    }
}
