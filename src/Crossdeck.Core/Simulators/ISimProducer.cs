using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Simulators;

public interface ISimProducer : IAsyncDisposable
{
    IAsyncEnumerable<SimEvent> Events { get; }
    
    Task SetSubscriptionsAsync(IReadOnlySet<SimRef> subscriptions);

    Task RunAsync(CancellationToken cancellationToken);

    Task<CommandResult> SendCommandAsync(SimCommand command, CancellationToken cancellationToken);
}