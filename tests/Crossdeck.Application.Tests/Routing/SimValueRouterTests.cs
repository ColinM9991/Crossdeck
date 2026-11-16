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
        fixture.Register(() => TimeProvider.System);
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
        activeSim.Current.Returns((ISimProducer?)null);

        using var cts = new CancellationTokenSource();

        var sut = new SimValueRouter(activeSim, deviceRegistry, TimeProvider.System, logger);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await cts.CancelAsync();

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

        var sut = new SimValueRouter(activeSim, deviceRegistry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());
        var aircraft = await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Equal("A320", aircraft);
    }

    [Theory, AutoNSubstituteData]
    public void TryGet_WhenNoValuesHaveChanged_ReturnsFalse(
        IActiveSim activeSim,
        IDeviceRegistry deviceRegistry,
        ILogger<SimValueRouter> logger,
        SimRef reference)
    {
        // Arrange
        var sut = new SimValueRouter(activeSim, deviceRegistry, TimeProvider.System, logger);

        // Act
        var found = sut.TryGet(reference, out var entry);

        // Assert
        Assert.False(found);
        Assert.Null(entry);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAValueChanges_CachesTheValue(
        [Frozen] IActiveSim activeSim,
        [Frozen] IDeviceRegistry deviceRegistry,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        SimValue value)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var sut = new SimValueRouter(activeSim, deviceRegistry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, value));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.True(sut.TryGet(reference, out var entry));
        Assert.Equal(value, entry!.Value);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetSnapshot_WhenMultipleValuesHaveChanged_ReturnsAllCachedEntries(
        [Frozen] IActiveSim activeSim,
        [Frozen] IDeviceRegistry deviceRegistry,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef firstReference,
        SimValue firstValue,
        SimRef secondReference,
        SimValue secondValue)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var sut = new SimValueRouter(activeSim, deviceRegistry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        var startedAt = TimeProvider.System.GetTimestamp();
        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(firstReference, firstValue));
        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(secondReference, secondValue));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;
        var finishedAt = TimeProvider.System.GetTimestamp();

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        var snapshot = sut.GetSnapshot();
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(firstValue, snapshot[firstReference].Value);
        Assert.InRange(snapshot[firstReference].UpdatedAt, startedAt, finishedAt);
        Assert.Equal(secondValue, snapshot[secondReference].Value);
        Assert.InRange(snapshot[secondReference].UpdatedAt, startedAt, finishedAt);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSameReferenceChangesTwice_CachesTheLatestValue(
        [Frozen] IActiveSim activeSim,
        [Frozen] IDeviceRegistry deviceRegistry,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        SimValue firstValue,
        SimValue secondValue)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var sut = new SimValueRouter(activeSim, deviceRegistry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, firstValue));
        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, secondValue));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.True(sut.TryGet(reference, out var entry));
        Assert.Equal(secondValue, entry!.Value);
    }

    private static (Channel<SimEvent> Events, Task SimulatorChanged, Task<string?> AircraftChanged) ConnectSimulator(
        SimValueRouter sut,
        IActiveSim activeSim,
        ISimProducer producer,
        ILogger<SimValueRouter> logger)
    {
        var events = Channel.CreateUnbounded<SimEvent>();
        producer.GetEventsAsync(Arg.Any<CancellationToken>())
            .Returns(call => events.Reader.ReadAllAsync(call.ArgAt<CancellationToken>(0)));

        var simulatorChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator changed"))
            .Do(_ => simulatorChanged.TrySetResult());

        var aircraftChanged = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.AircraftChanged += (_, aircraft) => aircraftChanged.TrySetResult(aircraft);

        activeSim.Current.Returns(producer);
        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChangedEventArgs>>(activeSim, new ActiveSimChangedEventArgs(producer));

        return (events, simulatorChanged.Task, aircraftChanged.Task);
    }
}
