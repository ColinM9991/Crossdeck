using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Devices.Moza.Drivers;
using Crossdeck.Devices.Serial;
using Crossdeck.Devices.Serial.Pairing;
using HidSharp.Reports;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class MozaDeviceDriverFactoryTests
{
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
        MozaDeviceDriverFactory sut)
    {
        // Act
        var result = sut.Priority;

        // Assert
        Assert.Equal(101, result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_ReturnsTrue_WhenDeviceIsMa3FcdDisplay(
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
        [Frozen] ISerialPortLocator serialPortLocator,
        [Frozen] ISerialTransportFactory serialTransportFactory,
        [Frozen] ILoggerFactory loggerFactory,
        string portName,
        MozaDeviceDriverFactory sut)
    {
        // Arrange
        descriptor = descriptor with { SerialInterfaces = [new SerialInterfaceDescriptor(portName)] };
        var connection = new HidConnection(transport, new ReportDescriptor(TestReportDescriptor.Fcd));
        hidTransportFactory.CreateAsync(descriptor.Path).Returns(connection);

        serialPortLocator.FindSiblingPort(descriptor).Returns(portName);
        serialTransportFactory.Create(Arg.Any<SerialSettings>()).Returns(Substitute.For<ISerialTransport>());
        loggerFactory.CreateLogger<FcdSession>().Returns(Substitute.For<ILogger<FcdSession>>());

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
