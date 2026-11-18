using System.Collections.Frozen;
using Crossdeck.Core.Models;
using Crossdeck.Core.Routing.Adapters;

namespace Crossdeck.Application.Routing.Adapters;

/// <inheritdoc/>
internal sealed class AircraftScreenAdapterFactory : IAircraftScreenAdapterFactory
{
    private readonly IReadOnlyDictionary<Simulator, FrozenSet<IAircraftScreenAdapter>> _adapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="AircraftScreenAdapterFactory"/> class using the specified aircraft screen adapters.
    /// </summary>
    /// <param name="adapters">The adapters to group by simulator and use when resolving aircraft screen adapters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="adapters"/> is null.</exception>
    public AircraftScreenAdapterFactory(
        IEnumerable<IAircraftScreenAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);

        _adapters = adapters.GroupBy(x => x.Simulator)
            .ToDictionary(
                x => x.Key,
                x => x.ToFrozenSet())
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public bool TryGet(Simulator simulator, SimulatorAircraft aircraft, out IAircraftScreenAdapter? adapter)
    {
        adapter = null;

        if (!_adapters.TryGetValue(simulator, out var simulatorAdapters))
            return false;

        adapter = simulatorAdapters.FirstOrDefault(a => a.CanSatisfy(aircraft));
        return adapter is not null;
    }
}