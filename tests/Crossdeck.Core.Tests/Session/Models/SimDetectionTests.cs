using Crossdeck.Core.Models;
using Crossdeck.Core.Session.Models;

namespace Crossdeck.Core.Tests.Session.Models;

public sealed class SimDetectionTests
{
    [Fact]
    public void None_Always_HasDefaultSimulatorValue()
    {
        // Act
        var result = SimDetection.None;

        // Assert
        Assert.Equal(default, result.Simulator);
    }

    [Theory, AutoNSubstituteData]
    public void Constructor_WithSimulator_SetsSimulatorProperty(Simulator simulator)
    {
        // Act
        var result = new SimDetection(simulator);

        // Assert
        Assert.Equal(simulator, result.Simulator);
    }
}
