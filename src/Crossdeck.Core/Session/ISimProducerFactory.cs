using Crossdeck.Core.Models;
using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session;

/// <summary>
/// Creates producers for a specific simulator.
/// </summary>
public interface ISimProducerFactory
{
    /// <summary>
    /// The simulator this factory produces producers for.
    /// </summary>
    Simulator Simulator { get; }

    /// <summary>
    /// Creates a new simulator producer.
    /// </summary>
    /// <returns>A producer for this factory's simulator.</returns>
    ISimProducer Create();
}