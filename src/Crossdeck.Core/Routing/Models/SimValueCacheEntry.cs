using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Routing.Models;

public sealed record SimValueCacheEntry(SimValue Value, long UpdatedAt);