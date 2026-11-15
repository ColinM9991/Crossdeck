using System.Text.Json;
using Crossdeck.Sim.XPlane.Api;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane;

/// <summary>
/// Provides access to the X-Plane Web API REST surface.
/// </summary>
public interface IXPlaneApiService
{
    /// <summary>
    /// Gets the supported API versions and the X-Plane version.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The capabilities, or null when the response body is empty.</returns>
    Task<Capabilities?> GetCapabilitiesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of registered datarefs.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The dataref count, or null when the response body is empty.</returns>
    Task<CountResponse?> GetDatarefCountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of registered commands.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The command count, or null when the response body is empty.</returns>
    Task<CountResponse?> GetCommandCountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lists datarefs, optionally filtered by name.
    /// </summary>
    /// <param name="filterName">Exact-match name filter, or null for all datarefs.</param>
    /// <param name="start">Index to start from, for pagination.</param>
    /// <param name="limit">Maximum number of results, for pagination.</param>
    /// <param name="fields">Comma-separated fields to return per record, or null for all.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching datarefs, or null when the response body is empty.</returns>
    Task<DatarefListResponse?> GetDatarefsAsync(
        string? filterName,
        int? start,
        int? limit,
        string? fields,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists commands, optionally filtered by name.
    /// </summary>
    /// <param name="filterName">Exact-match name filter, or null for all commands.</param>
    /// <param name="start">Index to start from, for pagination.</param>
    /// <param name="limit">Maximum number of results, for pagination.</param>
    /// <param name="fields">Comma-separated fields to return per record, or null for all.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching commands, or null when the response body is empty.</returns>
    Task<CommandListResponse?> GetCommandsAsync(
        string? filterName,
        int? start,
        int? limit,
        string? fields,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current value of a dataref, or of one element of an array dataref.
    /// </summary>
    /// <param name="id">The session-scoped dataref id.</param>
    /// <param name="index">The element index, for array datarefs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The value: a number, an array of numbers, or a base64-encoded string.</returns>
    Task<JsonElement> GetDatarefValueAsync(long id, int? index, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the value of a dataref, or of one element of an array dataref.
    /// </summary>
    /// <param name="id">The session-scoped dataref id.</param>
    /// <param name="payload">The value to write.</param>
    /// <param name="index">The element index to write, for array datarefs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task SetDatarefValueAsync(long id, DatarefValuePayload payload, int? index, CancellationToken cancellationToken);

    /// <summary>
    /// Activates a command for a fixed duration, releasing it afterwards.
    /// </summary>
    /// <param name="id">The session-scoped command id.</param>
    /// <param name="duration">Seconds to hold the command active; zero presses and releases immediately.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous activation operation.</returns>
    Task ActivateCommandAsync(long id, double duration, CancellationToken cancellationToken);
}
