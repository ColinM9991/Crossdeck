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
        descriptor = descriptor with { Kind = DeviceKind.Hid, HidInterfaces = [] };
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
        descriptor = descriptor with { HidInterfaces = [] };
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
        descriptor = descriptor with { HidInterfaces = [] };
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
        descriptor = descriptor with { HidInterfaces = [] };
        var failure = new InvalidOperationException();
        transportFactory
            .CreateAsync(descriptor.Path)
            .Returns(Task.FromException<HidConnection>(failure));

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(descriptor, CancellationToken.None));
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WithMultipleHidInterfaces_ReturnsCompositeDriver(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(transportFactory);
        descriptor = descriptor with
        {
            Kind = DeviceKind.Hid,
            HidInterfaces = [new HidInterfaceDescriptor("hid-first"), new HidInterfaceDescriptor("hid-second")]
        };

        var firstConnection = new HidConnection(
            Substitute.For<IHidTransport>(),
            new ReportDescriptor(TestReportDescriptor.Button));
        var secondConnection = new HidConnection(
            Substitute.For<IHidTransport>(),
            new ReportDescriptor(TestReportDescriptor.Button));
        transportFactory.CreateAsync("hid-first").Returns(firstConnection);
        transportFactory.CreateAsync("hid-second").Returns(secondConnection);

        // Act
        var driver = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        var compositeDriver = Assert.IsType<CompositeHidDeviceDriver>(driver);
        Assert.Equal(descriptor.Identity, compositeDriver.Identity);
        await transportFactory.Received(1).CreateAsync("hid-first");
        await transportFactory.Received(1).CreateAsync("hid-second");
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenACompositeInterfaceCannotOpen_ReturnsNullAndDisposesOpenConnections(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(transportFactory);
        descriptor = descriptor with
        {
            Kind = DeviceKind.Hid,
            HidInterfaces = [new HidInterfaceDescriptor("hid-first"), new HidInterfaceDescriptor("hid-second")]
        };

        var firstTransport = Substitute.For<IHidTransport>();
        transportFactory.CreateAsync("hid-first").Returns(
            new HidConnection(firstTransport, new ReportDescriptor(TestReportDescriptor.Button)));
        transportFactory.CreateAsync("hid-second").Returns(
            Task.FromException<HidConnection>(new DeviceNotUsableException("hid-second")));

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
        await firstTransport.Received(1).DisposeAsync();
    }
}
