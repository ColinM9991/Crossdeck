using Crossdeck.Core.Mapping;

namespace Crossdeck.Core.Routing;

/// <summary>
/// Routes simulator values to device outputs based on configured bindings.
/// </summary>
public interface ISimValueRouter : IRunnable
{
    /// <summary>
    /// Occurs when the active aircraft changes.
    /// </summary>
    event EventHandler<string?>? AircraftChanged;

    /// <summary>
    /// Sets the mappings used to route simulator values to device outputs.
    /// </summary>
    /// <param name="bindings">The output mappings to apply.</param>
    void SetBindings(IReadOnlyList<OutputMapping> bindings);
}