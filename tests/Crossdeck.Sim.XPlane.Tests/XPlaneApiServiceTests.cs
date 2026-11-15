using System.Net;
using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Idioms;
using Crossdeck.Sim.XPlane.Api;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane.Tests;

public sealed class XPlaneApiServiceTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(XPlaneApiService).GetConstructors());
    }

    [Fact]
    public async Task GetCapabilitiesAsync_Always_QueriesCapabilitiesEndpoint()
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new Capabilities()),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetCapabilitiesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new Uri("http://localhost/api/capabilities"), requestedUri);
        Assert.NotNull(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetDatarefCountAsync_Always_ReturnsCountFromDatarefCountEndpoint(int count)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CountResponse { Data = count }),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetDatarefCountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new Uri("http://localhost/datarefs/count"), requestedUri);
        Assert.Equal(count, result?.Data);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetCommandCountAsync_Always_ReturnsCountFromCommandCountEndpoint(int count)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CountResponse { Data = count }),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetCommandCountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new Uri("http://localhost/commands/count"), requestedUri);
        Assert.Equal(count, result?.Data);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetDatarefsAsync_WhenNoParametersAreGiven_QueriesBareDatarefsEndpoint(long datarefId)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DatarefListResponse { Data = [new Dataref { Id = datarefId }] }),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetDatarefsAsync(null, null, null, null, CancellationToken.None);

        // Assert
        Assert.Equal(new Uri("http://localhost/datarefs"), requestedUri);
        Assert.Equal(datarefId, result?.Data.FirstOrDefault()?.Id);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetDatarefsAsync_WhenFilterAndPaginationAreGiven_QueriesAllParameters(
        string filterName,
        int start,
        int limit,
        string fields)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DatarefListResponse()),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetDatarefsAsync(filterName, start, limit, fields, CancellationToken.None);

        // Assert
        var expectedQuery =
            $"?filter[name]={Uri.EscapeDataString(filterName)}&start={start}&limit={limit}&fields={Uri.EscapeDataString(fields)}";
        Assert.Equal(expectedQuery, requestedUri?.Query);
        Assert.NotNull(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetCommandsAsync_WhenFilterNameIsGiven_QueriesFilterParameter(string filterName)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CommandListResponse()),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetCommandsAsync(filterName, null, null, null, CancellationToken.None);

        // Assert
        Assert.Equal($"?filter[name]={Uri.EscapeDataString(filterName)}", requestedUri?.Query);
        Assert.NotNull(result);
    }

    [Theory, AutoNSubstituteData]
    public async Task GetDatarefValueAsync_WhenNoIndexIsGiven_ReturnsScalarValue(long datarefId, double value)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { data = value }),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetDatarefValueAsync(datarefId, null, CancellationToken.None);

        // Assert
        Assert.Equal(new Uri($"http://localhost/datarefs/{datarefId}/value"), requestedUri);
        Assert.Equal(value, result.GetDouble());
    }

    [Theory, AutoNSubstituteData]
    public async Task GetDatarefValueAsync_WhenIndexIsGiven_QueriesIndexedValueEndpoint(
        long datarefId,
        int index,
        double[] values)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { data = values }),
            };
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        var result = await sut.GetDatarefValueAsync(datarefId, index, CancellationToken.None);

        // Assert
        Assert.Equal(new Uri($"http://localhost/datarefs/{datarefId}/value?index={index}"), requestedUri);
        Assert.Equal(values.Length, result.GetArrayLength());
        Assert.Equal(values, result.EnumerateArray().Select(x => x.GetDouble()).ToArray());
    }

    [Theory, AutoNSubstituteData]
    public async Task SetDatarefValueAsync_WhenIndexIsGiven_PatchesIndexedValuePayload(
        long datarefId,
        int index,
        string value)
    {
        // Arrange
        HttpRequestMessage? request = null;
        var handler = new FakeHttpMessageHandler(message =>
        {
            request = message;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        var payload = new DatarefValuePayload { Data = value };

        // Act
        await sut.SetDatarefValueAsync(datarefId, payload, index, CancellationToken.None);

        // Assert
        Assert.Equal(HttpMethod.Patch, request!.Method);
        Assert.Equal(new Uri($"http://localhost/datarefs/{datarefId}/value?index={index}"), request.RequestUri);
        var body = await request.Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal(value, document.RootElement.GetProperty("data").GetString());
    }

    [Theory, AutoNSubstituteData]
    public async Task ActivateCommandAsync_Always_PostsDurationToActivateEndpoint(long commandId, double duration)
    {
        // Arrange
        HttpRequestMessage? request = null;
        var handler = new FakeHttpMessageHandler(message =>
        {
            request = message;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act
        await sut.ActivateCommandAsync(commandId, duration, CancellationToken.None);

        // Assert
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Equal(new Uri($"http://localhost/command/{commandId}/activate"), request.RequestUri);
        var body = await request.Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal(duration, document.RootElement.GetProperty("duration").GetDouble());
    }

    [Theory, AutoNSubstituteData]
    public async Task GetDatarefCountAsync_WhenCallerCancels_PropagatesCancellation(CancellationTokenSource cts)
    {
        // Arrange
        await cts.CancelAsync();
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new CountResponse()),
        });
        var sut = new XPlaneApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.GetDatarefCountAsync(cts.Token));
    }
}
