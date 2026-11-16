using System.Collections.Concurrent;
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
            HidInterfaces = [new HidInterfaceDescriptor(0, "hid-first"), new HidInterfaceDescriptor(1, "hid-second")]
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
    public async Task CreateAsync_WithMultipleHidInterfaces_ForwardsInputEventsWithThePerInterfaceIdentity(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory)
    {
        // Arrange
        var sut = new HidDeviceDriverFactory(transportFactory);
        descriptor = descriptor with
        {
            Kind = DeviceKind.Hid,
            HidInterfaces = [new HidInterfaceDescriptor(0, "hid-first"), new HidInterfaceDescriptor(1, "hid-second")]
        };

        using var cts = new CancellationTokenSource();
        var firstTransport = Substitute.For<IHidTransport>();
        firstTransport.MaxInputReportLength.Returns(2);
        firstTransport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var report = call.ArgAt<Memory<byte>>(0).Span;
                report[0] = 0x00;
                report[1] = 0x01;
                await cts.CancelAsync();
                return 2;
            });

        var secondTransport = Substitute.For<IHidTransport>();
        secondTransport.MaxInputReportLength.Returns(2);
        secondTransport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var report = call.ArgAt<Memory<byte>>(0).Span;
                report[0] = 0x00;
                report[1] = 0x01;
                await cts.CancelAsync();
                return 2;
            });

        var firstConnection = new HidConnection(firstTransport, new ReportDescriptor(TestReportDescriptor.Button));
        var secondConnection = new HidConnection(secondTransport, new ReportDescriptor(TestReportDescriptor.Button));
        transportFactory.CreateAsync("hid-first").Returns(firstConnection);
        transportFactory.CreateAsync("hid-second").Returns(secondConnection);

        // Act
        var compositeDriver = Assert.IsType<CompositeHidDeviceDriver>(await sut.CreateAsync(descriptor, CancellationToken.None));
        var events = new ConcurrentBag<DeviceInputEvent>();
        compositeDriver.InputReceived += (_, e) => events.Add(e);
        await compositeDriver.RunAsync(cts.Token);

        // Assert
        var expectedIdentities = new[]
        {
            new DeviceIdentity(descriptor.Identity.Kind, descriptor.Identity.VendorId, descriptor.Identity.ProductId,
                $"{descriptor.Identity.Value}@if=0"),
            new DeviceIdentity(descriptor.Identity.Kind, descriptor.Identity.VendorId, descriptor.Identity.ProductId,
                $"{descriptor.Identity.Value}@if=1")
        };
        Assert.Equal(expectedIdentities.OrderBy(x => x.Value), events.Select(x => x.Identity).OrderBy(x => x.Value));
        Assert.Equal(2, compositeDriver.Controls.Count);
        Assert.All(events, x => Assert.Equal(new InputId(InputKind.Button, "1"), x.Control.Id));
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
            HidInterfaces = [new HidInterfaceDescriptor(0, "hid-first"), new HidInterfaceDescriptor(1, "hid-second")]
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
