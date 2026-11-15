using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;
using HidSharp.Reports;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class HidDeviceDriverFactoryTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(HidDeviceDriverFactory).GetConstructors());
    }

    [Fact]
    public void Priority_Always_IsZero()
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(Substitute.For<IHidTransportFactory>());

        // Act / Assert
        Assert.Equal(0, sut.Priority);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_WhenDeviceIsHid_ReturnsTrue([Frozen] DeviceDescriptor descriptor)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(Substitute.For<IHidTransportFactory>());
        descriptor = descriptor with { Kind = DeviceKind.Hid };

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.True(result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_WhenDeviceIsNotHid_ReturnsFalse([Frozen] DeviceDescriptor descriptor)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(Substitute.For<IHidTransportFactory>());
        descriptor = descriptor with { Kind = DeviceKind.Serial };

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTransportIsAvailable_ReturnsDriverWithTheDecodedControls(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(transportFactory);
        descriptor = descriptor with { Kind = DeviceKind.Hid };
        var connection = new HidConnection(
            Substitute.For<IHidTransport>(),
            new ReportDescriptor(TestReportDescriptor.Button));
        transportFactory.CreateAsync(descriptor.Path).Returns(connection);

        // Act
        var driver = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        var deviceDriver = Assert.IsType<HidDeviceDriver>(driver);
        Assert.Equal(descriptor.Identity, deviceDriver.Identity);
        var control = Assert.IsType<ButtonControl>(Assert.Single(deviceDriver.Controls));
        Assert.Equal("Button 1", control.Name);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTheTransportIsNotUsable_ReturnsNull(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(transportFactory);
        transportFactory
            .CreateAsync(descriptor.Path)
            .Returns(Task.FromException<HidConnection>(new DeviceNotUsableException(descriptor.Path)));

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTheDeviceIsNotFound_ReturnsNull(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(transportFactory);
        transportFactory
            .CreateAsync(descriptor.Path)
            .Returns(Task.FromException<HidConnection>(new DeviceNotFoundException(descriptor.Path)));

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTheTransportFactoryThrowsUnexpectedException_PropagatesIt(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(transportFactory);
        var failure = new InvalidOperationException();
        transportFactory
            .CreateAsync(descriptor.Path)
            .Returns(Task.FromException<HidConnection>(failure));

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(descriptor, CancellationToken.None));
    }
}
