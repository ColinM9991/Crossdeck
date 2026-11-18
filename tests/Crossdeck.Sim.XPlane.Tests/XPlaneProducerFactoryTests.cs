using Crossdeck.Core.Models;
using Crossdeck.Sim.XPlane.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Sim.XPlane.Tests;

public sealed class XPlaneProducerFactoryTests
{
    [Theory, AutoNSubstituteData]
    public void Simulator_Always_IsXPlane(XPlaneProducerFactory sut)
    {
        // Act
        var result = sut.Simulator;

        // Assert
        Assert.Equal(Simulator.XPlane, result);
    }

    [Theory, AutoNSubstituteData]
    public void Create_Always_ReturnsXPlaneApiSimProducer(
        XPlaneOptions options,
        string host,
        int port,
        IXPlaneApiService api,
        ILoggerFactory loggerFactory)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneProducerFactory(api, options, loggerFactory);

        // Act
        var result = sut.Create();

        // Assert
        Assert.IsType<XPlaneApiSimProducer>(result);
    }
}
