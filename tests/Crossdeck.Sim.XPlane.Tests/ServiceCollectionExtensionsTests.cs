using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddXPlane_Always_RegistersSimProducerFactoryAsSingleton()
    {
        // Arrange
        var provider = new ServiceCollection().AddXPlane().BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<ISimProducerFactory>();
        var second = provider.GetRequiredService<ISimProducerFactory>();

        // Assert
        Assert.IsType<XPlaneProducerFactory>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void AddXPlane_Always_RegistersSimDetectorAsSingleton()
    {
        // Arrange
        var provider = new ServiceCollection().AddXPlane().BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<ISimDetector>();
        var second = provider.GetRequiredService<ISimDetector>();

        // Assert
        Assert.IsType<XPlaneApiSimDetector>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void AddXPlane_Always_RegistersSimProducerAsTransient()
    {
        // Arrange
        var provider = new ServiceCollection().AddXPlane().BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<ISimProducer>();
        var second = provider.GetRequiredService<ISimProducer>();

        // Assert
        Assert.IsType<XPlaneApiSimProducer>(first);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void AddXPlane_Always_RegistersDefaultHostAndPort()
    {
        // Arrange
        var provider = new ServiceCollection().AddXPlane().BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<XPlaneOptions>();

        // Assert
        Assert.Equal("localhost", options.Host);
        Assert.Equal(8086, options.Port);
    }

    [Fact]
    public void AddXPlane_Always_ConfiguresNamedHttpClientBaseAddress()
    {
        // Arrange
        var provider = new ServiceCollection().AddXPlane().BuildServiceProvider();

        // Act
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("xplane");

        // Assert
        Assert.Equal(new Uri("http://localhost:8086/api/v3/"), client.BaseAddress);
    }
}
