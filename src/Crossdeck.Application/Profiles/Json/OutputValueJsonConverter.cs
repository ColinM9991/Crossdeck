using System.Text.Json;
using System.Text.Json.Serialization;
using Crossdeck.Core.Mapping;

namespace Crossdeck.Application.Profiles.Json;

public sealed class OutputValueJsonConverter : JsonConverter<OutputValue>
{
    public override OutputValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => new OutputValue.Boolean(true),
            JsonTokenType.False => new OutputValue.Boolean(false),
            JsonTokenType.Number => new OutputValue.Number(reader.GetDouble()),
            JsonTokenType.String => new OutputValue.Text(reader.GetString()!),
            _ => throw new JsonException($"An output value must be a boolean, a number or a string, not {reader.TokenType}")
        };

    public override void Write(Utf8JsonWriter writer, OutputValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case OutputValue.Boolean boolean:
                writer.WriteBooleanValue(boolean.Value);
                break;
            case OutputValue.Number number:
                writer.WriteNumberValue(number.Value);
                break;
            case OutputValue.Text text:
                writer.WriteStringValue(text.Value);
                break;
            default:
                throw new JsonException($"Unsupported output value type {value.GetType().Name}");
        }
    }
}
