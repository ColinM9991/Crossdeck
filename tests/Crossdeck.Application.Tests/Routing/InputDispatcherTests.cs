using Crossdeck.Application.Routing;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Tests.Routing;

public sealed class InputDispatcherTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(InputDispatcher).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public async Task Post_Always_MakesEventAvailableViaRecent(
        DeviceInputEvent inputEvent,
        InputDispatcher sut)
    {
        // Act
        sut.Post(inputEvent);

        // Assert
        await using var enumerator = sut.Recent.GetAsyncEnumerator();
        var moved = await enumerator.MoveNextAsync();

        Assert.True(moved);
        Assert.Equal(inputEvent, enumerator.Current);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenPressedEventArrivesWhileSimIsReady_SendsBeginCommand(
        DeviceInputEvent inputEvent,
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        [Frozen] ILogger<InputDispatcher> logger,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.Pressed };

        producer.SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new CommandResult.Success();
            });
        sut.SetBindings([new InputMapping(
            inputEvent.Identity,
            inputEvent.Control.Id,
            new Dictionary<string, IReadOnlyList<InputAction>>
            {
                ["Pressed"] = [new InputAction.Command(new SimRef("begin.test"), CommandPhase.Begin)]
            })]);
        var (simulatorChanged, simulatorReady) = WireSimulator(activeSim, producer, logger);

        // Act
        var task = sut.RunAsync(cts.Token);
        await simulatorChanged;
        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());
        await simulatorReady;
        sut.Post(inputEvent);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await producer.Received(1).SendCommandAsync(Arg.Is<SimCommand>(c => c is SimCommand.Begin), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenReleasedEventArrivesWhileSimIsReady_SendsEndCommand(
        DeviceInputEvent inputEvent,
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        [Frozen] ILogger<InputDispatcher> logger,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.Released };

        producer.SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new CommandResult.Success();
            });
        sut.SetBindings([new InputMapping(
            inputEvent.Identity,
            inputEvent.Control.Id,
            new Dictionary<string, IReadOnlyList<InputAction>>
            {
                ["Released"] = [new InputAction.Command(new SimRef("end.test"), CommandPhase.End)]
            })]);
        var (simulatorChanged, simulatorReady) = WireSimulator(activeSim, producer, logger);

        // Act
        var task = sut.RunAsync(cts.Token);
        await simulatorChanged;
        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());
        await simulatorReady;
        sut.Post(inputEvent);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await producer.Received(1).SendCommandAsync(Arg.Is<SimCommand>(c => c is SimCommand.End), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenEventKindIsNeitherPressedNorReleased_SendsNoCommand(
        DeviceInputEvent inputEvent,
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        [Frozen] ILogger<InputDispatcher> logger,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.AxisChanged };

        sut.SetBindings([new InputMapping(
            inputEvent.Identity,
            inputEvent.Control.Id,
            new Dictionary<string, IReadOnlyList<InputAction>>
            {
                ["Pressed"] = [new InputAction.Command(new SimRef("begin.test"), CommandPhase.Begin)]
            })]);
        var (simulatorChanged, simulatorReady) = WireSimulator(activeSim, producer, logger);

        // Act
        var task = sut.RunAsync(cts.Token);
        await simulatorChanged;
        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());
        await simulatorReady;
        sut.Post(inputEvent);
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await producer.DidNotReceive().SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenSimIsNotReady_SendsNoCommand(
        DeviceInputEvent inputEvent,
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.Pressed };
        activeSim.Current.Returns(producer);
        await cts.CancelAsync();
        sut.Post(inputEvent);

        // Act
        var task = sut.RunAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await producer.DidNotReceive().SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSimulatorIsDisconnected_RecoversWhenANewSimulatorConnects(
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        [Frozen] ILogger<InputDispatcher> logger,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var secondProducer = Substitute.For<ISimProducer>();
        var (simulatorChanged, _) = WireSimulator(activeSim, producer, logger);

        var simulatorChangingHandled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator changing"))
            .Do(_ => simulatorChangingHandled.TrySetResult());

        var simulatorChangedCount = 0;
        var secondSimulatorChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator changed"))
            .Do(_ =>
            {
                simulatorChangedCount++;
                if (simulatorChangedCount == 2)
                {
                    secondSimulatorChanged.TrySetResult();
                }
            });

        activeSim.Changing += Raise.Event<EventHandler<ActiveSimChangingEventArgs>>(activeSim,
            new ActiveSimChangingEventArgs(producer, null));
        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChangedEventArgs>>(activeSim,
            new ActiveSimChangedEventArgs(secondProducer));

        // Act
        var task = sut.RunAsync(cts.Token);
        await simulatorChanged;
        await simulatorChangingHandled.Task;
        await secondSimulatorChanged.Task;
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenTheSimulatorDisconnects_DiscardsInputEvents(
        DeviceInputEvent inputEvent,
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        [Frozen] ILogger<InputDispatcher> logger,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.Pressed };

        sut.SetBindings([new InputMapping(
            inputEvent.Identity,
            inputEvent.Control.Id,
            new Dictionary<string, IReadOnlyList<InputAction>>
            {
                ["Pressed"] = [new InputAction.Command(new SimRef("disconnect.test"), CommandPhase.Begin)]
            })]);
        var (simulatorChanged, _) = WireSimulator(activeSim, producer, logger);

        var simulatorDisconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator disconnected"))
            .Do(_ => simulatorDisconnected.TrySetResult());

        // Act
        var task = sut.RunAsync(cts.Token);
        await simulatorChanged;

        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());

        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChangedEventArgs>>(activeSim,
            new ActiveSimChangedEventArgs(null));
        await simulatorDisconnected.Task;
        sut.Post(inputEvent);
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await producer.DidNotReceive().SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>());
    }

    private static (Task SimulatorChanged, Task SimulatorReady) WireSimulator(
        IActiveSim activeSim,
        ISimProducer producer,
        ILogger<InputDispatcher> logger)
    {
        var simulatorChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator changed"))
            .Do(_ => simulatorChanged.TrySetResult());

        var simulatorReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.When(l => l.LogInformation("Simulator ready"))
            .Do(_ => simulatorReady.TrySetResult());

        activeSim.Current.Returns(producer);
        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChangedEventArgs>>(activeSim, new ActiveSimChangedEventArgs(producer));

        return (simulatorChanged.Task, simulatorReady.Task);
    }
}
