using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Devices.Moza.Drivers;

namespace Crossdeck.Devices.Moza.Tests.Drivers;

public sealed class MozaFcdDeviceDriverTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(MozaFcdDeviceDriver).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Identity_Always_ReturnsUnderlyingDriverIdentity(
        [Frozen] DeviceIdentity identity,
        [Frozen] HidDeviceDriver hidDeviceDriver,
        MozaFcdDeviceDriver sut)
    {
        // Act
        var result = sut.Identity;

        // Assert
        Assert.Equal(identity, result);
    }

    [Theory, AutoNSubstituteData]
    public void Controls_Always_ReturnsUnderlyingDriverControls(
        IReadOnlyList<DeviceControl> controls,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] HidDeviceDriver hidDeviceDriver,
        MozaFcdDeviceDriver sut)
    {
        // Arrange
        decoder.Controls.Returns(controls);

        // Act
        var result = sut.Controls;

        // Assert
        Assert.Same(controls, result);
    }

    [Theory, AutoNSubstituteData]
    public async Task InputReceived_WhenUnderlyingDriverRaisesEvent_ForwardsEventToSubscribers(
        ButtonControl button,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        MozaFcdDeviceDriver sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

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
        Assert.Equal(InputEventKind.Pressed, raised?.Kind);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenUnderlyingDriverCompletes_CancelsLinkedCancellationToken(
        [Frozen] IHidTransport transport,
        MozaFcdDeviceDriver sut)
    {
        // Arrange
        CancellationToken capturedToken = default;
        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedToken = callInfo.ArgAt<CancellationToken>(1);
                return 0;
            });

        // Act
        await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.True(capturedToken.IsCancellationRequested);
    }

    [Theory, AutoNSubstituteData]
    public async Task DisposeAsync_Always_DisposesUnderlyingTransport(
        [Frozen] IHidTransport transport,
        MozaFcdDeviceDriver sut)
    {
        // Act
        await sut.DisposeAsync();

        // Assert
        await transport.Received(1).DisposeAsync();
    }
}
