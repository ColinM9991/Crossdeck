using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Crossdeck.Core.Mapping.Input;

namespace Crossdeck.Application.Profiles.Json;

public static class ProfileJsonOptions
{
    public static JsonSerializerOptions Create() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowOutOfOrderMetadataProperties = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddInputActionPolymorphism } },
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
            new SimRefJsonConverter(),
            new InputIdJsonConverter(),
            new OutputIdJsonConverter(),
            new DeviceIdentityJsonConverter(),
            new ExpressionSpecJsonConverter(),
            new SimValueJsonConverter(),
            new OutputValueJsonConverter(),
            new SetValueJsonConverter(),
            new SortedImmutableDictionaryConverterFactory()
        }
    };

    private static void AddInputActionPolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(InputAction))
            return;

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "type",
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
            DerivedTypes =
            {
                new JsonDerivedType(typeof(InputAction.Command), "command"),
                new JsonDerivedType(typeof(InputAction.Set), "set")
            }
        };
    }
}
