using Crossdeck.Application.Session;
using Crossdeck.Core.Models;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Tests.Session;

public sealed class SimSessionSupervisorTests
{
    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenDetectorFindsSimulator_CreatesRunsAndDisposesMatchingProducer(
        SimDetection detection,
        ISimDetector detector,
        ISimProducerFactory factory,
        ISimProducer producer,
        ILogger<SimSessionSupervisor> logger)
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

        var sut = new SimSessionSupervisor([detector], [factory], logger);
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
        ISimProducer producer,
        ILogger<SimSessionSupervisor> logger)
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

        var sut = new SimSessionSupervisor([detector], [factory], logger);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await detector.Received(2).ProbeAsync(Arg.Any<CancellationToken>());
        await producer.Received(2).RunAsync(Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenADetectorFaults_TriesTheNextDetector(
        SimDetection detection,
        InvalidOperationException fault,
        ISimDetector faultyDetector,
        ISimProducerFactory factory,
        ISimProducer producer,
        ILogger<SimSessionSupervisor> logger)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var changes = new List<ISimProducer?>();
        var healthyDetector = Substitute.For<ISimDetector>();

        faultyDetector.CanProbe().Returns(true);
        faultyDetector.ProbeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SimDetection?>(fault));
        healthyDetector.CanProbe().Returns(true);
        healthyDetector.ProbeAsync(Arg.Any<CancellationToken>()).Returns(detection);
        factory.Simulator.Returns(detection.Simulator);
        factory.Create().Returns(producer);
        producer.RunAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var sut = new SimSessionSupervisor([faultyDetector, healthyDetector], [factory], logger);
        sut.Changed += (_, e) => changes.Add(e.Current);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await faultyDetector.Received(1).ProbeAsync(Arg.Any<CancellationToken>());
        await healthyDetector.Received(1).ProbeAsync(Arg.Any<CancellationToken>());
        Assert.Equal(producer, changes[0]);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenNoFactoryMatchesTheDetectedSimulator_RetriesDetectionUntilCancelled(
        SimDetection detection,
        ISimDetector detector,
        ISimProducerFactory factory,
        ILogger<SimSessionSupervisor> logger)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var probeCount = 0;

        detection = new SimDetection(Simulator.XPlane);
        detector.CanProbe().Returns(true);
        detector.ProbeAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                probeCount++;
                if (probeCount >= 2) cts.Cancel();
                return Task.FromResult<SimDetection?>(detection);
            });
        factory.Simulator.Returns(Simulator.MSFS);

        var sut = new SimSessionSupervisor([detector], [factory], logger, TimeSpan.FromMilliseconds(10));

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await detector.Received(2).ProbeAsync(Arg.Any<CancellationToken>());
        factory.DidNotReceive().Create();
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAllDetectorsFault_RetriesDetectionUntilCancelled(
        InvalidOperationException fault,
        ISimDetector detector,
        ILogger<SimSessionSupervisor> logger)
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
                return Task.FromException<SimDetection?>(fault);
            });

        var sut = new SimSessionSupervisor([detector], [], logger, TimeSpan.FromMilliseconds(10));

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await detector.Received(2).ProbeAsync(Arg.Any<CancellationToken>());
    }
}
