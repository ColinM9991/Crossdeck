using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Messages;

/// <summary>
/// The <c>params</c> payload of a command set is active message.
/// </summary>
public sealed class CommandSetIsActiveParams
{
    /// <summary>
    /// Gets or sets the command activations the message applies.
    /// </summary>
    [JsonPropertyName("commands")]
    public IEnumerable<CommandSetIsActive> Commands { get; init; } = [];
}
