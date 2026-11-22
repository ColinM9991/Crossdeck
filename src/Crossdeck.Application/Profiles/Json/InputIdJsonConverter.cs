using System.Text.Json;
using System.Text.Json.Serialization;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Application.Profiles.Json;

public sealed class InputIdJsonConverter : JsonConverter<InputId>
{
    public override InputId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString(), reader.TokenType);

    public override InputId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString(), JsonTokenType.String);

    public override void Write(Utf8JsonWriter writer, InputId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());

    public override void WriteAsPropertyName(Utf8JsonWriter writer, InputId value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value.ToString());

    private static InputId Parse(string? text, JsonTokenType tokenType)
    {
        if (tokenType != JsonTokenType.String)
            throw new JsonException($"A InputId must be a string, not {tokenType}");

        return InputId.TryParse(text, out var value)
            ? value
            : throw new JsonException($"'{text}' is not a valid InputId");
    }
}
