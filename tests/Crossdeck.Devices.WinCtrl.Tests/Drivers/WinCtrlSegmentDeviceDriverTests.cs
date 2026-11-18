using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;
using Crossdeck.Devices.WinCtrl.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.WinCtrl.Tests.Drivers;

public sealed class WinCtrlSegmentDeviceDriverTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(WinCtrlSegmentDeviceDriver).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void SetText_WhenCalled_ForwardsTheTextToTheFrameSource(
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] IHidReportEncoder ledEncoder,
        ILogger<HidDeviceDriver> hidLogger,
        ILogger<WinCtrlOutputWriter> writerLogger)
    {
        // Arrange
        var hid = new HidDeviceDriver(identity, transport, decoder, hidLogger);
        var source = new SegmentFrameSource(new WinCtrlSegmentEncoder([Panel()]));
        var writer = new WinCtrlOutputWriter(transport, ledEncoder, source, writerLogger);
        var sut = new WinCtrlSegmentDeviceDriver(hid, writer, source);
        var display = new OutputId(OutputKind.Display, "spd");
        const string text = "123";

        Assert.Null(source.TakeFrame());

        // Act
        sut.SetText(display, text);
        var frames = source.TakeFrame();

        // Assert
        Assert.Equal(2, frames!.Length);
        Assert.Equal(0xF0, frames[0][0]);
    }

    [Theory, AutoNSubstituteData]
    public void Set_WhenTheFlagValueIsNonZero_SetsTheFlagBitInTheFrame(
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] IHidReportEncoder ledEncoder,
        ILogger<HidDeviceDriver> hidLogger,
        ILogger<WinCtrlOutputWriter> writerLogger)
    {
        // Arrange
        var hid = new HidDeviceDriver(identity, transport, decoder, hidLogger);
        var source = new SegmentFrameSource(new WinCtrlSegmentEncoder([Panel()]));
        var writer = new WinCtrlOutputWriter(transport, ledEncoder, source, writerLogger);
        var sut = new WinCtrlSegmentDeviceDriver(hid, writer, source);
        var flag = new OutputId(OutputKind.Flag, "spd.mach");

        // Act
        sut.Set(flag, 1);
        var frames = source.TakeFrame();

        // Assert
        Assert.Equal(2, frames!.Length);
        Assert.Equal(0x80, frames[0][25]);
    }

    [Theory, AutoNSubstituteData]
    public void Set_WhenTheFlagValueIsZero_LeavesTheFlagBitClear(
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] IHidReportEncoder ledEncoder,
        ILogger<HidDeviceDriver> hidLogger,
        ILogger<WinCtrlOutputWriter> writerLogger)
    {
        // Arrange
        var hid = new HidDeviceDriver(identity, transport, decoder, hidLogger);
        var source = new SegmentFrameSource(new WinCtrlSegmentEncoder([Panel()]));
        var writer = new WinCtrlOutputWriter(transport, ledEncoder, source, writerLogger);
        var sut = new WinCtrlSegmentDeviceDriver(hid, writer, source);
        var flag = new OutputId(OutputKind.Flag, "spd.mach");

        // Act
        sut.Set(flag, 0);
        var frames = source.TakeFrame();

        // Assert
        Assert.Equal(2, frames!.Length);
        Assert.Equal(0x00, frames[0][25]);
    }

    [Theory, AutoNSubstituteData]
    public async Task SetText_WhenTheDriverIsRunning_WritesTheSegmentFrameToTheTransport(
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] IHidReportEncoder ledEncoder,
        ILogger<HidDeviceDriver> hidLogger,
        ILogger<WinCtrlOutputWriter> writerLogger)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var hid = new HidDeviceDriver(identity, transport, decoder, hidLogger);
        var source = new SegmentFrameSource(new WinCtrlSegmentEncoder([Panel()]));
        var writer = new WinCtrlOutputWriter(transport, ledEncoder, source, writerLogger);
        var sut = new WinCtrlSegmentDeviceDriver(hid, writer, source);

        var frameWritten = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ledEncoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>()).Returns([]);
        transport.MaxInputReportLength.Returns(2);
        transport.WriteAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                if (info.Arg<ReadOnlyMemory<byte>>().Length == 64) frameWritten.TrySetResult();
                return Task.CompletedTask;
            });
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await frameWritten.Task;
                await cts.CancelAsync();
                return 0;
            });

        // Act
        var runTask = sut.RunAsync(cts.Token);
        sut.SetText(new OutputId(OutputKind.Display, "spd"), "123");

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        await transport.Received(2).WriteAsync(
            Arg.Is<ReadOnlyMemory<byte>>(data => data.Length == 64 && data.ToArray()[0] == 0xF0),
            Arg.Any<CancellationToken>());
    }

    private static SegmentPanel Panel()
    {
        var bars = Enumerable
            .Range(0, 8)
            .Select(k => (BitAddr?)new BitAddr(0, (byte)(1 << k)))
            .ToArray();
        var window = new SegmentWindow([new DigitSlot(bars), new DigitSlot(bars), new DigitSlot(bars)]);

        return new SegmentPanel(
            "spd",
            0xBF0F,
            1,
            new Dictionary<string, SegmentWindow> { ["spd"] = window },
            new Dictionary<string, BitAddr[]> { ["mach"] = [new BitAddr(0, 0x80)] },
            new SegmentFraming(5, 0x0050A2DF, 0x0050A2DF, true));
    }
}
