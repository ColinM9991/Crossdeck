using System.Collections.Immutable;
using System.Threading.Channels;
using Crossdeck.Application.Routing.Output;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Output;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Routing.Adapters;
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
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        activeSim.Current.Returns((ISimProducer?)null);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSimulatorIsReadyAndTheAircraftChanges_RaisesAircraftChanged(
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);
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
    public async Task RunAsync_WhenNoValuesHaveChanged_LeavesTheCacheEmpty(
        SimRef reference,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (_, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.False(simValueCache.TryGet(reference, out var entry));
        Assert.Null(entry);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAValueChanges_CachesTheValue(
        SimRef reference,
        SimValue value,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, value)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.True(simValueCache.TryGet(reference, out var entry));
        Assert.Equal(value, entry!.Value);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenMultipleValuesHaveChanged_CachesEachEntry(
        SimRef firstReference,
        SimValue firstValue,
        SimRef secondReference,
        SimValue secondValue,
        TestTimeProvider timeProvider,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        var startedAt = timeProvider.GetTimestamp();
        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged(
        [
            new SimEvent.ValueChanged(firstReference, firstValue),
            new SimEvent.ValueChanged(secondReference, secondValue),
        ]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        var finishedAt = timeProvider.GetTimestamp();

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        var snapshot = simValueCache.GetSnapshot();
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(firstValue, snapshot[firstReference].Value);
        Assert.InRange(snapshot[firstReference].UpdatedAt, startedAt, finishedAt);
        Assert.Equal(secondValue, snapshot[secondReference].Value);
        Assert.InRange(snapshot[secondReference].UpdatedAt, startedAt, finishedAt);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSameReferenceChangesTwice_CachesTheLatestValue(
        SimRef reference,
        SimValue firstValue,
        SimValue secondValue,
        TestTimeProvider timeProvider,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        var firstSet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondSet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var setCount = 0;
        simValueCache.EntryCached += () =>
        {
            switch (Interlocked.Increment(ref setCount))
            {
                case 1:
                    firstSet.TrySetResult();
                    break;
                case 2:
                    secondSet.TrySetResult();
                    break;
            }
        };

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, firstValue)]));
        await firstSet.Task;

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, secondValue)]));
        await secondSet.Task;
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.True(simValueCache.TryGet(reference, out var entry));
        Assert.Equal(secondValue, entry!.Value);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAValueChangesOnABoundReference_SetsTheIndicator(
        string id,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, driver) = NewIndicatorDriver();
        deviceRegistry.Add(device, driver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(device, output))
        ]);
        var (simEvents, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer);
        var (calls, completed) = WatchSets(indicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        await completed;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Contains((output, (int)number.Value), calls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheValueIsAString_DoesNotSetTheIndicator(
        string id,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.String value,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, driver) = NewIndicatorDriver();
        deviceRegistry.Add(device, driver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(device, output))
        ]);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, value)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
        Assert.True(simValueCache.TryGet(reference, out _));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheBindingHasMultipleSources_DoesNotSetTheIndicator(
        string id,
        SimRef firstReference,
        SimRef secondReference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, driver) = NewIndicatorDriver();
        deviceRegistry.Add(device, driver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef>
                {
                    ["deploy"] = firstReference,
                    ["power"] = secondReference,
                }.ToImmutableDictionary(),
                new OutputSink.Indicator(device, output))
        ]);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(firstReference, number)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
        Assert.True(simValueCache.TryGet(firstReference, out _));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheDeviceIsNotConnected_DoesNotSetTheIndicator(
        string id,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, _) = NewIndicatorDriver();
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(device, output))
        ]);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSinkIsNotAnIndicator_DoesNotSetTheIndicator(
        string id,
        SimRef reference,
        DeviceIdentity device,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, driver) = NewIndicatorDriver();
        deviceRegistry.Add(device, driver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Screen(device))
        ]);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTwoBindingsShareAReference_BothIndicatorsAreSet(
        string id,
        SimRef reference,
        DeviceIdentity firstDevice,
        OutputId firstOutput,
        DeviceIdentity secondDevice,
        OutputId secondOutput,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (firstIndicator, firstDriver) = NewIndicatorDriver();
        var (secondIndicator, secondDriver) = NewIndicatorDriver();
        deviceRegistry.Add(firstDevice, firstDriver);
        deviceRegistry.Add(secondDevice, secondDriver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(firstDevice, firstOutput)),
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(secondDevice, secondOutput)),
        ]);
        var (simEvents, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer);
        var (firstCalls, firstCompleted) = WatchSets(firstIndicator);
        var (secondCalls, secondCompleted) = WatchSets(secondIndicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        await Task.WhenAll(firstCompleted, secondCompleted);

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Contains((firstOutput, (int)number.Value), firstCalls);
        Assert.Contains((secondOutput, (int)number.Value), secondCalls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAnIndicatorThrows_SubsequentSimEventsAreNotProcessed(
        string id,
        SimRef reference,
        SimValue.Number number,
        SimRef secondReference,
        SimValue secondValue,
        DeviceIdentity device,
        OutputId output,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, driver) = NewIndicatorDriver();
        deviceRegistry.Add(device, driver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(device, output))
        ]);
        var (simEvents, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer);

        var indicatorFaulted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var call = indicator.When(i => i.Set(Arg.Any<OutputId>(), Arg.Any<int>()));
        call.Do(_ => indicatorFaulted.TrySetResult());
        call.Throw(new InvalidOperationException());

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        await indicatorFaulted.Task;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([
            new SimEvent.ValueChanged(secondReference, secondValue)
        ]));
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.True(simValueCache.TryGet(reference, out _));
        Assert.False(simValueCache.TryGet(secondReference, out _));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenNoBindingsAreSet_DoesNotSetTheIndicator(
        SimRef reference,
        DeviceIdentity device,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, driver) = NewIndicatorDriver();
        deviceRegistry.Add(device, driver);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        indicator.DidNotReceive().Set(Arg.Any<OutputId>(), Arg.Any<int>());
        Assert.True(simValueCache.TryGet(reference, out _));
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenBindingsAreReplaced_DispatchFollowsTheNewBinding(
        string id,
        SimRef oldReference,
        DeviceIdentity oldDevice,
        OutputId oldOutput,
        SimRef newReference,
        DeviceIdentity newDevice,
        OutputId newOutput,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (oldIndicator, oldDriver) = NewIndicatorDriver();
        var (newIndicator, newDriver) = NewIndicatorDriver();
        deviceRegistry.Add(oldDevice, oldDriver);
        deviceRegistry.Add(newDevice, newDriver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = oldReference }.ToImmutableDictionary(),
                new OutputSink.Indicator(oldDevice, oldOutput))
        ]);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);
        var (oldCalls, oldCompleted) = WatchSets(oldIndicator);
        var (newCalls, newCompleted) = WatchSets(newIndicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(oldReference, number)]));
        await oldCompleted;

        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = newReference }.ToImmutableDictionary(),
                new OutputSink.Indicator(newDevice, newOutput))
        ]);
        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(oldReference, number)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(newReference, number)]));
        await newCompleted;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Single(oldCalls);
        Assert.Contains((newOutput, (int)number.Value), newCalls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenBindingsAreClearedWithAnEmptyList_DoesNotSetTheIndicator(
        string id,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        SimValue.Number number,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (indicator, driver) = NewIndicatorDriver();
        deviceRegistry.Add(device, driver);
        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(device, output))
        ]);
        var (simEvents, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);
        var (calls, completed) = WatchSets(indicator);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        await completed;

        sut.SetBindings([]);
        simEvents.Writer.TryWrite(new SimEvent.ValuesChanged([new SimEvent.ValueChanged(reference, number)]));
        simEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Single(calls);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSimulatorIsDisconnected_RecoversWhenANewSimulatorConnects(
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var secondProducer = Substitute.For<ISimProducer>();

        var (_, simulatorChanged, aircraftChanged) = ConnectSimulator(sut, activeSim, producer);
        activeSim.Current.Returns((ISimProducer?)null);

        var secondSimulatorConnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEvents = Channel.CreateUnbounded<SimEvent>();
        secondProducer.GetEventsAsync(Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                secondSimulatorConnected.TrySetResult();
                return secondEvents.Reader.ReadAllAsync(call.ArgAt<CancellationToken>(0));
            });

        var simulatorChangingHandled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator changing"))
            .Do(_ => simulatorChangingHandled.TrySetResult());

        activeSim.Changing += Raise.Event<EventHandler<ActiveSimChangingEventArgs>>(activeSim,
            new ActiveSimChangingEventArgs(producer, null));
        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChangedEventArgs>>(activeSim,
            new ActiveSimChangedEventArgs(secondProducer));

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;
        await simulatorChangingHandled.Task;
        await secondSimulatorConnected.Task;

        secondEvents.Writer.TryWrite(new SimEvent.AircraftChanged("A320"));
        var aircraft = await aircraftChanged;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Equal("A320", aircraft);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenBindingsAreSet_PublishesTheRequiredRefsToTheAggregator(
        string id,
        SimRef reference,
        DeviceIdentity device,
        OutputId output,
        [Frozen] IActiveSim activeSim,
        [Frozen] FakeDeviceRegistry deviceRegistry,
        [Frozen] NotifyingSimValueCache simValueCache,
        [Frozen] ISubscriptionAggregator subscriptionAggregator,
        [Frozen] IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ISimProducer producer,
        [Frozen] ILogger<SimValueRouter> logger,
        SimValueRouter sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var (_, simulatorChanged, _) = ConnectSimulator(sut, activeSim, producer);

        var requiredRefsPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        subscriptionAggregator
            .When(a => a.SetOutputRefs(Arg.Any<IReadOnlySet<SimRef>>()))
            .Do(_ => requiredRefsPublished.TrySetResult());

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await simulatorChanged;

        sut.SetBindings([
            new OutputMapping(
                id,
                new Dictionary<string, SimRef> { ["deploy"] = reference }.ToImmutableDictionary(),
                new OutputSink.Indicator(device, output))
        ]);
        await requiredRefsPublished.Task;

        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        subscriptionAggregator.Received(1).SetOutputRefs(
            Arg.Is<IReadOnlySet<SimRef>>(refs => refs.Count == 1 && refs.Contains(reference)));
    }

    private static (IIndicatorOutput Indicator, IDeviceDriver Driver) NewIndicatorDriver()
    {
        var driver = Substitute.For<IDeviceDriver, IIndicatorOutput>();
        return ((IIndicatorOutput)driver, driver);
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
        ISimProducer producer)
    {
        var events = Channel.CreateUnbounded<SimEvent>();

        var simulatorChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        producer.GetEventsAsync(Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                simulatorChanged.TrySetResult();
                return events.Reader.ReadAllAsync(call.ArgAt<CancellationToken>(0));
            });

        var aircraftChanged = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.AircraftChanged += (_, aircraft) => aircraftChanged.TrySetResult(aircraft);

        activeSim.Current.Returns(producer);
        activeSim.Changed +=
            Raise.Event<EventHandler<ActiveSimChangedEventArgs>>(activeSim, new ActiveSimChangedEventArgs(producer));

        return (events, simulatorChanged.Task, aircraftChanged.Task);
    }
}