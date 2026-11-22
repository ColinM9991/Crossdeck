using System.Text.Json;
using System.Text.Json.Serialization;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Profiles.Json;

public sealed class SimRefJsonConverter : JsonConverter<SimRef>
{
    public override SimRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"A sim reference must be a string, not {reader.TokenType}");

        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text)
            ? throw new JsonException("A sim reference must not be empty")
            : SimRef.Parse(text);
    }

    public override void Write(Utf8JsonWriter writer, SimRef value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Key);
}
