using System.Text.Json;
using Crossdeck.Sim.XPlane.Api;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane;

/// <summary>
/// Implements <see cref="IXPlaneApiService"/> using the X-Plane Web API.
/// </summary>
/// <remarks>
/// Register as a typed client of the <c>xplane</c> named client, for example
/// <c>services.AddHttpClient&lt;IXPlaneApiService, XPlaneApiService&gt;("xplane")</c>,
/// so the client inherits the named client's base address.
/// </remarks>
public sealed class XPlaneApiService(HttpClient httpClient) : IXPlaneApiService
{
    private readonly HttpClient _httpClient = httpClient ??
                                                   throw new ArgumentNullException(nameof(httpClient));

    /// <summary>
    /// Gets the supported API versions and the X-Plane version.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The capabilities, or null when the response body is empty.</returns>
    public Task<Capabilities?> GetCapabilitiesAsync(CancellationToken cancellationToken)
        => _httpClient.GetFromJsonAsync<Capabilities?>(XPlaneConstants.CapabilitiesPath, cancellationToken);

    /// <summary>
    /// Gets the number of registered datarefs.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The dataref count.</returns>
    public Task<CountResponse?> GetDatarefCountAsync(CancellationToken cancellationToken)
        => _httpClient.GetFromJsonAsync<CountResponse?>(XPlaneConstants.DatarefCountPath, cancellationToken);

    /// <summary>
    /// Gets the number of registered commands.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The command count.</returns>
    public Task<CountResponse?> GetCommandCountAsync(CancellationToken cancellationToken)
        => _httpClient.GetFromJsonAsync<CountResponse?>(XPlaneConstants.CommandCountPath, cancellationToken);

    /// <summary>
    /// Lists datarefs, optionally filtered by name.
    /// </summary>
    /// <param name="filterName">Exact-match name filter, or null for all datarefs.</param>
    /// <param name="start">Index to start from, for pagination.</param>
    /// <param name="limit">Maximum number of results, for pagination.</param>
    /// <param name="fields">Comma-separated fields to return per record, or null for all.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching datarefs.</returns>
    public Task<DatarefListResponse?> GetDatarefsAsync(
        string? filterName,
        int? start,
        int? limit,
        string? fields,
        CancellationToken cancellationToken)
        => _httpClient.GetFromJsonAsync<DatarefListResponse?>(
            BuildListingUrl(XPlaneConstants.DatarefsPath, filterName, start, limit, fields), cancellationToken);

    /// <summary>
    /// Lists commands, optionally filtered by name.
    /// </summary>
    /// <param name="filterName">Exact-match name filter, or null for all commands.</param>
    /// <param name="start">Index to start from, for pagination.</param>
    /// <param name="limit">Maximum number of results, for pagination.</param>
    /// <param name="fields">Comma-separated fields to return per record, or null for all.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching commands.</returns>
    public Task<CommandListResponse?> GetCommandsAsync(
        string? filterName,
        int? start,
        int? limit,
        string? fields,
        CancellationToken cancellationToken)
        => _httpClient.GetFromJsonAsync<CommandListResponse?>(
            BuildListingUrl(XPlaneConstants.CommandsPath, filterName, start, limit, fields), cancellationToken);

    /// <summary>
    /// Gets the current value of a dataref, or of one element of an array dataref.
    /// </summary>
    /// <param name="id">The session-scoped dataref id.</param>
    /// <param name="index">The element index, for array datarefs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The value: a number, an array of numbers, or a base64-encoded string.</returns>
    public async Task<JsonElement> GetDatarefValueAsync(long id, int? index, CancellationToken cancellationToken)
    {
        var body = await _httpClient.GetFromJsonAsync<JsonElement>(
            BuildDatarefValueUrl(id, index), cancellationToken);

        return body.GetProperty("data");
    }

    /// <summary>
    /// Sets the value of a dataref, or of one element of an array dataref.
    /// </summary>
    /// <param name="id">The session-scoped dataref id.</param>
    /// <param name="payload">The value to write.</param>
    /// <param name="index">The element index to write, for array datarefs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public async Task SetDatarefValueAsync(long id, DatarefValuePayload payload, int? index, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            BuildDatarefValueUrl(id, index), payload, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Activates a command for a fixed duration, releasing it afterwards.
    /// </summary>
    /// <param name="id">The session-scoped command id.</param>
    /// <param name="duration">Seconds to hold the command active; zero presses and releases immediately.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous activation operation.</returns>
    public async Task ActivateCommandAsync(long id, double duration, CancellationToken cancellationToken)
    {
        var url = $"{XPlaneConstants.CommandPath}/{id}/{XPlaneConstants.CommandActivatePath}";
        var response = await _httpClient.PostAsJsonAsync(
            url, new ActivateCommandRequest { Duration = duration }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private static string BuildListingUrl(
        string path,
        string? filterName,
        int? start,
        int? limit,
        string? fields)
    {
        var query = new List<string>();

        if (filterName is not null)
        {
            query.Add($"filter[name]={Uri.EscapeDataString(filterName)}");
        }

        if (start is { } startValue)
        {
            query.Add($"start={startValue}");
        }

        if (limit is { } limitValue)
        {
            query.Add($"limit={limitValue}");
        }

        if (fields is not null)
        {
            query.Add($"fields={Uri.EscapeDataString(fields)}");
        }

        return query.Count > 0
            ? $"{path}?{string.Join("&", query)}"
            : path;
    }

    private static string BuildDatarefValueUrl(long id, int? index)
    {
        var url = $"{XPlaneConstants.DatarefsPath}/{id}/{XPlaneConstants.DatarefValuePath}";
        return index is { } indexValue ? $"{url}?index={indexValue}" : url;
    }
}
