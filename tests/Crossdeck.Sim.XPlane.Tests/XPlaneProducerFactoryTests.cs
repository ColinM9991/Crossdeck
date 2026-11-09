using Crossdeck.Core.Models;
using Crossdeck.Sim.XPlane.Models;

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
        IHttpClientFactory httpClientFactory,
        XPlaneOptions options,
        string host,
        int port)
    {
        // Arrange
        options.Host = host;
        options.Port = port;
        var sut = new XPlaneProducerFactory(httpClientFactory, options);

        // Act
        var result = sut.Create();

        // Assert
        Assert.IsType<XPlaneApiSimProducer>(result);
    }
}
