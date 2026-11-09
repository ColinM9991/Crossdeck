using Crossdeck.Core.Simulators.Models;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane.Tests;

public sealed class XPlaneApiSimProducerTests
{
    [Theory, AutoNSubstituteData]
    public async Task SendCommandAsync_BeforeAnySessionIsEstablished_ReturnsNoConnection(
        IHttpClientFactory httpClientFactory,
        XPlaneOptions options,
        string host,
        int port,
        SimCommand command)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(httpClientFactory, options);

        // Act
        var result = await sut.SendCommandAsync(command, CancellationToken.None);

        // Assert
        Assert.IsType<CommandResult.NoConnection>(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task Events_FirstAccess_ReturnsConsumableStream(
        IHttpClientFactory httpClientFactory,
        XPlaneOptions options,
        string host,
        int port)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(httpClientFactory, options);

        // Act
        var stream = sut.Events;

        // Assert
        Assert.NotNull(stream);
    }

    [Theory, AutoNSubstituteData]
    public void Events_SecondAccess_ThrowsInvalidOperationException(
        IHttpClientFactory httpClientFactory,
        XPlaneOptions options,
        string host,
        int port)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(httpClientFactory, options);
        _ = sut.Events;

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => sut.Events);
    }

    [Theory, AutoNSubstituteData]
    public async Task DisposeAsync_BeforeAnySessionIsEstablished_CompletesEventsChannel(
        IHttpClientFactory httpClientFactory,
        XPlaneOptions options,
        string host,
        int port)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneApiSimProducer(httpClientFactory, options);
        var events = sut.Events;

        // Act
        await sut.DisposeAsync();

        // Assert
        await using var enumerator = events.GetAsyncEnumerator();
        Assert.False(await enumerator.MoveNextAsync());
    }
}
