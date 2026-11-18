using Crossdeck.Core.Models;

namespace Crossdeck.Core.Routing.Adapters;

/// <summary>
/// Provides a factory for creating adapters that can build text screens for aircraft displays.
/// </summary>
public interface IAircraftScreenAdapterFactory
{
    /// <summary>
    /// Attempts to get an aircraft screen adapter that can satisfy requests for the specified aircraft.
    /// </summary>
    /// <param name="simulator">The simulator for which an adapter is requested.</param>
    /// <param name="aircraft">The identifier of the aircraft for which an adapter is requested.</param>
    /// <param name="adapter">When this method returns, contains the matching aircraft screen adapter if one was found; otherwise, null.</param>
    /// <returns>true if a matching adapter was found; otherwise, false.</returns>
    bool TryGet(Simulator simulator, SimulatorAircraft aircraft, out IAircraftScreenAdapter? adapter);
}