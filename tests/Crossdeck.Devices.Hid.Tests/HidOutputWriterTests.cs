using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class HidOutputWriterTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(HidOutputWriter).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_OnStart_ResetsEncoderBeforeWaitingForDirtyState(
        [Frozen] IHidReportEncoder encoder,
        HidOutputWriter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        encoder.Received(1).Reset();
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_AfterSet_PassesPendingStateToEncoder(
        OutputId id,
        int value,
        [Frozen] IHidReportEncoder encoder,
        HidOutputWriter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        sut.Set(id, value);

        encoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>())
            .Returns(_ =>
            {
                cts.Cancel();
                return [];
            });

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        encoder.Received(1).Encode(Arg.Is<IReadOnlyDictionary<OutputId, int>>(d => d[id] == value));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenEncodedReportIsOutputType_WritesReportToTransport(
        OutputId id,
        int value,
        HidOutputReport report,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportEncoder encoder,
        HidOutputWriter sut)
    {
        // Arrange
        report = report with { Type = HidReportType.Output };

        using var cts = new CancellationTokenSource();

        sut.Set(id, value);
        encoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>()).Returns([report]);

        transport.WriteAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ => await cts.CancelAsync());

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await transport.Received(1).WriteAsync(report.Data, Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenEncodedReportIsFeatureType_SetsFeatureOnTransport(
        OutputId id,
        int value,
        HidOutputReport report,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportEncoder encoder,
        HidOutputWriter sut)
    {
        // Arrange
        report = report with { Type = HidReportType.Feature };

        using var cts = new CancellationTokenSource();

        sut.Set(id, value);
        encoder.Encode(Arg.Any<IReadOnlyDictionary<OutputId, int>>()).Returns([report]);

        transport.SetFeatureAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ => await cts.CancelAsync());

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await transport.Received(1).SetFeatureAsync(report.Data, Arg.Any<CancellationToken>());
    }
}
