using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Messages;

/// <summary>
/// A command activation: the target id, the desired active state, and an optional duration.
/// </summary>
/// <remarks>
/// The auto-generated <c>CommandSetIsActive</c> contract has a required <c>duration</c>,
/// which would turn held activations into immediate press-and-release, so this custom type
/// omits <c>duration</c> unless a timed release is intended.
/// </remarks>
public sealed class CommandSetIsActive
{
    /// <summary>
    /// Gets or sets the session-scoped command id.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Gets or sets whether the command should be active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets or sets the seconds after which the command is deactivated. Omitted means hold until deactivated.
    /// </summary>
    [JsonPropertyName("duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Duration { get; init; }
}
