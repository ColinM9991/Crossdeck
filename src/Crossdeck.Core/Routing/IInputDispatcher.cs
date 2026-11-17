using Crossdeck.Core.Mapping.Input;

namespace Crossdeck.Core.Routing;

/// <summary>
/// Dispatches device input events to the active simulator.
/// </summary>
public interface IInputDispatcher : IRunnable
{
    void SetBindings(IReadOnlyList<InputMapping> bindings);
}