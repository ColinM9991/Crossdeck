using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Idioms;
using AutoFixture.Xunit2;
using Crossdeck.Application.Routing;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Crossdeck.Testing;
using NSubstitute;
using Xunit;

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
    public async Task Post_Always_MakesEventAvailableViaRecent(DeviceInputEvent inputEvent, InputDispatcher sut)
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
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.Pressed };

        activeSim.Current.Returns(producer);
        producer.SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new CommandResult.Success();
            });

        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChanged>>(activeSim, new ActiveSimChanged(producer));
        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());

        sut.Post(inputEvent);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await producer.Received(1).SendCommandAsync(Arg.Is<SimCommand>(c => c is SimCommand.Begin), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenReleasedEventArrivesWhileSimIsReady_SendsEndCommand(
        DeviceInputEvent inputEvent,
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.Released };

        activeSim.Current.Returns(producer);
        producer.SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new CommandResult.Success();
            });

        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChanged>>(activeSim, new ActiveSimChanged(producer));
        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());

        sut.Post(inputEvent);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await producer.Received(1).SendCommandAsync(Arg.Is<SimCommand>(c => c is SimCommand.End), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenEventKindIsNeitherPressedNorReleased_SendsNoCommand(
        DeviceInputEvent inputEvent,
        [Frozen] IActiveSim activeSim,
        ISimProducer producer,
        InputDispatcher sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        inputEvent = inputEvent with { Kind = InputEventKind.AxisChanged };

        activeSim.Current.Returns(_ =>
        {
            cts.Cancel();
            return producer;
        });

        activeSim.Changed += Raise.Event<EventHandler<ActiveSimChanged>>(activeSim, new ActiveSimChanged(producer));
        producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());

        sut.Post(inputEvent);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
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
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        // Assert
        await producer.DidNotReceive().SendCommandAsync(Arg.Any<SimCommand>(), Arg.Any<CancellationToken>());
    }
}
