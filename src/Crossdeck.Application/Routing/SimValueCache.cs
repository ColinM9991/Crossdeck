using System.Collections.Concurrent;
using System.Collections.Immutable;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Routing.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing;

public sealed class SimValueCache : ISimValueCache
{
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<SimRef, SimValueCacheEntry> _simCache = new();

    public SimValueCache(
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Clear() => _simCache.Clear();

    public bool TryGet(SimRef reference, out SimValueCacheEntry? value) =>
        _simCache.TryGetValue(reference, out value);

    public void TrySet(SimRef reference, SimValue value)
    {
        var updatedAt = _timeProvider.GetTimestamp();
        var newEntry = new SimValueCacheEntry(value, updatedAt);

        _simCache.AddOrUpdate(reference, newEntry,
            (_, entry) => updatedAt > entry.UpdatedAt ? newEntry : entry);
    }

    public ImmutableDictionary<SimRef, SimValueCacheEntry> GetSnapshot() => _simCache.ToImmutableDictionary();
}