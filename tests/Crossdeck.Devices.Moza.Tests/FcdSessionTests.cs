using Crossdeck.Devices.Serial;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class FcdSessionTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        fixture.Register(() => TimeProvider.System);
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(FcdSession).GetConstructors());
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_CompletesAndDisposesTheTransport()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var transport = Substitute.For<ISerialTransport>();
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var token = call.ArgAt<CancellationToken>(1);
                return Task.Delay(Timeout.Infinite, token).ContinueWith(_ => 0, TaskScheduler.Default);
            });

        var sut = CreateSut(transport);

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await cts.CancelAsync();
        await runTask;

        // Assert
        transport.Received(1).Open();
        await transport.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task RunAsync_WhenReadReturnsZero_ThrowsFcdSessionLostException()
    {
        // Arrange
        var transport = Substitute.For<ISerialTransport>();
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = CreateSut(transport);

        // Act
        var exception = await Assert.ThrowsAsync<FcdSessionLostException>(
            () => sut.RunAsync(CancellationToken.None));

        // Assert
        Assert.IsType<EndOfStreamException>(exception.InnerException);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalled_DisposesTheTransport()
    {
        // Arrange
        var transport = Substitute.For<ISerialTransport>();
        var sut = CreateSut(transport);

        // Act
        await sut.DisposeAsync();

        // Assert
        await transport.Received(1).DisposeAsync();
    }

    private static FcdSession CreateSut(ISerialTransport transport) =>
        new(transport, TimeProvider.System, Substitute.For<ILogger<FcdSession>>());
}
