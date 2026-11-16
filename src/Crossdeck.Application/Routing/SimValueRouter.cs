using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Routing.Models;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Routing;

/*
 * TODO: Logic to handle
 * 2. Profile updates
 *  a. New ref added - update subscriptions, append reference
 *  b. New ref removed - update subscriptions, remove reference
 * 3. Profile changes
 *  a. New profile loaded - wipe subscriptions. Rebuild references
 *  b. Profile unloaded - wipe subscriptions
 * 4. Device changes (added, removed)
 *  a. Added - replay events for device
 *
 * Loops:
 * 1. Sim Event loop (created when simulator is connected)
 * 2.
 */
public sealed class SimValueRouter : ISimValueRouter, ISimValueCache
{
    private readonly IActiveSim _activeSim;
    private readonly IDeviceRegistry _deviceRegistry;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SimValueRouter> _logger;
    private readonly ConcurrentDictionary<SimRef, SimValueCacheEntry> _simCache = new();

    private readonly Channel<RouterCommand> _commands = Channel.CreateUnbounded<RouterCommand>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
        });

    private CancellationTokenSource? _simulatorEventsCancellationToken;
    private Task? _simulatorEventsTask;
    private ISimProducer? _currentProducer;

    public SimValueRouter(
        IActiveSim activeSim,
        IDeviceRegistry deviceRegistry,
        TimeProvider timeProvider,
        ILogger<SimValueRouter> logger)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _activeSim.Changing += ActiveSimOnChanging;
        _activeSim.Changed += ActiveSimOnChanged;
    }

    public event EventHandler<string?>? AircraftChanged;

    public void SetBindings(IReadOnlyList<OutputMapping> bindings) => throw new NotImplementedException();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_activeSim.Current is not null)
            _commands.Writer.TryWrite(new RouterCommand.SimChanged(_activeSim.Current));

        await foreach (var command in _commands.Reader.ReadAllAsync(cancellationToken))
        {
            switch (command)
            {
                case RouterCommand.SimChanging:
                    await HandleSimulatorChanging();
                    break;
                case RouterCommand.SimChanged p:
                    HandleSimulatorChanged(p, cancellationToken);
                    break;
                case RouterCommand.SimReady:
                    break;
            }
        }
    }

    public bool TryGet(SimRef reference, out SimValueCacheEntry? value) =>
        _simCache.TryGetValue(reference, out value);

    public ImmutableDictionary<SimRef, SimValueCacheEntry> GetSnapshot() => _simCache.ToImmutableDictionary();

    private void HandleSimulatorChanged(RouterCommand.SimChanged p, CancellationToken cancellationToken)
    {
        if (ReferenceEquals(p.Producer, _currentProducer))
            return;

        _logger.LogInformation("Simulator changed");
        _simCache.Clear();

        _currentProducer = p.Producer;
        _currentProducer.Ready += CurrentProducerOnReady;
        _simulatorEventsCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _simulatorEventsTask = RunSimEventLoopAsync(_currentProducer!, _simulatorEventsCancellationToken!.Token);
    }

    private async Task HandleSimulatorChanging()
    {
        _logger.LogInformation("Simulator changing");
        if (_currentProducer is null)
            return;

        _currentProducer.Ready -= CurrentProducerOnReady;

        if (_simulatorEventsCancellationToken is null || _simulatorEventsTask is null)
            return;

        await _simulatorEventsCancellationToken!.CancelAsync();
        await _simulatorEventsTask;

        _simulatorEventsCancellationToken = null;
        _simulatorEventsTask = null;
        _currentProducer = null;
    }

    private async Task RunSimEventLoopAsync(
        ISimProducer producer,
        CancellationToken cancellationToken)
    {
        await foreach (var simEvent in producer.GetEventsAsync(cancellationToken))
        {
            switch (simEvent)
            {
                case SimEvent.AircraftChanged ac:
                    _logger.LogDebug("Aircraft changed {Aircraft}", ac.AircraftId);
                    AircraftChanged?.Invoke(this, ac.AircraftId);
                    break;
                case SimEvent.ValueChanged v:
                    _logger.LogDebug("Simulator value changed {SimRef}:{Value}", v.Ref, v.Value);

                    _simCache[v.Ref] = new SimValueCacheEntry(v.Value, _timeProvider.GetTimestamp());
                    break;
            }
        }
    }

    private void ActiveSimOnChanged(object? sender, ActiveSimChangedEventArgs e)
    {
        if (e.Current is null)
            return;

        _commands.Writer.TryWrite(new RouterCommand.SimChanged(e.Current));
    }

    private void CurrentProducerOnReady(object? sender, SimReady e) =>
        _commands.Writer.TryWrite(RouterCommand.SimReady.Instance.Value);

    private void ActiveSimOnChanging(object? sender, ActiveSimChangingEventArgs e) =>
        _commands.Writer.TryWrite(RouterCommand.SimChanging.Instance.Value);

    internal closed record RouterCommand
    {
        internal sealed record SimChanging : RouterCommand
        {
            public static readonly Lazy<SimChanging> Instance = new(() => new());

            private SimChanging()
            {
            }
        }

        internal sealed record SimChanged(ISimProducer Producer) : RouterCommand;

        internal sealed record SimReady : RouterCommand
        {
            public static readonly Lazy<SimReady> Instance = new(() => new());

            private SimReady()
            {
            }
        }
    }
}