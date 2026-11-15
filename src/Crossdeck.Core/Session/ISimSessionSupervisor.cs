namespace Crossdeck.Core.Session;

/// <summary>
/// Monitors for running simulators and manages the active simulator session.
/// </summary>
public interface ISimSessionSupervisor : IActiveSim, IRunnable;