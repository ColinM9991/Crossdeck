using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;
using HidSharp.Reports;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class CompositeHidDeviceDriverTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(CompositeHidDeviceDriver).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Identity_Always_ReturnsConstructorValue(
        [Frozen] DeviceIdentity identity,
        CompositeHidDeviceDriver sut)
    {
        // Act
        var result = sut.Identity;

        // Assert
        Assert.Equal(identity, result);
    }

    [Theory, AutoNSubstituteData]
    public void Controls_WhenInnerDriversShareAnInputId_ReturnsEveryControl(
        DeviceIdentity identity,
        IHidTransport firstTransport,
        IHidTransport secondTransport,
        ILogger<HidDeviceDriver> logger)
    {
        // Arrange
        var firstDriver = new HidDeviceDriver(identity, firstTransport,
            new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button)), logger);
        var secondDriver = new HidDeviceDriver(identity, secondTransport,
            new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button)), logger);

        // Act
        var sut = new CompositeHidDeviceDriver(identity, [firstDriver, secondDriver]);

        // Assert
        var controls = sut.Controls;
        Assert.Equal(2, controls.Count);
        Assert.All(controls, x => Assert.Equal(new InputId(InputKind.Button, "1"), x.Id));
    }

    [Theory, AutoNSubstituteData]
    public void Controls_WhenInnerDriversCarryInterfaceKeys_SuffixesEveryControlIdAndName(
        DeviceIdentity identity,
        IHidTransport firstTransport,
        IHidTransport secondTransport,
        ILogger<HidDeviceDriver> logger)
    {
        // Arrange
        var firstIdentity = new DeviceIdentity(identity.Kind, identity.VendorId, identity.ProductId, $"{identity.Value}@if=0");
        var secondIdentity = new DeviceIdentity(identity.Kind, identity.VendorId, identity.ProductId, $"{identity.Value}@if=1");
        var firstDriver = new HidDeviceDriver(firstIdentity, firstTransport,
            new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button)), logger);
        var secondDriver = new HidDeviceDriver(secondIdentity, secondTransport,
            new HidReportDecoder(new ReportDescriptor(TestReportDescriptor.Button)), logger);

        // Act
        var sut = new CompositeHidDeviceDriver(identity, [firstDriver, secondDriver]);

        // Assert
        var controls = sut.Controls;
        Assert.Equal(2, controls.Count);
        Assert.Equal(new InputId(InputKind.Button, "1@if=0"), controls[0].Id);
        Assert.Equal("Button 1 (if 0)", controls[0].Name);
        Assert.Equal(new InputId(InputKind.Button, "1@if=1"), controls[1].Id);
        Assert.Equal("Button 1 (if 1)", controls[1].Name);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAnInnerDriverRaisesInput_ForwardsTheInputEvent(
        [Frozen] DeviceIdentity identity,
        ButtonControl button,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        ILogger<HidDeviceDriver> logger)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sut = new CompositeHidDeviceDriver(identity,
            [new HidDeviceDriver(identity, transport, decoder, logger)]);

        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return 8;
            });

        decoder.When(d => d.Decode(Arg.Any<byte[]>(), Arg.Any<List<HidInputChange>>()))
            .Do(call => call.ArgAt<List<HidInputChange>>(1).Add(new HidInputChange(button, 1)));

        DeviceInputEvent? raised = null;
        sut.InputReceived += (_, e) => raised = e;

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        Assert.Equal(identity, raised?.Identity);
        Assert.Equal(InputEventKind.Pressed, raised?.Kind);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAnInnerDriverRaisesInput_RaisesTheEventWithTheRemappedControl(
        [Frozen] DeviceIdentity identity,
        ButtonControl button,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        ILogger<HidDeviceDriver> logger)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var driverIdentity = new DeviceIdentity(identity.Kind, identity.VendorId, identity.ProductId,
            $"{identity.Value}@if=0");

        IReadOnlyList<DeviceControl> controls = new[] { button };
        decoder.Controls.Returns(controls);
        decoder.When(d => d.Decode(Arg.Any<byte[]>(), Arg.Any<List<HidInputChange>>()))
            .Do(call => call.ArgAt<List<HidInputChange>>(1).Add(new HidInputChange(button, 1)));

        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return 8;
            });

        var sut = new CompositeHidDeviceDriver(identity,
            [new HidDeviceDriver(driverIdentity, transport, decoder, logger)]);

        DeviceInputEvent? raised = null;
        sut.InputReceived += (_, e) => raised = e;

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        Assert.Equal(driverIdentity, raised?.Identity);
        Assert.Equal(new InputId(button.Id.Kind, $"{button.Id.Name}@if=0"), raised?.Control.Id);
        Assert.Equal($"{button.Name} (if 0)", raised?.Control.Name);
    }

    [Theory, AutoNSubstituteData]
    public async Task DisposeAsync_Always_DisposesEveryInnerDriver(
        DeviceIdentity identity,
        IHidTransport firstTransport,
        IHidTransport secondTransport,
        IHidReportDecoder decoder,
        ILogger<HidDeviceDriver> logger)
    {
        // Arrange
        var sut = new CompositeHidDeviceDriver(identity,
        [
            new HidDeviceDriver(identity, firstTransport, decoder, logger),
            new HidDeviceDriver(identity, secondTransport, decoder, logger)
        ]);

        // Act
        await sut.DisposeAsync();

        // Assert
        await firstTransport.Received(1).DisposeAsync();
        await secondTransport.Received(1).DisposeAsync();
    }
}
