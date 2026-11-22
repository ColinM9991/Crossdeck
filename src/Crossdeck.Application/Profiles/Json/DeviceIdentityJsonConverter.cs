using System.Text.Json;
using System.Text.Json.Serialization;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Application.Profiles.Json;

public sealed class DeviceIdentityJsonConverter : JsonConverter<DeviceIdentity>
{
    public override DeviceIdentity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString(), reader.TokenType);

    public override DeviceIdentity ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString(), JsonTokenType.String);

    public override void Write(Utf8JsonWriter writer, DeviceIdentity value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DeviceIdentity value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value.ToString());

    private static DeviceIdentity Parse(string? text, JsonTokenType tokenType)
    {
        if (tokenType != JsonTokenType.String)
            throw new JsonException($"A DeviceIdentity must be a string, not {tokenType}");

        return DeviceIdentity.TryParse(text, out var value)
            ? value
            : throw new JsonException($"'{text}' is not a valid DeviceIdentity");
    }
}
