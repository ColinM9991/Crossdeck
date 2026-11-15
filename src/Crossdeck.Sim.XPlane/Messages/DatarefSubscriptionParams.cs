using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Messages;

/// <summary>
/// The <c>params</c> payload of a dataref subscribe or unsubscribe message.
/// </summary>
public sealed class DatarefSubscriptionParams
{
    /// <summary>
    /// Gets or sets the datarefs the message applies to.
    /// </summary>
    [JsonPropertyName("datarefs")]
    public IEnumerable<DatarefSubscription> Datarefs { get; init; } = [];
}
