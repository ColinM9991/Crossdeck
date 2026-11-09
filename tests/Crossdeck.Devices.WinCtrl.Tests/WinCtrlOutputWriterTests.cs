using AutoFixture.Xunit2;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Devices.WinCtrl.Frames;
using Crossdeck.Testing;
using NSubstitute;
using Xunit;

namespace Crossdeck.Devices.WinCtrl.Tests;

public sealed class WinCtrlOutputWriterTests
{
    [Theory, AutoNSubstituteData]
    public async Task RunAsync_OnStart_WritesLedStateThenInitPacketsInOrder(
        byte[] ledData,
        byte[] initPacket,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportEncoder ledEncoder,
        [Frozen] IWinCtrlFrameSource frames,
        WinCtrlOutputWriter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        ledEncoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>())
            .Returns([new HidOutputReport(HidReportType.Output, ledData)]);
        frames.EncodeInit().Returns([initPacket]);
        transport.WriteAsync(initPacket, Arg.Any<CancellationToken>())
            .Returns(async _ => await cts.CancelAsync());

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        Received.InOrder(() =>
        {
            transport.WriteAsync(ledData, Arg.Any<CancellationToken>());
            transport.WriteAsync(initPacket, Arg.Any<CancellationToken>());
        });
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenMarkedDirtyAfterTheInitialPass_WritesTheNewlyAvailableFrame(
        byte[] framePacket,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportEncoder ledEncoder,
        [Frozen] IWinCtrlFrameSource frames,
        WinCtrlOutputWriter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        ledEncoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>()).Returns([]);
        frames.EncodeInit().Returns([]);
        frames.TakeFrame().Returns(
            _ => null,
            _ =>
            {
                cts.Cancel();
                return [framePacket];
            });

        var runTask = sut.RunAsync(cts.Token);

        // Act
        sut.MarkDirty();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        await transport.Received(1).WriteAsync(framePacket, Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenNoFrameIsAvailable_WritesOnlyLedState(
        byte[] ledData,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportEncoder ledEncoder,
        [Frozen] IWinCtrlFrameSource frames,
        WinCtrlOutputWriter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        frames.EncodeInit().Returns([]);
        ledEncoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>())
            .Returns([new HidOutputReport(HidReportType.Output, ledData)]);
        frames.TakeFrame().Returns(_ =>
        {
            cts.Cancel();
            return null;
        });

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await transport.Received().WriteAsync(ledData, Arg.Any<CancellationToken>());
    }
}
