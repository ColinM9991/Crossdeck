using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Crossdeck.Application.Profiles.Json;
using Crossdeck.Application.Routing.Output;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Tests.Profiles;

public sealed class ProfileSerializerTests
{
    private const string DeviceKey = "3f2a9c1e-5b7d-4e21-9a0c-6d8e1f2b3c4d";

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_WithAValidProfileDocument_ReturnsLoadedWithTheRoundTrippedProfile(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();

        var document = JsonSerializer.SerializeToNode(profile, ProfileJsonOptions.Create())!;
        document.AsObject().Insert(0, "$schemaVersion", 1);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToJsonString()));

        // Act
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var loaded = Assert.IsType<ProfileLoadResult.Loaded>(result);

        Assert.Equal(1, loaded.Document.FileVersion);
        Assert.False(loaded.Document.Migrated);
        AssertProfileRoundTripped(profile, loaded.Document.Profile);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_WhenTheSchemaVersionIsMissing_ReturnsInvalidPointingAtTheSchemaVersionKey(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();

        var document = JsonSerializer.SerializeToNode(profile, ProfileJsonOptions.Create())!;
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToJsonString()));

        // Act
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var invalid = Assert.IsType<ProfileLoadResult.Invalid>(result);
        var problem = Assert.Single(invalid.Problems);

        Assert.Equal("/$schemaVersion", problem.Path);
        Assert.Equal("Missing schema version", problem.Message);
        Assert.Equal(ProblemSeverity.Error, problem.Severity);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_WhenTheSchemaVersionIsNewerThanTheCurrent_ReturnsTooNewCarryingBothVersions(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();

        var document = JsonSerializer.SerializeToNode(profile, ProfileJsonOptions.Create())!;
        document.AsObject().Insert(0, "$schemaVersion", 2);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToJsonString()));

        // Act
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(new ProfileLoadResult.TooNew(2, 1), result);
    }

    [Fact]
    public async Task ReadAsync_WithMalformedJson_ReturnsInvalidWithTheJsonExceptionPathAndMessage()
    {
        // Arrange
        var sut = CreateSerializer();

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("not json"));

        // Act
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var invalid = Assert.IsType<ProfileLoadResult.Invalid>(result);
        var problem = Assert.Single(invalid.Problems);

        Assert.Equal(string.Empty, problem.Path);
        Assert.Equal("'not json' is an invalid JSON literal. Expected the literal 'null'. LineNumber: 0 | BytePositionInLine: 1.", problem.Message);
        Assert.Equal(ProblemSeverity.Error, problem.Severity);
    }

    [Fact]
    public async Task ReadAsync_WhenTheJsonIsWellFormedButNotAnObject_ReturnsInvalid()
    {
        // Arrange
        var sut = CreateSerializer();

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("[1, 2, 3]"));

        // Act
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var invalid = Assert.IsType<ProfileLoadResult.Invalid>(result);
        var problem = Assert.Single(invalid.Problems);

        Assert.Equal(string.Empty, problem.Path);
        Assert.Equal("Invalid JSON profile", problem.Message);
        Assert.Equal(ProblemSeverity.Error, problem.Severity);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_WhenTheObjectPayloadIsNotAValidProfile_ReturnsInvalidPointingAtTheFaultingMember(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();

        var document = JsonSerializer.SerializeToNode(profile, ProfileJsonOptions.Create())!;
        document.AsObject().Insert(0, "$schemaVersion", 1);
        document.AsObject()["id"] = "not-a-guid";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToJsonString()));

        // Act
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var invalid = Assert.IsType<ProfileLoadResult.Invalid>(result);
        var problem = Assert.Single(invalid.Problems);

        Assert.Equal("$.id", problem.Path);
        Assert.Contains("System.Guid", problem.Message);
        Assert.Equal(ProblemSeverity.Error, problem.Severity);
    }

    [Fact]
    public async Task ReadAsync_WhenTheSchemaVersionIsNotAnInteger_ReturnsInvalidPointingAtTheSchemaVersionKey()
    {
        // Arrange
        var sut = CreateSerializer();

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{ "$schemaVersion": "2" }"""));

        // Act
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var invalid = Assert.IsType<ProfileLoadResult.Invalid>(result);
        var problem = Assert.Single(invalid.Problems);

        Assert.Equal("/$schemaVersion", problem.Path);
        Assert.Equal(ProblemSeverity.Error, problem.Severity);
    }

    [Theory, AutoNSubstituteData]
    public async Task WriteAsync_WithAProfile_ProducesIndentedJsonWithTheSchemaVersionAndTheProfilePayload(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();

        var stream = new MemoryStream();

        // Act
        await sut.WriteAsync(profile, stream, CancellationToken.None);
        var json = Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        Assert.Contains("\n  \"$schemaVersion\": 1", json);
        Assert.Contains($"\"id\": \"{profile.Id}\"", json);
    }

    [Theory, AutoNSubstituteData]
    public async Task WriteAsync_WhenTheWrittenDocumentIsReadBack_ReturnsTheRoundTrippedProfile(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();

        var stream = new MemoryStream();

        // Act
        await sut.WriteAsync(profile, stream, CancellationToken.None);
        stream.Position = 0;
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var loaded = Assert.IsType<ProfileLoadResult.Loaded>(result);

        Assert.Equal(1, loaded.Document.FileVersion);
        AssertProfileRoundTripped(profile, loaded.Document.Profile);
    }

    [Theory, AutoNSubstituteData]
    public async Task WriteAsync_ThenReadAsync_WithConverterExercisingData_ReturnsTheRoundTrippedProfile(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();
        var assigned = new DeviceIdentity(DeviceIdentitySource.Serial, 0x294B, 0x1901, "A1B2C3D4");
        var (enriched, command, set) = CreateConverterData(profile, assigned);
        var stream = new MemoryStream();

        // Act
        await sut.WriteAsync(enriched, stream, CancellationToken.None);
        stream.Position = 0;
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var loaded = Assert.IsType<ProfileLoadResult.Loaded>(result);

        Assert.Equal(1, loaded.Document.FileVersion);
        AssertProfileRoundTripped(enriched, loaded.Document.Profile);

        var actions = loaded.Document.Profile.Devices[DeviceKey]
            .Inputs[InputId.Parse("button:22")]
            .Events["Pressed"]
            .Actions;
        Assert.Equal(2, actions.Count);
        Assert.IsType<InputAction.Command>(actions[0]);
        Assert.IsType<InputAction.Set>(actions[1]);
        Assert.Equal(command, actions[0]);
        Assert.Equal(set, actions[1]);
    }

    [Theory, AutoNSubstituteData]
    public async Task WriteAsync_ThenReadAsync_WithAnUnassignedDeviceSlot_ReturnsTheRoundTrippedProfile(
        Profile profile)
    {
        // Arrange
        var sut = CreateSerializer();
        var (enriched, _, _) = CreateConverterData(profile, assigned: null);
        var stream = new MemoryStream();

        // Act
        await sut.WriteAsync(enriched, stream, CancellationToken.None);
        stream.Position = 0;
        var result = await sut.ReadAsync(stream, CancellationToken.None);

        // Assert
        var loaded = Assert.IsType<ProfileLoadResult.Loaded>(result);

        Assert.Equal(1, loaded.Document.FileVersion);
        AssertProfileRoundTripped(enriched, loaded.Document.Profile);
    }

    private static void AssertProfileRoundTripped(Profile expected, Profile actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Simulator, actual.Simulator);
        Assert.Equal(expected.Aircraft.Matches, actual.Aircraft.Matches);

        foreach (var key in AssertSameKeys(expected.Variables, actual.Variables))
            Assert.Equal(expected.Variables[key], actual.Variables[key]);

        foreach (var key in AssertSameKeys(expected.Devices, actual.Devices))
            AssertDeviceSlotRoundTripped(expected.Devices[key], actual.Devices[key]);

        foreach (var key in AssertSameKeys(expected.Outputs, actual.Outputs))
            AssertOutputDefinitionRoundTripped(expected.Outputs[key], actual.Outputs[key]);
    }

    private static void AssertDeviceSlotRoundTripped(DeviceSlot expected, DeviceSlot actual)
    {
        Assert.Equal(expected.Model, actual.Model);
        Assert.Equal(expected.Assigned, actual.Assigned);
        Assert.Equal(expected.Screen, actual.Screen);

        foreach (var key in AssertSameKeys(expected.Inputs, actual.Inputs))
            AssertInputDefinitionRoundTripped(expected.Inputs[key], actual.Inputs[key]);

        foreach (var key in AssertSameKeys(expected.Outputs, actual.Outputs))
            AssertOutputDefinitionRoundTripped(expected.Outputs[key], actual.Outputs[key]);
    }

    private static void AssertInputDefinitionRoundTripped(InputDefinition expected, InputDefinition actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Sync, actual.Sync);

        foreach (var key in AssertSameKeys(expected.Sources, actual.Sources))
            Assert.Equal(expected.Sources[key], actual.Sources[key]);

        foreach (var key in AssertSameKeys(expected.Events, actual.Events))
            AssertInputEventBindingRoundTripped(expected.Events[key], actual.Events[key]);
    }

    private static void AssertInputEventBindingRoundTripped(InputEventBinding expected, InputEventBinding actual)
    {
        Assert.Equal(expected.Actions.Count, actual.Actions.Count);
        for (var i = 0; i < expected.Actions.Count; i++)
            Assert.Equal(expected.Actions[i], actual.Actions[i]);

        Assert.Equal(expected.When, actual.When);
    }

    private static void AssertOutputDefinitionRoundTripped(OutputDefinition expected, OutputDefinition actual)
    {
        Assert.Equal(expected.Id, actual.Id);

        foreach (var key in AssertSameKeys(expected.Sources, actual.Sources))
            Assert.Equal(expected.Sources[key], actual.Sources[key]);

        Assert.Equal(expected.Transform, actual.Transform);
        Assert.Equal(expected.When, actual.When);
        Assert.Equal(expected.Fallback, actual.Fallback);
    }

    private static IEnumerable<TKey> AssertSameKeys<TKey, TValue>(
        ImmutableDictionary<TKey, TValue> expected,
        ImmutableDictionary<TKey, TValue> actual)
        where TKey : notnull
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var key in expected.Keys)
            Assert.True(actual.ContainsKey(key), $"Missing key {key}");

        return expected.Keys;
    }

    private static IProfileSerializer CreateSerializer()
    {
        var type = typeof(SimValueRouter).Assembly
            .GetType("Crossdeck.Application.Profiles.ProfileSerializer")
            ?? throw new InvalidOperationException();

        return (IProfileSerializer)Activator.CreateInstance(type)!;
    }

    private static (Profile Enriched, InputAction Command, InputAction Set) CreateConverterData(
        Profile profile,
        DeviceIdentity? assigned)
    {
        var command = new InputAction.Command(new SimRef.Plain("sim/starters/engage_starter_1"));
        var set = new InputAction.Set(new SimRef.Variable("fn"), new SetValue.Literal(new SimValue.Number(1)));
        var binding = new InputEventBinding(
            ImmutableList.Create<InputAction>(command, set),
            new ExpressionSpec.Raw("[fn] == 0"));
        var input = new InputDefinition(
            "6a1d2f3e-0b4c-4d5e-8f6a-7b8c9d0e1f2a",
            ImmutableDictionary<string, SimRef>.Empty,
            ImmutableDictionary<string, InputEventBinding>.Empty.SetItem("Pressed", binding));
        var output = new OutputDefinition(
            "0f1e2d3c-4b5a-4697-8a8b-9c0d1e2f3a4b",
            ImmutableDictionary<string, SimRef>.Empty.SetItem("bat", new SimRef.Plain("sim/cockpit/electrical/battery_on")),
            new ExpressionSpec.Raw("if([g] >= 1, 1, 0)"),
            new ExpressionSpec.Raw("[bat] == 1"),
            new OutputValue.Number(0));
        var slot = new DeviceSlot(
            new DeviceModel("294B", "1901", "Honeycomb Bravo Throttle Quadrant"),
            ImmutableDictionary<InputId, InputDefinition>.Empty.SetItem(InputId.Parse("button:22"), input),
            ImmutableDictionary<OutputId, OutputDefinition>.Empty,
            assigned);

        var enriched = profile with
        {
            Variables = profile.Variables.SetItem("fn", new VariableDefinition(new SimValue.Number(0))),
            Outputs = profile.Outputs.SetItem(output.Id, output),
            Devices = profile.Devices.SetItem(DeviceKey, slot)
        };

        return (enriched, command, set);
    }
}
