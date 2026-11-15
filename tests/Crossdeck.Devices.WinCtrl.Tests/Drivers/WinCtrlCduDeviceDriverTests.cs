using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Tests.Drivers;

public sealed class WinCtrlCduDeviceDriverTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(WinCtrlCduDeviceDriver).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Show_WhenCalled_ForwardsTheScreenToTheFrameSource(
        TextScreen screen,
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] IWinCtrlDisplayEncoder encoder,
        [Frozen] IHidReportEncoder ledEncoder)
    {
        // Arrange
        var hid = new HidDeviceDriver(identity, transport, decoder);
        var source = new CduFrameSource(encoder);
        var writer = new WinCtrlOutputWriter(transport, ledEncoder, source);
        var sut = new WinCtrlCduDeviceDriver(hid, writer, source);
        byte[] frame = [0x01, 0x02, 0x03];
        encoder.EncodeFrame(screen).Returns([frame]);

        // Act
        sut.Show(screen);
        var frames = source.TakeFrame();

        // Assert
        var taken = Assert.Single(frames!);
        Assert.Same(frame, taken);
    }

    [Theory, AutoNSubstituteData]
    public async Task Show_WhenTheWriterIsRunning_WritesTheEncodedFrame(
        TextScreen screen,
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] IWinCtrlDisplayEncoder encoder,
        [Frozen] IHidReportEncoder ledEncoder)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var hid = new HidDeviceDriver(identity, transport, decoder);
        var source = new CduFrameSource(encoder);
        var writer = new WinCtrlOutputWriter(transport, ledEncoder, source);
        var sut = new WinCtrlCduDeviceDriver(hid, writer, source);

        byte[] frame = [0x01, 0x02, 0x03];
        ledEncoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>()).Returns([]);
        encoder.EncodeInit().Returns([]);
        encoder.EncodeFrame(screen).Returns([frame]);
        transport.WriteAsync(frame, Arg.Any<CancellationToken>())
            .Returns(async _ => await cts.CancelAsync());

        // Act
        var runTask = writer.RunAsync(cts.Token);
        sut.Show(screen);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        await transport.Received(1).WriteAsync(frame, Arg.Any<CancellationToken>());
    }
}
