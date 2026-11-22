using Crossdeck.Application.Profiles.Validation;

namespace Crossdeck.Application.Tests.Profiles.Validation;

public sealed class ProfilePathTests
{
    [Theory]
    [InlineData("Name", "/name")]
    [InlineData("Aircraft.Matches", "/aircraft/matches")]
    [InlineData("Variables/mode.Value.Initial", "/variables/mode/initial")]
    [InlineData("Outputs/busPowered.Value.Transform", "/outputs/busPowered/transform")]
    [InlineData("Variables/mode.Key", "/variables/mode")]
    [InlineData(
        "Devices/3f2a.Value.Inputs/button:25.Value.Events/Pressed.Value.When",
        "/devices/3f2a/inputs/button:25/events/Pressed/when")]
    [InlineData(
        "Devices/3f2a.Value.Inputs/button:25.Value.Events/Pressed.Value.Actions[2].Value.Source.Id",
        "/devices/3f2a/inputs/button:25/events/Pressed/actions/2/value/source/id")]
    public void FromPropertyName_WithAFluentValidationPropertyChain_ReturnsAJsonPointer(
        string propertyName,
        string expected)
    {
        // Act
        var path = ProfilePath.FromPropertyName(propertyName);

        // Assert
        Assert.Equal(expected, path);
    }

    [Fact]
    public void FromPropertyName_WithAnEmptyPropertyName_ReturnsTheRootPointer()
    {
        // Act
        var path = ProfilePath.FromPropertyName(string.Empty);

        // Assert
        Assert.Equal("/", path);
    }
}
