using Crossdeck.Core.Mapping;

namespace Crossdeck.Core.Routing;

public interface ISimValueRouter : IRunnable
{
    event EventHandler<string> AircraftChanged;

    void SetBindings(IReadOnlyList<OutputMapping> bindings);
}