using Crossdeck.Core.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Simulators;

/// <summary>
/// Provides access to the events, values and commands of a connected simulator.
/// </summary>
public interface ISimProducer : IAsyncDisposable
{
    /// <summary>
    /// Gets the simulator that this producer is responsible for.
    /// </summary>
    Simulator Simulator { get; }

    /// <summary>
    /// Occurs when the simulator becomes ready.
    /// </summary>
    event EventHandler<SimReady>? Ready;

    /// <summary>
    /// Provides an asynchronous read over the simulator events.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumeration of simulator events.</returns>
    IAsyncEnumerable<SimEvent> GetEventsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets which simulator values are subscribed for event reporting.
    /// </summary>
    /// <param name="subscriptions">The simulator references to subscribe to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SetSubscriptionsAsync(IReadOnlySet<SimRef> subscriptions, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the producer, reporting simulator events until cancelled.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends a command to the simulator.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the command.</returns>
    Task<CommandResult> SendCommandAsync(SimCommand command, CancellationToken cancellationToken);
}
