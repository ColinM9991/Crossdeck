using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    public static TheoryData<Type, ServiceLifetime, object?> ExpectedRegistrations => new()
    {
        { typeof(IDeviceDriverFactory), ServiceLifetime.Singleton, typeof(MozaDeviceDriverFactory) },
        { typeof(TimeProvider), ServiceLifetime.Singleton, TimeProvider.System },
    };

    [Theory]
    [MemberData(nameof(ExpectedRegistrations))]
    public void AddMozaServices_Always_RegistersExpectedServiceAsSingleton(
        Type serviceType,
        ServiceLifetime lifetime,
        object? expectedImplementation)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMozaServices();

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == serviceType);
        var actualImplementation = descriptor.ImplementationType ?? descriptor.ImplementationInstance;
        Assert.Equal(lifetime, descriptor.Lifetime);
        Assert.Equal(expectedImplementation, actualImplementation);
    }
}
