using Crossdeck.Core.Devices;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMozaServices_Always_RegistersMozaDeviceDriverFactoryAsSingleton()
    {
        // Arrange
        var provider = CreateServiceProvider();

        // Act
        var first = provider.GetRequiredService<IDeviceDriverFactory>();
        var second = provider.GetRequiredService<IDeviceDriverFactory>();

        // Assert
        Assert.IsType<MozaDeviceDriverFactory>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void AddMozaServices_Always_RegistersSystemTimeProvider()
    {
        // Arrange
        var provider = CreateServiceProvider();

        // Act
        var result = provider.GetRequiredService<TimeProvider>();

        // Assert
        Assert.Same(TimeProvider.System, result);
    }

    private static ServiceProvider CreateServiceProvider() => new ServiceCollection()
        .AddSingleton(Substitute.For<IHidTransportFactory>())
        .AddMozaServices()
        .BuildServiceProvider();
}
