using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Devices.Moza.Drivers;
using HidSharp.Reports;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class MozaDeviceDriverFactoryTests
{
    private static readonly byte[] FcdReportDescriptor =
    [
        0x05, 0x0D,         // Usage Page (Buttons)
        0x09, 0x01,         // Usage (Button 1)
        0x15, 0x00,         // Logical Minimum (0)
        0x25, 0x01,         // Logical Maximum (1)
        0x19, 0x01,         // Physical Minimum (1)
        0x2A, 0x01, 0x00,   // Physical Maximum (1)
        0x75, 0x01,         // Report Size (1)
        0x95, 0x01,         // Report Count (1)
        0x81, 0x02,         // Input (Data, Variable, Absolute)
    ];

    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(MozaDeviceDriverFactory).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Priority_Always_Returns101(
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Act
        var result = sut.Priority;

        // Assert
        Assert.Equal(101, result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_ReturnsTrue_WhenDeviceIsMa3FcdDisplay(
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        var descriptor = CreateDescriptor(0x346E, 0x1305);

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.True(result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_ReturnsTrue_WhenDeviceIsMb7FcdDisplay(
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        var descriptor = CreateDescriptor(0x346E, 0x1306);

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.True(result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_ReturnsFalse_WhenVendorIdIsNotMoza(
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        var descriptor = CreateDescriptor(0x1234, 0x1305);

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_ReturnsFalse_WhenProductIdIsNotAnFcdDisplay(
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        var descriptor = CreateDescriptor(0x346E, 0x1307);

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_ReturnsFcdDeviceDriver_WhenHidConnectionIsCreated(
        DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory hidTransportFactory,
        [Frozen] IHidTransport transport,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        var connection = new HidConnection(transport, new ReportDescriptor(FcdReportDescriptor));
        hidTransportFactory.CreateAsync(descriptor.Path).Returns(connection);

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        var driver = Assert.IsType<MozaFcdDeviceDriver>(result);
        Assert.Equal(descriptor.Identity, driver.Identity);
        await hidTransportFactory.Received(1).CreateAsync(descriptor.Path);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_ReturnsNull_WhenTransportFactoryThrowsDeviceNotUsableException(
        DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        hidTransportFactory.CreateAsync(descriptor.Path).Returns(Task.FromException<HidConnection>(
            new DeviceNotUsableException(descriptor.Path)));

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_ReturnsNull_WhenTransportFactoryThrowsDeviceNotFoundException(
        DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        hidTransportFactory.CreateAsync(descriptor.Path).Returns(Task.FromException<HidConnection>(
            new DeviceNotFoundException(descriptor.Path)));

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTransportFactoryThrowsUnexpectedException_PropagatesException(
        DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory hidTransportFactory,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        hidTransportFactory.CreateAsync(descriptor.Path).Returns(Task.FromException<HidConnection>(
            new InvalidOperationException("boom")));

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(descriptor, CancellationToken.None));
    }

    private static DeviceDescriptor CreateDescriptor(int vendorId, int productId) => new(
        new DeviceIdentity(DeviceIdentitySource.Serial, vendorId, productId, "FCD001"),
        DeviceKind.Hid,
        "path",
        "Moza FCD");
}
