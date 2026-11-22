using System.Text.Json;
using System.Text.Json.Serialization;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Profiles.Json;

public sealed class SetValueJsonConverter : JsonConverter<SetValue>
{
    public override SetValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return new SetValue.Literal(Converter<SimValue>(options).Read(ref reader, typeof(SimValue), options)!);

        if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException("A set value object must have exactly one property, 'ref' or 'expr'");

        var name = reader.GetString();
        reader.Read();
        SetValue result = name switch
        {
            "ref" => new SetValue.Ref(Converter<SimRef>(options).Read(ref reader, typeof(SimRef), options)!),
            "expr" => ReadExpression(ref reader),
            _ => throw new JsonException($"'{name}' is not a valid set value property, expected 'ref' or 'expr'")
        };

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("A set value object must have exactly one property, 'ref' or 'expr'");

        return result;
    }

    public override void Write(Utf8JsonWriter writer, SetValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case SetValue.Literal literal:
                Converter<SimValue>(options).Write(writer, literal.Value, options);
                break;
            case SetValue.Ref reference:
                writer.WriteStartObject();
                writer.WriteString("ref", reference.Source.Key);
                writer.WriteEndObject();
                break;
            case SetValue.Expr { Spec: ExpressionSpec.Raw raw }:
                writer.WriteStartObject();
                writer.WriteString("expr", raw.Expr);
                writer.WriteEndObject();
                break;
            default:
                throw new JsonException($"Unsupported set value type {value.GetType().Name}");
        }
    }

    private static SetValue.Expr ReadExpression(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"The 'expr' property must be a string, not {reader.TokenType}");

        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text)
            ? throw new JsonException("The 'expr' property must be a non-empty string")
            : new SetValue.Expr(new ExpressionSpec.Raw(text));
    }

    private static JsonConverter<T> Converter<T>(JsonSerializerOptions options) =>
        (JsonConverter<T>)options.GetConverter(typeof(T));
}
