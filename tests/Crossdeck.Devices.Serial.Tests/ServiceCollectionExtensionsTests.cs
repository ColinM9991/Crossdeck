using Crossdeck.Devices.Serial;
using Crossdeck.Devices.Serial.Pairing;

namespace Crossdeck.Devices.Serial.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSerialServices_Always_RegistersTheSerialTransportFactory()
    {
        // Arrange
        var provider = new ServiceCollection().AddSerialServices().BuildServiceProvider();

        // Act
        var factory = provider.GetRequiredService<ISerialTransportFactory>();

        // Assert
        Assert.IsType<SerialTransportFactory>(factory);
    }

    [Fact]
    public void AddSerialServices_Always_RegistersThePlatformSerialPortLocator()
    {
        // Arrange
        var provider = new ServiceCollection().AddSerialServices().BuildServiceProvider();

        // Act
        var locator = provider.GetRequiredService<ISerialPortLocator>();

        // Assert
        var expected = OperatingSystem.IsWindows() ? typeof(WindowsSerialPortLocator) : typeof(NullSerialPortLocator);
        Assert.IsAssignableFrom(expected, locator);
    }
}
