using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class HidDeviceWatcherTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(HidDeviceWatcher).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void BuildDeviceIdentity_WithSerial_ReturnsSerialIdentity(
        string serial, int vendorId, int productId, uint usage)
    {
        // Act
        var result = HidDeviceWatcher.BuildDeviceIdentity(vendorId, productId, serial, usage);

        // Assert
        Assert.Equal(new DeviceIdentity(DeviceIdentitySource.Serial, vendorId, productId, serial.Trim()), result);
    }

    [Theory, AutoNSubstituteData]
    public void BuildDeviceIdentity_WithoutSerial_ReturnsUsageIdentity(int vendorId, int productId, uint usage)
    {
        // Act
        var result = HidDeviceWatcher.BuildDeviceIdentity(vendorId, productId, null, usage);

        // Assert
        Assert.Equal(new DeviceIdentity(DeviceIdentitySource.Usage, vendorId, productId, HidDeviceWatcher.FormatUsage(usage)), result);
    }

    [Theory, AutoNSubstituteData]
    public void BuildDeviceIdentity_WithEmptySerial_ReturnsUsageIdentity(int vendorId, int productId, uint usage)
    {
        // Act
        var result = HidDeviceWatcher.BuildDeviceIdentity(vendorId, productId, "", usage);

        // Assert
        Assert.Equal(new DeviceIdentity(DeviceIdentitySource.Usage, vendorId, productId, HidDeviceWatcher.FormatUsage(usage)), result);
    }

    [Theory, AutoNSubstituteData]
    public void BuildDeviceIdentity_WithWhitespaceSerial_ReturnsUsageIdentity(int vendorId, int productId, uint usage)
    {
        // Act
        var result = HidDeviceWatcher.BuildDeviceIdentity(vendorId, productId, "   ", usage);

        // Assert
        Assert.Equal(new DeviceIdentity(DeviceIdentitySource.Usage, vendorId, productId, HidDeviceWatcher.FormatUsage(usage)), result);
    }

    [Theory, AutoNSubstituteData]
    public void BuildDeviceIdentity_WithPaddedSerial_ReturnsTrimmedSerialIdentity(
        int vendorId, int productId, uint usage)
    {
        // Act
        var result = HidDeviceWatcher.BuildDeviceIdentity(vendorId, productId, " 8B4238001422361051353730 ", usage);

        // Assert
        Assert.Equal(new DeviceIdentity(DeviceIdentitySource.Serial, vendorId, productId, "8B4238001422361051353730"), result);
    }
}
