using System.Text.Json.Serialization;

namespace Crossdeck.Sim.XPlane;

public class XPlaneCapabilities
{
    [JsonPropertyName("api")]
    public ApiVersions Api { get; set; }
    
    [JsonPropertyName("x-plane")]
    public XPlaneVersion Version { get; set; }

    public class ApiVersions
    {
        public string[] Versions { get; set; }
    }

    public class XPlaneVersion
    {
        public string Version { get; set; }
    }
}