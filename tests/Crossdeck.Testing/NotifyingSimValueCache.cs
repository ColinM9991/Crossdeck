using System.Collections.Immutable;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Routing.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Testing;

/// <summary>
/// An <see cref="ISimValueCache"/> that delegates to another implementation and raises
/// <see cref="EntryCached"/> after each <see cref="TrySet"/>, so tests can synchronise with
/// a component that caches values on a background loop.
/// </summary>
public sealed class NotifyingSimValueCache : ISimValueCache
{
    private readonly ISimValueCache _inner;

    public NotifyingSimValueCache(ISimValueCache inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public event Action? EntryCached;

    public void Clear() => _inner.Clear();

    public bool TryGet(SimRef reference, out SimValueCacheEntry? value) =>
        _inner.TryGet(reference, out value);

    public void TrySet(SimRef reference, SimValue value)
    {
        _inner.TrySet(reference, value);
        EntryCached?.Invoke();
    }

    public ImmutableDictionary<SimRef, SimValueCacheEntry> GetSnapshot() => _inner.GetSnapshot();
}
