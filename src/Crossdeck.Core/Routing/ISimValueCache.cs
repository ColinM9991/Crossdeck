using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Routing;

/// <summary>
/// Provides a mechanism to cache and retrieve simulator values.
/// </summary>
public interface ISimValueCache
{
    bool TryGet(SimRef reference, out SimValue? value);

    IReadOnlyDictionary<SimRef, (SimValue Value, long UpdatedAt)> GetSnapshot();
}