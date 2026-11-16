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

    [Theory, AutoNSubstituteData]
    public void OrderPaths_WhenUsagesDiffer_OrdersByUsage(string firstPath, string secondPath, string thirdPath)
    {
        // Arrange
        var usages = new Dictionary<string, uint>
        {
            [firstPath] = 1,
            [secondPath] = 2,
            [thirdPath] = 3
        };

        // Act
        var result = HidDeviceWatcher.OrderPaths([firstPath, secondPath, thirdPath], x => usages[x]);

        // Assert
        Assert.Equal([firstPath, secondPath, thirdPath], result);
    }

    [Theory, AutoNSubstituteData]
    public void OrderPaths_WhenTheSamePathsEnumerateInDifferentOrders_ReturnsTheSameOrder(
        string firstPath, string secondPath, string thirdPath)
    {
        // Arrange
        Func<string, uint> firstUsage = _ => 1;
        var paths = new[] { firstPath, secondPath, thirdPath };

        // Act
        var ordered = HidDeviceWatcher.OrderPaths(paths, firstUsage);
        var reordered = HidDeviceWatcher.OrderPaths(paths.Reverse(), firstUsage);

        // Assert
        Assert.Equal(ordered, reordered);
    }

    [Fact]
    public void OrderPaths_WhenUsagesTie_OrdersByPathCaseInsensitively()
    {
        // Arrange
        var paths = new[] { "bravo", "Alpha", "charlie" };
        Func<string, uint> firstUsage = _ => 1;

        // Act
        var result = HidDeviceWatcher.OrderPaths(paths, firstUsage);

        // Assert
        Assert.Equal(["Alpha", "bravo", "charlie"], result);
    }

    [Fact]
    public void KeyInterfaces_WhenTheResolverSuppliesKeys_ReturnsTheInterfacesOrderedByKey()
    {
        // Arrange
        var orderedPaths = new[] { "hid-first", "hid-second" };
        var resolverInterfaces = new[]
        {
            new HidInterfaceDescriptor(0, "HID-FIRST"),
            new HidInterfaceDescriptor(1, "hid-second")
        };

        // Act
        var result = HidDeviceWatcher.KeyInterfaces(orderedPaths, resolverInterfaces);

        // Assert
        Assert.Equal([new HidInterfaceDescriptor(0, "hid-first"), new HidInterfaceDescriptor(1, "hid-second")], result);
    }

    [Fact]
    public void KeyInterfaces_WhenNoResolverIsAvailable_UsesTheEnumerationOrdinal()
    {
        // Arrange
        var orderedPaths = new[] { "hid-first", "hid-second" };

        // Act
        var result = HidDeviceWatcher.KeyInterfaces(orderedPaths, null);

        // Assert
        Assert.Equal([new HidInterfaceDescriptor(0, "hid-first"), new HidInterfaceDescriptor(1, "hid-second")], result);
    }

    [Fact]
    public void KeyInterfaces_WhenAPathHasNoResolverKey_UsesTheOrdinalForThatPath()
    {
        // Arrange
        var orderedPaths = new[] { "hid-first", "hid-second" };
        var resolverInterfaces = new[] { new HidInterfaceDescriptor(3, "hid-first") };

        // Act
        var result = HidDeviceWatcher.KeyInterfaces(orderedPaths, resolverInterfaces);

        // Assert
        Assert.Equal([new HidInterfaceDescriptor(1, "hid-second"), new HidInterfaceDescriptor(3, "hid-first")], result);
    }

    [Fact]
    public void KeyInterfaces_WhenKeysTie_OrdersByPath()
    {
        // Arrange
        var orderedPaths = new[] { "b", "a" };
        var resolverInterfaces = new[]
        {
            new HidInterfaceDescriptor(0, "b"),
            new HidInterfaceDescriptor(0, "a")
        };

        // Act
        var result = HidDeviceWatcher.KeyInterfaces(orderedPaths, resolverInterfaces);

        // Assert
        Assert.Equal([new HidInterfaceDescriptor(0, "a"), new HidInterfaceDescriptor(0, "b")], result);
    }
}
