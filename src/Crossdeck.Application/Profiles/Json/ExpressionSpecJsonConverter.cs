using System.Text.Json;
using System.Text.Json.Serialization;
using Crossdeck.Core.Mapping;

namespace Crossdeck.Application.Profiles.Json;

public sealed class ExpressionSpecJsonConverter : JsonConverter<ExpressionSpec>
{
    public override ExpressionSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"An expression must be an object, not {reader.TokenType}");

        string? expr = null;
        var properties = 0;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            properties++;
            if (reader.GetString() != "expr" || properties > 1)
                throw new JsonException("An expression must have exactly one property, 'expr'");

            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"The 'expr' property must be a string, not {reader.TokenType}");

            expr = reader.GetString();
        }

        return string.IsNullOrWhiteSpace(expr)
            ? throw new JsonException("The 'expr' property must be a non-empty string")
            : new ExpressionSpec.Raw(expr);
    }

    public override void Write(Utf8JsonWriter writer, ExpressionSpec value, JsonSerializerOptions options)
    {
        if (value is not ExpressionSpec.Raw raw)
            throw new JsonException($"Unsupported expression type {value.GetType().Name}");

        writer.WriteStartObject();
        writer.WriteString("expr", raw.Expr);
        writer.WriteEndObject();
    }
}
