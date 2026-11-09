using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Tests.Devices.Models;

public sealed class InputIdTests
{
    [Theory, AutoNSubstituteData]
    public void ToString_Always_FormatsAsKindColonName(InputKind kind, string name)
    {
        // Arrange
        var inputId = new InputId(kind, name);

        // Act
        var result = inputId.ToString();

        // Assert
        Assert.Equal($"{kind.ToString().ToLowerInvariant()}:{name}", result);
    }
}
