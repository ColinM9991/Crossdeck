using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Routing;

/// <summary>
/// Provides a mechanism to cache and retrieve simulator values.
/// </summary>
public interface ISimValueCache
{
    /// <summary>
    /// Attempts to retrieve a cached simulator value.
    /// </summary>
    /// <param name="reference">The reference of the value to retrieve.</param>
    /// <param name="value">When this method returns, the cached value, or null if not found.</param>
    /// <returns><c>true</c> if a value was found; otherwise, <c>false</c>.</returns>
    bool TryGet(SimRef reference, out SimValue? value);

    /// <summary>
    /// Gets a snapshot of all cached simulator values.
    /// </summary>
    /// <returns>The cached values and the time each was last updated.</returns>
    IReadOnlyDictionary<SimRef, (SimValue Value, long UpdatedAt)> GetSnapshot();
}