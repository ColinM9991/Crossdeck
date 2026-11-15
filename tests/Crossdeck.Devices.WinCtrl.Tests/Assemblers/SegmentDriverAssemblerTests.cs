using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Assemblers;
using Crossdeck.Devices.WinCtrl.Drivers;

namespace Crossdeck.Devices.WinCtrl.Tests.Assemblers;

public sealed class SegmentDriverAssemblerTests
{
    [Fact]
    public void Kind_Always_IsSegment()
    {
        // Arrange
        var sut = new SegmentDriverAssembler();

        // Act / Assert
        Assert.Equal(WinCtrlKind.Segment, sut.Kind);
    }

    [Theory, AutoNSubstituteData]
    public void Assemble_WhenTheDefinitionHasNoSegmentLayout_Throws(
        [Frozen] HidDeviceDriver hid,
        [Frozen] IHidTransport transport)
    {
        // Arrange
        var definition = WinCtrlDefinitions.ForPid(0xBB32)!;
        var sut = new SegmentDriverAssembler();

        // Act / Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            sut.Assemble(definition, hid, transport);
        });
        Assert.Contains(definition.Part, exception.Message);
    }

    [Theory, AutoNSubstituteData]
    public async Task Assemble_WhenTheDeviceIsFcuWithEfisL_WritesOnlyTheKeptLights(
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        identity = identity with { ProductId = 0xBC1D };
        var hid = new HidDeviceDriver(identity, transport, decoder);
        var definition = WinCtrlDefinitions.ForPid(0xBC1D)!;
        var driver = new SegmentDriverAssembler().Assemble(definition, hid, transport);

        var ledsWritten = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var written = new List<byte[]>();
        transport.MaxInputReportLength.Returns(2);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await ledsWritten.Task;
                await cts.CancelAsync();
                return 0;
            });
        transport.WriteAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                var data = info.Arg<ReadOnlyMemory<byte>>().ToArray();
                written.Add(data);
                if (data.Length == 14) ledsWritten.TrySetResult();
                return Task.CompletedTask;
            });

        // Act
        var outputs = (IIndicatorOutput)driver;
        outputs.Set(new OutputId(OutputKind.Level, "backlight"), 128);
        outputs.Set(new OutputId(OutputKind.Level, "efisL.backlight"), 128);
        outputs.Set(new OutputId(OutputKind.Level, "efisR.backlight"), 128);
        var runTask = driver.RunAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        var targets = written
            .Where(report => report.Length == 14)
            .Select(report => (ushort)((report[2] << 8) | report[1]))
            .ToHashSet();
        Assert.Contains((ushort)0xBB10, targets);
        Assert.Contains((ushort)0xBF0D, targets);
        Assert.DoesNotContain((ushort)0xBF0E, targets);
    }

    [Theory, AutoNSubstituteData]
    public async Task Assemble_WhenTheDeviceIsNotFcu_KeepsEveryLightOfTheLayout(
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        identity = identity with { ProductId = 0xBF0F };
        var hid = new HidDeviceDriver(identity, transport, decoder);
        var definition = WinCtrlDefinitions.ForPid(0xBF0F)!;
        var driver = new SegmentDriverAssembler().Assemble(definition, hid, transport);

        var ledsWritten = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var written = new List<byte[]>();
        transport.MaxInputReportLength.Returns(2);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await ledsWritten.Task;
                await cts.CancelAsync();
                return 0;
            });
        transport.WriteAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                var data = info.Arg<ReadOnlyMemory<byte>>().ToArray();
                written.Add(data);
                if (data.Length == 14) ledsWritten.TrySetResult();
                return Task.CompletedTask;
            });

        // Act
        var outputs = (IIndicatorOutput)driver;
        outputs.Set(new OutputId(OutputKind.Level, "backlight"), 64);
        outputs.Set(new OutputId(OutputKind.Level, "lcd"), 64);
        outputs.Set(new OutputId(OutputKind.Led, "n1"), 1);
        var runTask = driver.RunAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        var lights = written
            .Where(report => report.Length == 14)
            .Select(report => report[7])
            .ToHashSet();
        Assert.Equal(new HashSet<byte> { 0x00, 0x01, 0x03 }, lights);
    }
}
