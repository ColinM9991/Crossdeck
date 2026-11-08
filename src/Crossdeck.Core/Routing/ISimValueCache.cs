using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Routing;

public interface ISimValueCache
{
    bool TryGet(SimRef reference, out SimValue? value);

    IReadOnlyDictionary<SimRef, (SimValue Value, long UpdatedAt)> GetSnapshot();
}