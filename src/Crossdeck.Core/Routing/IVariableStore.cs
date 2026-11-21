using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Routing;

public interface IVariableStore
{
    IReadOnlySet<SimRef.Variable> Declared { get; }

    event EventHandler<VariableChange>? Changed;

    bool TryGet(SimRef.Variable variable, out SimValue? value);

    void Set(SimRef.Variable variable, SimValue value);
}

public sealed record VariableChange(SimRef.Variable Variable, SimValue Value);