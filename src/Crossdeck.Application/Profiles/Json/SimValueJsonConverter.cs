using System.Text.Json;
using System.Text.Json.Serialization;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Profiles.Json;

public sealed class SimValueJsonConverter : JsonConverter<SimValue>
{
    public override bool HandleNull => true;

    public override SimValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return new SimValue.None();
            case JsonTokenType.String:
                return new SimValue.String(reader.GetString()!);
            case JsonTokenType.Number:
                return new SimValue.Number(reader.GetDouble());
            case JsonTokenType.StartArray:
                return ReadArray(ref reader, typeToConvert, options);
            default:
                throw new JsonException($"A sim value must be null, a string, a number or an array, not {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, SimValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case SimValue.None:
                writer.WriteNullValue();
                break;
            case SimValue.String text:
                writer.WriteStringValue(text.Value);
                break;
            case SimValue.Number number:
                writer.WriteNumberValue(number.Value);
                break;
            case SimValue.NumberArray numbers:
                writer.WriteStartArray();
                foreach (var number in numbers.Values)
                    writer.WriteNumberValue(number);

                writer.WriteEndArray();
                break;
            case SimValue.Array values:
                writer.WriteStartArray();
                foreach (var item in values.Values)
                    Write(writer, item, options);

                writer.WriteEndArray();
                break;
            default:
                throw new JsonException($"Unsupported sim value type {value.GetType().Name}");
        }
    }

    private SimValue ReadArray(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var items = new List<SimValue>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            items.Add(Read(ref reader, typeToConvert, options));

        return items.All(item => item is SimValue.Number)
            ? new SimValue.NumberArray(items.Cast<SimValue.Number>().Select(number => number.Value).ToList())
            : new SimValue.Array(items);
    }
}
