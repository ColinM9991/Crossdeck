using System.Threading.Channels;
using Crossdeck.Application.Routing;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping;
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

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAValueChangesOnABoundReference_SetsTheIndicator(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver driver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput indicator = (IIndicatorOutput)driver;
        registry.Add(device, driver);

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef> { ["deploy"] = reference },
            new OutputSink.Indicator(device, output))]);
        var (calls, completed) = WatchSets(indicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        await completed;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Contains((output, (int)number.Value), calls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheValueIsAString_DoesNotSetTheIndicator(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.String value)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver driver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput indicator = (IIndicatorOutput)driver;
        registry.Add(device, driver);

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef> { ["deploy"] = reference },
            new OutputSink.Indicator(device, output))]);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, value));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
        Assert.True(sut.TryGet(reference, out _));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheBindingHasMultipleSources_DoesNotSetTheIndicator(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef firstReference,
        SimRef secondReference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver driver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput indicator = (IIndicatorOutput)driver;
        registry.Add(device, driver);

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef>
            {
                ["deploy"] = firstReference,
                ["power"] = secondReference,
            },
            new OutputSink.Indicator(device, output))]);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(firstReference, number));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
        Assert.True(sut.TryGet(firstReference, out _));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheDeviceIsNotConnected_DoesNotSetTheIndicator(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        IIndicatorOutput indicator = (IIndicatorOutput)Substitute.For<IDeviceDriver, IIndicatorOutput>();

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef> { ["deploy"] = reference },
            new OutputSink.Indicator(device, output))]);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSinkIsNotAnIndicator_DoesNotSetTheIndicator(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity device,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver driver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput indicator = (IIndicatorOutput)driver;
        registry.Add(device, driver);

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef> { ["deploy"] = reference },
            new OutputSink.Screen(device))]);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTwoBindingsShareAReference_BothIndicatorsAreSet(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity firstDevice,
        OutputId firstOutput,
        DeviceIdentity secondDevice,
        OutputId secondOutput,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver firstDriver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput firstIndicator = (IIndicatorOutput)firstDriver;
        registry.Add(firstDevice, firstDriver);

        IDeviceDriver secondDriver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput secondIndicator = (IIndicatorOutput)secondDriver;
        registry.Add(secondDevice, secondDriver);

        sut.SetBindings([
            new OutputMapping(
                new Dictionary<string, SimRef> { ["deploy"] = reference },
                new OutputSink.Indicator(firstDevice, firstOutput)),
            new OutputMapping(
                new Dictionary<string, SimRef> { ["deploy"] = reference },
                new OutputSink.Indicator(secondDevice, secondOutput)),
        ]);
        var (firstCalls, firstCompleted) = WatchSets(firstIndicator);
        var (secondCalls, secondCompleted) = WatchSets(secondIndicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        await Task.WhenAll(firstCompleted, secondCompleted);

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Contains((firstOutput, (int)number.Value), firstCalls);
        Assert.Contains((secondOutput, (int)number.Value), secondCalls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenOneIndicatorThrows_TheOtherBindingIsStillDispatched(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity firstDevice,
        OutputId firstOutput,
        DeviceIdentity secondDevice,
        OutputId secondOutput,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver firstDriver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput firstIndicator = (IIndicatorOutput)firstDriver;
        registry.Add(firstDevice, firstDriver);
        firstIndicator.When(i => i.Set(Arg.Any<OutputId>(), Arg.Any<int>())).Throw(new InvalidOperationException());

        IDeviceDriver secondDriver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput secondIndicator = (IIndicatorOutput)secondDriver;
        registry.Add(secondDevice, secondDriver);

        sut.SetBindings([
            new OutputMapping(
                new Dictionary<string, SimRef> { ["deploy"] = reference },
                new OutputSink.Indicator(firstDevice, firstOutput)),
            new OutputMapping(
                new Dictionary<string, SimRef> { ["deploy"] = reference },
                new OutputSink.Indicator(secondDevice, secondOutput)),
        ]);
        var (secondCalls, secondCompleted) = WatchSets(secondIndicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        await secondCompleted;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        firstIndicator.Received(1).Set(Arg.Any<OutputId>(), Arg.Any<int>());
        Assert.Contains((secondOutput, (int)number.Value), secondCalls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenNoBindingsAreSet_DoesNotSetTheIndicator(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity device,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver driver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput indicator = (IIndicatorOutput)driver;
        registry.Add(device, driver);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
        Assert.True(sut.TryGet(reference, out _));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenBindingsAreReplaced_DispatchFollowsTheNewBinding(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef oldReference,
        DeviceIdentity oldDevice,
        OutputId oldOutput,
        SimRef newReference,
        DeviceIdentity newDevice,
        OutputId newOutput,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver oldDriver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput oldIndicator = (IIndicatorOutput)oldDriver;
        registry.Add(oldDevice, oldDriver);

        IDeviceDriver newDriver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput newIndicator = (IIndicatorOutput)newDriver;
        registry.Add(newDevice, newDriver);

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef> { ["deploy"] = oldReference },
            new OutputSink.Indicator(oldDevice, oldOutput))]);
        var (oldCalls, oldCompleted) = WatchSets(oldIndicator);
        var (newCalls, newCompleted) = WatchSets(newIndicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(oldReference, number));
        await oldCompleted;

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef> { ["deploy"] = newReference },
            new OutputSink.Indicator(newDevice, newOutput))]);
        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(oldReference, number));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(newReference, number));
        await newCompleted;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Single(oldCalls);
        Assert.Contains((newOutput, (int)number.Value), newCalls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenBindingsAreClearedWithAnEmptyList_DoesNotSetTheIndicator(
        [Frozen] IActiveSim activeSim,
        [Frozen] ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var registry = new FakeDeviceRegistry();
        var sut = new SimValueRouter(activeSim, registry, TimeProvider.System, logger);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer, logger);

        IDeviceDriver driver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        IIndicatorOutput indicator = (IIndicatorOutput)driver;
        registry.Add(device, driver);

        sut.SetBindings([new OutputMapping(
            new Dictionary<string, SimRef> { ["deploy"] = reference },
            new OutputSink.Indicator(device, output))]);
        var (calls, completed) = WatchSets(indicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        await completed;

        sut.SetBindings([]);
        simEvents.Writer.TryWrite(new SimEvent.ValueChanged(reference, number));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Single(calls);
    }

    private static (List<(OutputId Output, int Value)> Calls, Task Completed) WatchSets(IIndicatorOutput indicator)
    {
        var calls = new List<(OutputId Output, int Value)>();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        indicator.When(i => i.Set(Arg.Any<OutputId>(), Arg.Any<int>())).Do(call =>
        {
            calls.Add((call.ArgAt<OutputId>(0), call.ArgAt<int>(1)));
            completed.TrySetResult();
        });

        return (calls, completed.Task);
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

#pragma warning disable CS0067
    private sealed class FakeDeviceRegistry : IDeviceRegistry
    {
        private readonly Dictionary<DeviceIdentity, IDeviceDriver> _drivers = new();

        public event EventHandler<DriverAddedEventArgs>? DriverAdded;
        public event EventHandler<DriverRemovedEventArgs>? DriverRemoved;

        public void Add(DeviceIdentity identity, IDeviceDriver driver) => _drivers[identity] = driver;

        public bool TryGet(DeviceIdentity identity, out IDeviceDriver? driver) =>
            _drivers.TryGetValue(identity, out driver);

        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
#pragma warning restore CS0067
}
