using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session;

/// <summary>
/// Exposes the currently active simulator session.
/// </summary>
public interface IActiveSim
{
    /// <summary>
    /// The active simulator session, or null when no simulator is active.
    /// </summary>
    ISimProducer? Current { get; }

    /// <summary>
    /// Occurs before the active simulator session changes.
    /// </summary>
    event EventHandler<ActiveSimChangingEventArgs> Changing;

    /// <summary>
    /// Occurs after the active simulator session changes.
    /// </summary>
    event EventHandler<ActiveSimChangedEventArgs> Changed;
}