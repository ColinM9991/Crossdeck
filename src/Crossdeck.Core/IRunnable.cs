namespace Crossdeck.Core;

/// <summary>
/// Represents a component that runs until cancelled.
/// </summary>
public interface IRunnable
{
    /// <summary>
    /// Runs the component until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RunAsync(CancellationToken cancellationToken);
}