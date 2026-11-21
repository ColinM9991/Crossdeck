using System.Collections.Concurrent;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing;

internal sealed class VariableStore : IVariableStore
{
    private readonly ConcurrentDictionary<SimRef.Variable, SimValue> _simCache = new();

    public IReadOnlySet<SimRef.Variable> Declared => _simCache.Keys.ToHashSet();

    public event EventHandler<VariableChange>? Changed;

    public bool TryGet(SimRef.Variable variable, out SimValue? value) => _simCache.TryGetValue(variable, out value);

    public void Set(SimRef.Variable variable, SimValue value)
    {
        _simCache.AddOrUpdate(variable, value,
            (_, _) => value);

        Changed?.Invoke(this, new VariableChange(variable, value));
    }
}