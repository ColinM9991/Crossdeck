using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Serial.Pairing;

namespace Crossdeck.Devices.Serial.Tests.Pairing;

public sealed class NullSerialPortLocatorTests
{
    [Theory, AutoNSubstituteData]
    public void FindSiblingPort_Always_ReturnsNull(DeviceDescriptor descriptor)
    {
        // Arrange
        var sut = new NullSerialPortLocator();

        // Act / Assert
        Assert.Null(sut.FindSiblingPort(descriptor));
    }
}
