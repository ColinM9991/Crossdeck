using Crossdeck.Core;
using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators;

namespace Crossdeck.Sim.XPlane;

public sealed class XPlaneProducerFactory(
    IHttpClientFactory httpClientFactory,
    XPlaneOptions options): ISimProducerFactory
{
    public Simulator Simulator => Simulator.XPlane;

    public ISimProducer Create() => new XPlaneApiSimProducer(httpClientFactory, options);
}