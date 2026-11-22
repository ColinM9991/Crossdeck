using System.Text.Json;
using System.Text.Json.Nodes;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Application.Profiles;

internal sealed class ProfileSerializer : IProfileSerializer
{
    private const string SchemaVersionKey = "$schemaVersion";
    private const int CurrentSchemaVersion = 1;

    private readonly JsonSerializerOptions _serializerOptions = new();

    public async Task<ProfileLoadResult> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        JsonObject root;
        try
        {
            if (await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken) is not JsonObject jsonObject)
                return Invalid(string.Empty, "Invalid JSON profile");

            root = jsonObject;
        }
        catch (JsonException e)
        {
            return Invalid(e.Path ?? string.Empty, e.Message);
        }

        var schemaVersion = root[SchemaVersionKey]?.GetValue<int?>();
        switch (schemaVersion)
        {
            case null:
                return Invalid($"/{SchemaVersionKey}", "Missing schema version");
            case > CurrentSchemaVersion:
                return new ProfileLoadResult.TooNew(schemaVersion.Value, CurrentSchemaVersion);
            default:
                root.Remove(SchemaVersionKey);
                try
                {
                    var profile = root.Deserialize<Profile>(_serializerOptions)!;
                    return new ProfileLoadResult.Loaded(new ProfileDocument(profile, schemaVersion.Value, false));
                }
                catch (JsonException ex)
                {
                    return Invalid(ex.Path ?? "", ex.Message);
                }
        }
    }

    public async Task WriteAsync(Profile profile, Stream stream, CancellationToken cancellationToken)
    {
        var node = JsonSerializer.SerializeToNode(profile, _serializerOptions)!.AsObject();

        node.Insert(0, SchemaVersionKey, CurrentSchemaVersion);

        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        node?.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
    }

    private static ProfileLoadResult.Invalid Invalid(string path, string message) =>
        new([new ProfileProblem(path, message, ProblemSeverity.Error)]);
}