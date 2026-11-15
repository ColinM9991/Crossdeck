using Crossdeck.Core.Models;
using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane;

public sealed class XPlaneProducerFactory(
    IXPlaneApiService api,
    XPlaneOptions options): ISimProducerFactory
{
    public Simulator Simulator => Simulator.XPlane;

    public ISimProducer Create() => new XPlaneApiSimProducer(api, options);
}
