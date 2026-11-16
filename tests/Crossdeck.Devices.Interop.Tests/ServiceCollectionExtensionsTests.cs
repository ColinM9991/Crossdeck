using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Interop.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInteropServices_Always_RegistersThePlatformUsbDeviceResolver()
    {
        // Arrange
        var provider = new ServiceCollection().AddInteropServices().BuildServiceProvider();

        // Act
        var resolver = provider.GetRequiredService<IUsbDeviceResolver>();

        // Assert
        var expected = OperatingSystem.IsWindows() ? typeof(WindowsUsbDeviceResolver) : typeof(NullUsbDeviceResolver);
        Assert.IsAssignableFrom(expected, resolver);
    }
}
