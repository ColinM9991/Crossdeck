using Crossdeck.Application.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators.Models;
using Crossdeck.Core.Simulators;

namespace Crossdeck.Application.Tests.Session;

public sealed class SimSessionSupervisorTests
{
    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenDetectorFindsSimulator_CreatesRunsAndDisposesMatchingProducer(
        SimDetection detection,
        ISimDetector detector,
        ISimProducerFactory factory,
        ISimProducer producer)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var changes = new List<ISimProducer?>();

        detector.CanProbe().Returns(true);
        detector.ProbeAsync(Arg.Any<CancellationToken>()).Returns(detection);
        factory.Simulator.Returns(detection.Simulator);
        factory.Create().Returns(producer);
        producer.RunAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var sut = new SimSessionSupervisor([detector], [factory]);
        sut.Changed += (_, e) => changes.Add(e.Current);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        factory.Received(1).Create();
        Assert.Equal(producer, changes[0]);
        Assert.Null(sut.Current);
        await producer.Received(1).DisposeAsync();
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenProducerStopsVoluntarily_RetriesDetectionUntilCancelled(
        SimDetection detection,
        ISimDetector detector,
        ISimProducerFactory factory,
        ISimProducer producer)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var probeCount = 0;

        detector.CanProbe().Returns(true);
        detector.ProbeAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                probeCount++;
                if (probeCount >= 2) cts.Cancel();
                return Task.FromResult<SimDetection?>(detection);
            });
        factory.Simulator.Returns(detection.Simulator);
        factory.Create().Returns(producer);
        producer.RunAsync(Arg.Any<CancellationToken>()).Returns(_ => throw new OperationCanceledException());

        var sut = new SimSessionSupervisor([detector], [factory]);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await detector.Received(2).ProbeAsync(Arg.Any<CancellationToken>());
        await producer.Received(2).RunAsync(Arg.Any<CancellationToken>());
    }
}
