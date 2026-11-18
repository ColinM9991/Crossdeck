using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Routing.Adapters;

/// <summary>
/// Provides an mechanism for building a <see cref="TextScreen"/> for a given aircraft.
/// </summary>
public interface IAircraftScreenAdapter
{
    /// <summary>
    /// Gets the simulator associated with the adapter.
    /// </summary>
    Simulator Simulator { get; }

    /// <summary>
    /// Determines whether this adapter can provide a screen for the specified aircraft.
    /// </summary>
    /// <param name="aircraft">The identifier of the aircraft to evaluate.</param>
    /// <returns>true if the adapter can satisfy requests for the specified aircraft; otherwise, false.</returns>
    bool CanSatisfy(SimulatorAircraft aircraft);

    /// <summary>
    /// Gets the simulator references required to build a screen for the specified display role.
    /// </summary>
    /// <param name="role">The display role for which required simulator references are requested.</param>
    /// <returns>The set of simulator references needed to build the screen for the specified display role.</returns>
    IReadOnlySet<SimRef> GetRequiredRefs(DisplayRole role);

    /// <summary>
    /// Builds a text screen for the specified display role using the supplied simulator values.
    /// </summary>
    /// <param name="role">The display role for which the screen is being built.</param>
    /// <param name="simValues">The simulator values available for constructing the screen, keyed by simulator reference.</param>
    /// <returns>The constructed text screen, or null if a screen cannot be built from the supplied values.</returns>
    TextScreen? BuildScreen(DisplayRole role, IReadOnlyDictionary<SimRef, SimValue> simValues);
}