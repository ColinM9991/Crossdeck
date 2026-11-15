using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Messages;

/// <summary>
/// The <c>params</c> payload of a dataref set values message.
/// </summary>
public sealed class DatarefSetValueParams
{
    /// <summary>
    /// Gets or sets the value writes the message applies.
    /// </summary>
    [JsonPropertyName("datarefs")]
    public IEnumerable<DatarefSetValue> Datarefs { get; init; } = [];
}
