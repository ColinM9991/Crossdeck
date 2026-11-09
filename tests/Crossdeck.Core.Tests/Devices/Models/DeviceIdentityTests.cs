using Crossdeck.Core.Devices.Models;
using Crossdeck.Testing;
using Xunit;

namespace Crossdeck.Core.Tests.Devices.Models;

public sealed class DeviceIdentityTests
{
    [Theory, AutoNSubstituteData]
    public void ToString_Always_FormatsAsVendorProductKindAndValue(
        DeviceIdentitySource kind,
        int vendorId,
        int productId,
        string value)
    {
        // Arrange
        var identity = new DeviceIdentity(kind, vendorId, productId, value);

        // Act
        var result = identity.ToString();

        // Assert
        Assert.Equal($"{vendorId:X4}:{productId:X4}:{kind.ToString().ToLowerInvariant()}={value}", result);
    }
}
