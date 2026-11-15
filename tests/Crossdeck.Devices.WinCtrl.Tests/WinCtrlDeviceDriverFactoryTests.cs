using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Devices.WinCtrl.Assemblers;
using HidSharp.Reports;
using NSubstitute.ExceptionExtensions;

namespace Crossdeck.Devices.WinCtrl.Tests;

public sealed class WinCtrlDeviceDriverFactoryTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        fixture.Register(() => (IEnumerable<IWinCtrlDriverAssembler>)new[]
        {
            AssemblerWithKind(WinCtrlKind.Cdu),
            AssemblerWithKind(WinCtrlKind.Segment),
            AssemblerWithKind(WinCtrlKind.Indicator),
        });
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(WinCtrlDeviceDriverFactory).GetConstructors());
    }

    [Fact]
    public void Priority_Always_Is100()
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(Substitute.For<IHidTransportFactory>(), []);

        // Act / Assert
        Assert.Equal(100, sut.Priority);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_WhenTheDeviceIsWinCtrlWithAnAvailableAssembler_ReturnsTrue(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(Substitute.For<IHidTransportFactory>(), [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0xBB32 }
        };

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.True(result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_WhenTheVendorDoesNotMatch_ReturnsFalse(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(Substitute.For<IHidTransportFactory>(), [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x1234, ProductId = 0xBB32 }
        };

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_WhenThePidIsUnknown_ReturnsFalse(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(Substitute.For<IHidTransportFactory>(), [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0x1234 }
        };

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_WhenNoAssemblerHandlesTheDeviceKind_ReturnsFalse(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(Substitute.For<IHidTransportFactory>(), [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0xBF0F }
        };

        // Act
        var result = sut.CanCreate(descriptor);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenThePidIsUnknown_ReturnsNull(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(transportFactory, [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0x1234 }
        };

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
        await transportFactory.DidNotReceive().CreateAsync(descriptor.Path);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTheTransportIsNotUsable_ReturnsNull(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(transportFactory, [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0xBB32 }
        };
        transportFactory.CreateAsync(descriptor.Path)
            .Returns(Task.FromException<HidConnection>(new DeviceNotUsableException(descriptor.Path)));

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTheDeviceIsNotFound_ReturnsNull(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(transportFactory, [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0xBB32 }
        };
        transportFactory.CreateAsync(descriptor.Path)
            .Returns(Task.FromException<HidConnection>(new DeviceNotFoundException(descriptor.Path)));

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTheDeviceIsAvailable_ReturnsTheAssembledDriver(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory,
        [Frozen] IHidTransport transport,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(transportFactory, [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0xBB32 }
        };

        var definition = WinCtrlDefinitions.ForPid(0xBB32)!;
        var connection = new HidConnection(transport, new ReportDescriptor(TestReportDescriptor.Button));
        transportFactory.CreateAsync(descriptor.Path).Returns(connection);

        var expected = Substitute.For<IDeviceDriver>();
        cduAssembler.Assemble(
                definition,
                Arg.Any<HidDeviceDriver>(),
                Arg.Any<IHidTransport>())
            .Returns(expected);

        // Act
        var result = await sut.CreateAsync(descriptor, CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
        cduAssembler.Received(1).Assemble(
            definition,
            Arg.Is<HidDeviceDriver>(driver => driver.Identity == descriptor.Identity),
            Arg.Is<IHidTransport>(assemblerTransport => ReferenceEquals(assemblerTransport, transport)));
    }

    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_WhenTheAssemblerFails_DisposesTheTransportAndPropagatesTheFailure(
        [Frozen] DeviceDescriptor descriptor,
        [Frozen] IHidTransportFactory transportFactory,
        [Frozen] IHidTransport transport,
        [Frozen] IWinCtrlDriverAssembler cduAssembler)
    {
        // Arrange
        var sut = new WinCtrlDeviceDriverFactory(transportFactory, [cduAssembler]);
        cduAssembler.Kind.Returns(WinCtrlKind.Cdu);
        descriptor = descriptor with
        {
            Identity = descriptor.Identity with { VendorId = 0x4098, ProductId = 0xBB32 }
        };
        var connection = new HidConnection(transport, new ReportDescriptor(TestReportDescriptor.Button));
        transportFactory.CreateAsync(descriptor.Path).Returns(connection);
        cduAssembler.Assemble(
                Arg.Any<WinCtrlDefinition>(),
                Arg.Any<HidDeviceDriver>(),
                Arg.Any<IHidTransport>())
            .Throws(new InvalidOperationException());

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(descriptor, CancellationToken.None));
        await transport.Received(1).DisposeAsync();
    }

    private static IWinCtrlDriverAssembler AssemblerWithKind(WinCtrlKind kind)
    {
        var assembler = Substitute.For<IWinCtrlDriverAssembler>();
        assembler.Kind.Returns(kind);
        return assembler;
    }
}
