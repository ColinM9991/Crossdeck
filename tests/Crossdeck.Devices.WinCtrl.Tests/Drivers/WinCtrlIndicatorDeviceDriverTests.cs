using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Tests.Drivers;

public sealed class WinCtrlIndicatorDeviceDriverTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(WinCtrlIndicatorDeviceDriver).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public async Task Set_WhenTheIdIsNotAFlag_PassesTheValueToTheWriter(
        OutputId id,
        int value,
        byte[] frame,
        [Frozen] DeviceIdentity identity,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        [Frozen] IWinCtrlFrameSource frames,
        [Frozen] IHidReportEncoder ledEncoder)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        id = id with { Kind = OutputKind.Level };
        var hid = new HidDeviceDriver(identity, transport, decoder);
        var writer = new WinCtrlOutputWriter(transport, ledEncoder, frames);
        var sut = new WinCtrlIndicatorDeviceDriver(hid, writer);

        IReadOnlyDictionary<OutputId, int> state = new Dictionary<OutputId, int>();
        ledEncoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>())
            .Returns(info =>
            {
                state = info.ArgAt<IReadOnlyDictionary<OutputId, int>>(0);
                return [];
            });
        frames.EncodeInit().Returns([]);
        frames.TakeFrame().Returns(
            _ => null,
            _ =>
            {
                cts.Cancel();
                return [frame];
            });

        // Act
        var runTask = writer.RunAsync(cts.Token);
        sut.Set(id, value);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Equal(value, state[id]);
    }
}
