using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Messages;

/// <summary>
/// A dataref to subscribe to or unsubscribe from, identified by its session-scoped id.
/// </summary>
/// <remarks>
/// The auto-generated <c>DatarefSubscription</c> contract has a required <c>index</c>,
/// which would subscribe element zero of every dataref, so this custom type omits
/// <c>index</c> unless an array element is explicitly targeted.
/// </remarks>
public sealed class DatarefSubscription
{
    /// <summary>
    /// Gets or sets the session-scoped dataref id.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Gets or sets a single element index for array datarefs. Omitted when the whole dataref is subscribed.
    /// </summary>
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Index { get; init; }
}
