using Crossdeck.Core.Models;
using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session;

public interface ISimProducerFactory
{
    Simulator Simulator { get; }
    
    ISimProducer Create();
}