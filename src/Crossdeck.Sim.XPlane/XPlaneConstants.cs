namespace Crossdeck.Sim.XPlane;

/// <summary>
/// Constants for the X-Plane integration: the named HTTP client, the Web API REST paths,
/// the WebSocket message type names, and the dataref key Crossdeck itself subscribes to.
/// </summary>
public static class XPlaneConstants
{
    /// <summary>
    /// The name of the <see cref="System.Net.Http.HttpClient"/> configuration for the X-Plane API.
    /// </summary>
    public const string HttpClientName = "xplane";

    /// <summary>
    /// The unversioned REST path of the capabilities endpoint (available since API v2).
    /// </summary>
    public const string CapabilitiesPath = "/api/capabilities";

    /// <summary>
    /// The REST path of the datarefs listing endpoint, relative to the v3 base.
    /// </summary>
    public const string DatarefsPath = "datarefs";

    /// <summary>
    /// The REST path of the dataref count endpoint.
    /// </summary>
    public const string DatarefCountPath = DatarefsPath + "/count";

    /// <summary>
    /// The REST path segment used for individual dataref value reads and writes.
    /// </summary>
    public const string DatarefValuePath = "value";

    /// <summary>
    /// The REST path of the commands listing endpoint, relative to the v3 base.
    /// </summary>
    public const string CommandsPath = "commands";

    /// <summary>
    /// The REST path of the command count endpoint.
    /// </summary>
    public const string CommandCountPath = CommandsPath + "/count";

    /// <summary>
    /// The singular REST path segment under which command activation endpoints are served.
    /// </summary>
    public const string CommandPath = "command";

    /// <summary>
    /// The REST path segment used to activate a command for a fixed duration.
    /// </summary>
    public const string CommandActivatePath = "activate";

    /// <summary>
    /// The path of the X-Plane WebSocket endpoint, relative to the host and port.
    /// </summary>
    public const string WebSocketPath = "/api/v3";

    /// <summary>
    /// The dataref holding the currently loaded aircraft's .acf path relative to the X-Plane root.
    /// </summary>
    public const string AircraftRefKey = "sim/aircraft/view/acf_relative_path";

    public const string AcfNameRefKey = "sim/aircraft/view/acf_ui_name";

    public const string AcfAuthorRefKey = "sim/aircraft/view/acf_author";

    /// <summary>
    /// WebSocket client message type: add datarefs to the subscription list.
    /// </summary>
    public const string DatarefSubscribeValues = "dataref_subscribe_values";

    /// <summary>
    /// WebSocket client message type: remove datarefs from the subscription list.
    /// </summary>
    public const string DatarefUnsubscribeValues = "dataref_unsubscribe_values";

    /// <summary>
    /// WebSocket client message type: set one or more dataref values.
    /// </summary>
    public const string DatarefSetValues = "dataref_set_values";

    /// <summary>
    /// WebSocket client message type: add commands to the activation subscription list.
    /// </summary>
    public const string CommandSubscribeIsActive = "command_subscribe_is_active";

    /// <summary>
    /// WebSocket client message type: remove commands from the activation subscription list.
    /// </summary>
    public const string CommandUnsubscribeIsActive = "command_unsubscribe_is_active";

    /// <summary>
    /// WebSocket client message type: activate or deactivate one or more commands.
    /// </summary>
    public const string CommandSetIsActive = "command_set_is_active";

    /// <summary>
    /// WebSocket server message type: the result of a client message, matched by request id.
    /// </summary>
    public const string ResultMessage = "result";

    /// <summary>
    /// WebSocket server message type: a periodic push of changed dataref values to subscribers.
    /// </summary>
    public const string DatarefUpdateValues = "dataref_update_values";

    /// <summary>
    /// WebSocket server message type: a push of changed command activation states to subscribers.
    /// </summary>
    public const string CommandUpdateIsActive = "command_update_is_active";
}
