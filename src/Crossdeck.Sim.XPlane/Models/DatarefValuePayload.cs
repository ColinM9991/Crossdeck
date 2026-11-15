using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Models;

/// <summary>
/// Request body for a dataref value write.
/// The <c>data</c> payload is a number for scalar datarefs, an array of numbers for array datarefs,
/// or a base64-encoded string for data datarefs.
/// </summary>
/// <remarks>
/// The auto-generated <c>DatarefValueRequest</c> contract models <c>data</c> as a double,
/// which cannot carry string or array values, so this custom type is used instead.
/// </remarks>
public sealed class DatarefValuePayload
{
    /// <summary>
    /// Gets or sets the value to write, serialised at its runtime type.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; init; }
}
