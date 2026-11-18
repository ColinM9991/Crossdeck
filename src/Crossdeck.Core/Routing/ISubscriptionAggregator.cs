using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Routing;

public interface ISubscriptionAggregator : IRunnable
{
    void SetOutputRefs(IReadOnlySet<SimRef> refs);

    void SetInputRefs(IReadOnlySet<SimRef> refs);
}