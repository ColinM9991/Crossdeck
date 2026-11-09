using Crossdeck.Core.Devices;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHidServices_Always_RegistersDeviceWatcherAsSingleton()
    {
        // Arrange
        var provider = new ServiceCollection().AddHidServices().BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<IDeviceWatcher>();
        var second = provider.GetRequiredService<IDeviceWatcher>();

        // Assert
        Assert.IsType<HidDeviceWatcher>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void AddHidServices_Always_RegistersDeviceDriverFactoryAsSingleton()
    {
        // Arrange
        var provider = new ServiceCollection().AddHidServices().BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<IDeviceDriverFactory>();
        var second = provider.GetRequiredService<IDeviceDriverFactory>();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void AddHidServices_Always_RegistersHidTransportFactoryAsSingleton()
    {
        // Arrange
        var provider = new ServiceCollection().AddHidServices().BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<IHidTransportFactory>();
        var second = provider.GetRequiredService<IHidTransportFactory>();

        // Assert
        Assert.Same(first, second);
    }
}
