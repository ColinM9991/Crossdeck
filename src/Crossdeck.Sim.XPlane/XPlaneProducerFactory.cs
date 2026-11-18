using Crossdeck.Core.Models;
using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators;
using Crossdeck.Sim.XPlane.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Sim.XPlane;

public sealed class XPlaneProducerFactory(
    IXPlaneApiService api,
    XPlaneOptions options,
    ILoggerFactory loggerFactory): ISimProducerFactory
{
    public Simulator Simulator => Simulator.XPlane;

    public ISimProducer Create() =>
        new XPlaneApiSimProducer(api, options, loggerFactory.CreateLogger<XPlaneApiSimProducer>());
}
