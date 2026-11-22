using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crossdeck.Application.Profiles.Json;

public sealed class SortedImmutableDictionaryConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(SortedImmutableDictionaryConverter<,>).MakeGenericType(typeToConvert.GetGenericArguments());
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class SortedImmutableDictionaryConverter<TKey, TValue> : JsonConverter<ImmutableDictionary<TKey, TValue>>
        where TKey : notnull
    {
        public override ImmutableDictionary<TKey, TValue> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"A dictionary must be an object, not {reader.TokenType}");

            var keyConverter = (JsonConverter<TKey>)options.GetConverter(typeof(TKey));
            var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = keyConverter.ReadAsPropertyName(ref reader, typeof(TKey), options);
                reader.Read();
                var value = JsonSerializer.Deserialize<TValue>(ref reader, options)
                    ?? throw new JsonException($"The value for key '{key}' must not be null");

                if (!builder.TryAdd(key, value))
                    throw new JsonException($"Duplicate key '{key}'");
            }

            return builder.ToImmutable();
        }

        public override void Write(
            Utf8JsonWriter writer,
            ImmutableDictionary<TKey, TValue> value,
            JsonSerializerOptions options)
        {
            var keyConverter = (JsonConverter<TKey>)options.GetConverter(typeof(TKey));
            writer.WriteStartObject();
            foreach (var entry in value.OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal))
            {
                keyConverter.WriteAsPropertyName(writer, entry.Key, options);
                JsonSerializer.Serialize(writer, entry.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}
