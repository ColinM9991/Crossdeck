using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Simulators;

public interface ISimProducer : IAsyncDisposable
{
    event EventHandler<SimReady>? Ready;

    IAsyncEnumerable<SimEvent> GetEventsAsync(CancellationToken cancellationToken);

    Task SetSubscriptionsAsync(IReadOnlySet<SimRef> subscriptions, CancellationToken cancellationToken);

    Task RunAsync(CancellationToken cancellationToken);

    Task<CommandResult> SendCommandAsync(SimCommand command, CancellationToken cancellationToken);
}
