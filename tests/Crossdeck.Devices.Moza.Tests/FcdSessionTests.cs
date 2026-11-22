using Crossdeck.Devices.Serial;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class FcdSessionTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture()
            .Customize(new AutoNSubstituteCustomization())
            .Customize(new TimeProviderCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(FcdSession).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenCancelled_CompletesAndDisposesTheTransport(
        [Frozen] ISerialTransport transport,
        TimeProvider timeProvider,
        FcdSession sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var token = call.ArgAt<CancellationToken>(1);
                return Task.Delay(Timeout.Infinite, token).ContinueWith(_ => 0, TaskScheduler.Default);
            });

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await cts.CancelAsync();
        await runTask;

        // Assert
        transport.Received(1).Open();
        await transport.Received(1).DisposeAsync();
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenReadReturnsZero_ThrowsFcdSessionLostException(
        [Frozen] ISerialTransport transport,
        TimeProvider timeProvider,
        FcdSession sut)
    {
        // Arrange
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var exception = await Assert.ThrowsAsync<FcdSessionLostException>(
            () => sut.RunAsync(CancellationToken.None));

        // Assert
        Assert.IsType<EndOfStreamException>(exception.InnerException);
    }

    [Theory, AutoNSubstituteData]
    public async Task DisposeAsync_WhenCalled_DisposesTheTransport(
        [Frozen] ISerialTransport transport,
        TimeProvider timeProvider,
        FcdSession sut)
    {
        // Act
        await sut.DisposeAsync();

        // Assert
        await transport.Received(1).DisposeAsync();
    }
}
