using System.Threading.Channels;
using Crossdeck.Application.Routing;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Tests.Routing;

public sealed class SimValueRouterTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(SimValueRouter).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenThereIsNoActiveSimulator_CompletesWhenCancelled(
        [Frozen] IActiveSim activeSim,
        [Frozen] IDeviceRegistry deviceRegistry,
        [Frozen] ILogger<SimValueRouter> logger)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sut = new SimValueRouter(activeSim, deviceRegistry, logger);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSimulatorIsReadyAndTheAircraftChanges_RaisesAircraftChanged(
        [Frozen] IActiveSim activeSim,
        [Frozen] IDeviceRegistry deviceRegistry,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var sut = new SimValueRouter(activeSim, deviceRegistry, logger);

        var simEvents = Channel.CreateUnbounded<SimEvent>();
        producer.GetEventsAsync(Arg.Any<CancellationToken>())
            .Returns(call => simEvents.Reader.ReadAllAsync(call.ArgAt<CancellationToken>(0)));

        var simulatorChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator changed"))
            .Do(_ => simulatorChanged.TrySetResult());

        var aircraftChanged = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.AircraftChanged += (_, aircraft) => aircraftChanged.TrySetResult(aircraft);

        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChangedEventArgs>>(activeSim, new ActiveSimChangedEventArgs(producer));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged.Task;

        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());
        var aircraft = await aircraftChanged.Task;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Equal("A320", aircraft);
    }
}
