using Crossdeck.Core.Simulators.Models;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane.Tests;

public sealed class XPlaneApiSimProducerTests
{
    [Theory, AutoNSubstituteData]
    public async Task SendCommandAsync_BeforeAnySessionIsEstablished_ReturnsNoConnection(
        XPlaneOptions options,
        string host,
        int port,
        SimCommand command,
        IXPlaneApiService api)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(api, options);

        // Act
        var result = await sut.SendCommandAsync(command, CancellationToken.None);

        // Assert
        Assert.IsType<CommandResult.NoConnection>(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetEventsAsync_FirstAccess_ReturnsConsumableStream(
        XPlaneOptions options,
        string host,
        int port,
        IXPlaneApiService api)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(api, options);

        // Act
        var stream = sut.GetEventsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(stream);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetEventsAsync_ConcurrentEnumeration_ThrowsInvalidOperationException(
        XPlaneOptions options,
        string host,
        int port,
        IXPlaneApiService api)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(api, options);

        var activeFirstConsumer = sut.GetEventsAsync(CancellationToken.None).GetAsyncEnumerator();
        var firstConsumption = activeFirstConsumer.MoveNextAsync();

        // Act
        var secondEnumeration = sut.GetEventsAsync(CancellationToken.None).GetAsyncEnumerator();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await secondEnumeration.MoveNextAsync());

        await sut.DisposeAsync();
        await firstConsumption;
        await secondEnumeration.DisposeAsync();
        await activeFirstConsumer.DisposeAsync();
    }

    [Theory, AutoNSubstituteData]
    public async Task DisposeAsync_BeforeAnySessionIsEstablished_CompletesEventsChannel(
        XPlaneOptions options,
        string host,
        int port,
        IXPlaneApiService api)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(api, options);
        var events = sut.GetEventsAsync(CancellationToken.None);

        // Act
        await sut.DisposeAsync();

        // Assert
        await using var enumerator = events.GetAsyncEnumerator();
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Theory, AutoNSubstituteData]
    public async Task GetEventsAsync_CancelledToken_ThrowsOperationCanceledException(
        XPlaneOptions options,
        string host,
        int port,
        CancellationTokenSource cts,
        IXPlaneApiService api)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(api, options);

        await cts.CancelAsync();
        var events = sut.GetEventsAsync(cts.Token);

        // Act
        await using var enumerator = events.GetAsyncEnumerator();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await enumerator.MoveNextAsync());
    }
}
