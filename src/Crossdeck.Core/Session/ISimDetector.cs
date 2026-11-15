using Crossdeck.Core.Session.Models;

namespace Crossdeck.Core.Session;

/// <summary>
/// Detects running flight simulators.
/// </summary>
public interface ISimDetector
{
    /// <summary>
    /// Determines whether a simulator probe can be performed.
    /// </summary>
    /// <returns><c>true</c> if a probe can be performed; otherwise, <c>false</c>.</returns>
    bool CanProbe();

    /// <summary>
    /// Probes for a running simulator.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The detected simulator, or null if none was found.</returns>
    Task<SimDetection?> ProbeAsync(CancellationToken cancellationToken);
}