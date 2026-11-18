using System.Collections.Frozen;
using System.Threading.Channels;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Routing;

public sealed class SubscriptionAggregator : ISubscriptionAggregator
{
    private readonly IActiveSim _activeSim;
    private readonly ILogger<SubscriptionAggregator> _logger;

    private readonly Channel<AggregatorCommand> _commands =
        Channel.CreateUnbounded<AggregatorCommand>(new UnboundedChannelOptions { SingleReader = true });

    private FrozenSet<SimRef> _outputRefs = FrozenSet<SimRef>.Empty;
    private FrozenSet<SimRef> _inputRefs = FrozenSet<SimRef>.Empty;
    private FrozenSet<SimRef> _watchRefs = FrozenSet<SimRef>.Empty;

    private ISimProducer? _currentProducer;

    public SubscriptionAggregator(
        IActiveSim activeSim,
        ILogger<SubscriptionAggregator> logger)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _activeSim.Changing += ActiveSimOnChanging;
        _activeSim.Changed += ActiveSimOnChanged;
    }

    public void SetOutputRefs(IReadOnlySet<SimRef> refs) =>
        _commands.Writer.TryWrite(new AggregatorCommand.OutputRefsUpdate(refs.ToFrozenSet()));

    public void SetInputRefs(IReadOnlySet<SimRef> refs) =>
        _commands.Writer.TryWrite(new AggregatorCommand.InputRefsUpdate(refs.ToFrozenSet()));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await foreach (var command in _commands.Reader.ReadAllAsync(cancellationToken))
        {
            switch (command)
            {
                case AggregatorCommand.OutputRefsUpdate update:
                    _outputRefs = update.Refs.ToFrozenSet();
                    break;
                case AggregatorCommand.InputRefsUpdate update:
                    _inputRefs = update.Refs.ToFrozenSet();
                    break;
                case AggregatorCommand.WatchListUpdate update:
                    _watchRefs = update.Refs.ToFrozenSet();
                    break;
                case AggregatorCommand.SimChanging:
                    _currentProducer = null;
                    break;
                case AggregatorCommand.SimChanged change:
                    _currentProducer = change.Producer;
                    break;
            }

            await ApplyAsync(cancellationToken);
        }
    }

    private static bool IsVariableRef(SimRef reference) =>
        reference.Key.StartsWith("var:", StringComparison.Ordinal);

    private void ActiveSimOnChanged(object? sender, ActiveSimChangedEventArgs e) =>
        _commands.Writer.TryWrite(new AggregatorCommand.SimChanged(e.Current));

    private void ActiveSimOnChanging(object? sender, ActiveSimChangingEventArgs e) =>
        _commands.Writer.TryWrite(AggregatorCommand.SimChanging.Instance.Value);

    private async Task ApplyAsync(CancellationToken cancellationToken)
    {
        if (_currentProducer is null)
            return;

        var union = _outputRefs
            .Union(_inputRefs)
            .Union(_watchRefs)
            .Where(static reference => !IsVariableRef(reference))
            .ToFrozenSet();

        try
        {
            await _currentProducer.SetSubscriptionsAsync(union, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The run is stopping.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set simulator subscriptions");
        }
    }

    internal closed record AggregatorCommand
    {
        internal sealed record OutputRefsUpdate(IReadOnlySet<SimRef> Refs) : AggregatorCommand;

        internal sealed record InputRefsUpdate(IReadOnlySet<SimRef> Refs) : AggregatorCommand;

        internal sealed record WatchListUpdate(IReadOnlySet<SimRef> Refs) : AggregatorCommand;

        internal sealed record SimChanging : AggregatorCommand
        {
            public static readonly Lazy<SimChanging> Instance = new(() => new());

            private SimChanging()
            {
            }
        }

        internal sealed record SimChanged(ISimProducer? Producer) : AggregatorCommand;
    }
}