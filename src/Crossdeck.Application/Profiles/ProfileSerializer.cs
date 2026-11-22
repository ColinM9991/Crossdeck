using System.Text.Json;
using System.Text.Json.Nodes;
using Crossdeck.Application.Profiles.Json;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Application.Profiles;

internal sealed class ProfileSerializer : IProfileSerializer
{
    private const string SchemaVersionKey = "$schemaVersion";
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = ProfileJsonOptions.Create();

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
        if (!root.TryGetPropertyValue(SchemaVersionKey, out var schemaVersionNode) || schemaVersionNode is null)
            return Invalid($"/{SchemaVersionKey}", "Missing schema version");

        var versionNodeValue = schemaVersionNode.AsValue();

        if (!versionNodeValue.TryGetValue(out int schemaVersion) || schemaVersion < 0)
            return Invalid($"/{SchemaVersionKey}", "Invalid schema version");

        switch (schemaVersion)
        {
            case > CurrentSchemaVersion:
                return new ProfileLoadResult.TooNew(schemaVersion, CurrentSchemaVersion);
            default:
                root.Remove(SchemaVersionKey);
                try
                {
                    var profile = root.Deserialize<Profile>(SerializerOptions)!;
                    return new ProfileLoadResult.Loaded(new ProfileDocument(profile, schemaVersion, false));
                }
                catch (JsonException ex)
                {
                    return Invalid(ex.Path ?? "", ex.Message);
                }
        }
    }

    public async Task WriteAsync(Profile profile, Stream stream, CancellationToken cancellationToken)
    {
        var node = JsonSerializer.SerializeToNode(profile, SerializerOptions)!.AsObject();

        node.Insert(0, SchemaVersionKey, CurrentSchemaVersion);

        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        node?.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
    }

    private static ProfileLoadResult.Invalid Invalid(string path, string message) =>
        new([new ProfileProblem(path, message, ProblemSeverity.Error)]);
}