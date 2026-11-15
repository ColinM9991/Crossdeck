using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Messages;

/// <summary>
/// A dataref value write: the target id, the value, and optionally a single array element index.
/// </summary>
/// <remarks>
/// The auto-generated <c>DatarefSetValue</c> contract models <c>value</c> as a double,
/// which cannot carry base64 strings or number arrays, so this custom type uses
/// <see cref="object"/> and lets System.Text.Json serialise the runtime type.
/// </remarks>
public sealed class DatarefSetValue
{
    /// <summary>
    /// Gets or sets the session-scoped dataref id.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Gets or sets the value to write: a number for scalar datarefs, an array of numbers for array datarefs,
    /// or a base64-encoded string for data datarefs.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; init; }

    /// <summary>
    /// Gets or sets a single element index to write, for array datarefs. Omitted when the whole dataref is written.
    /// </summary>
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Index { get; init; }
}
