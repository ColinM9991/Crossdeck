using System.Net;
using System.Net.Http.Json;
using Crossdeck.Core.Models;
using Crossdeck.Sim.XPlane.Api;

namespace Crossdeck.Sim.XPlane.Tests;

public sealed class XPlaneApiSimDetectorTests
{
    [Theory, AutoNSubstituteData]
    public async Task ProbeAsync_WhenCapabilitiesEndpointReturnsData_ReturnsXPlaneDetection(
        IHttpClientFactory httpClientFactory)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new Capabilities()),
        });
        httpClientFactory.CreateClient("xplane").Returns(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        var sut = new XPlaneApiSimDetector(httpClientFactory);

        // Act
        var result = await sut.ProbeAsync(CancellationToken.None);

        // Assert
        Assert.Equal(Simulator.XPlane, result?.Simulator);
    }

    [Theory, AutoNSubstituteData]
    public async Task ProbeAsync_WhenCapabilitiesEndpointReturnsNull_ReturnsNull(
        IHttpClientFactory httpClientFactory)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<Capabilities?>(null),
        });
        httpClientFactory.CreateClient("xplane").Returns(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        var sut = new XPlaneApiSimDetector(httpClientFactory);

        // Act
        var result = await sut.ProbeAsync(CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task ProbeAsync_WhenCapabilitiesEndpointIsUnreachable_ReturnsNull(
        IHttpClientFactory httpClientFactory)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("unreachable"));
        httpClientFactory.CreateClient("xplane").Returns(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        var sut = new XPlaneApiSimDetector(httpClientFactory);

        // Act
        var result = await sut.ProbeAsync(CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task ProbeAsync_WhenCallerCancels_PropagatesCancellation(
        IHttpClientFactory httpClientFactory)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new Capabilities()),
        });
        httpClientFactory.CreateClient("xplane").Returns(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        var sut = new XPlaneApiSimDetector(httpClientFactory);

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.ProbeAsync(cts.Token));
    }
}
