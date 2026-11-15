using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane.Messages;

/// <summary>
/// An envelope for a message sent to the X-Plane WebSocket API.
/// </summary>
/// <remarks>
/// The auto-generated <c>WebSocketClientMessage</c> contract only models the subscribe message's
/// <c>params</c> shape, so this custom envelope carries the payload at its runtime type instead.
/// </remarks>
public sealed class ClientMessage
{
    /// <summary>
    /// Gets or sets the request id, echoed by the matching <c>result</c> message.
    /// </summary>
    [JsonPropertyName("req_id")]
    public int ReqId { get; init; }

    /// <summary>
    /// Gets or sets the message type, one of the <c>XPlaneConstants</c> message type names.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the message parameters, serialised at their runtime type.
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; init; }
}
