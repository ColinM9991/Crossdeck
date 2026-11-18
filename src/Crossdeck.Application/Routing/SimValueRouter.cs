using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Threading.Channels;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
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

    private volatile OutputIndex _outputIndex = OutputIndex.Empty;
    private CancellationTokenSource? _simulatorEventsCancellationTokenSource;
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

    public void SetBindings(IReadOnlyList<OutputMapping> bindings)
    {
        var bindingIndex = BuildIndex(bindings);

        _outputIndex = bindingIndex;
        _logger.LogInformation("Output bindings set {Count}", bindings.Count);

        _commands.Writer.TryWrite(new RouterCommand.ProfileBindingUpdate(bindingIndex));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_activeSim.Current is not null)
            _commands.Writer.TryWrite(new RouterCommand.SimChanged(_activeSim.Current));

        try
        {
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
                    case RouterCommand.ProfileBindingUpdate update:
                        await HandleProfileBindingUpdateAsync(update, cancellationToken);
                        break;
                }
            }
        }
        finally
        {
            await StopSimEventLoopAsync();
        }
    }

    public bool TryGet(SimRef reference, out SimValueCacheEntry? value) =>
        _simCache.TryGetValue(reference, out value);

    public ImmutableDictionary<SimRef, SimValueCacheEntry> GetSnapshot() => _simCache.ToImmutableDictionary();

    private static OutputIndex BuildIndex(IReadOnlyList<OutputMapping> mappings)
    {
        var byRef = new Dictionary<SimRef, List<OutputBinding>>();
        var byDevice = new Dictionary<DeviceIdentity, List<OutputBinding>>();
        var requiredRefs = new HashSet<SimRef>();

        foreach (var mapping in mappings)
        {
            var outputBinding = new OutputBinding(mapping);

            foreach (var simRef in mapping.Sources.Values.Distinct())
            {
                if (!byRef.TryGetValue(simRef, out var bindings))
                {
                    bindings = [];
                    byRef[simRef] = bindings;
                }

                bindings.Add(outputBinding);
                requiredRefs.Add(simRef);
            }

            if (mapping.Sink is not OutputSink.Indicator indicator) continue;
            {
                if (!byDevice.TryGetValue(indicator.Device, out var deviceBindings))
                {
                    deviceBindings = [];
                    byDevice[indicator.Device] = deviceBindings;
                }

                deviceBindings.Add(outputBinding);
            }
        }

        var byRefFrozen =
            byRef.ToFrozenDictionary(
                x => x.Key,
                x => x.Value.Distinct().ToArray());
        var byDeviceFrozen = byDevice.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Distinct().ToArray());

        return new OutputIndex(byRefFrozen, byDeviceFrozen, requiredRefs.ToFrozenSet());
    }

    private void HandleSimulatorChanged(RouterCommand.SimChanged p, CancellationToken cancellationToken)
    {
        if (ReferenceEquals(p.Producer, _currentProducer))
            return;

        _logger.LogInformation("Simulator changed {Producer}", p.Producer);
        _simCache.Clear();

        _currentProducer = p.Producer;
        p.Producer.Ready += CurrentProducerOnReady;

        var simulatorEventsCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _simulatorEventsCancellationTokenSource = simulatorEventsCancellationTokenSource;
        _simulatorEventsTask = RunSimEventLoopAsync(p.Producer, simulatorEventsCancellationTokenSource.Token);
    }

    private async Task HandleSimulatorChanging()
    {
        if (_currentProducer is null && _simulatorEventsTask is null)
            return;

        _logger.LogInformation("Simulator changing");
        await StopSimEventLoopAsync();
    }

    private async Task HandleProfileBindingUpdateAsync(
        RouterCommand.ProfileBindingUpdate update,
        CancellationToken cancellationToken)
    {
        if (_currentProducer is null)
            return;

        try
        {
            await _currentProducer.SetSubscriptionsAsync(update.Index.RequiredRefs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The run is stopping; subscriptions are re-applied when a session starts.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set simulator subscriptions");
        }
    }

    private async Task StopSimEventLoopAsync()
    {
        if (_simulatorEventsCancellationTokenSource is not null)
        {
            var cts = _simulatorEventsCancellationTokenSource;
            _simulatorEventsCancellationTokenSource = null;

            await cts.CancelAsync();

            if (_simulatorEventsTask is not null)
            {
                var task = _simulatorEventsTask;
                _simulatorEventsTask = null;

                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    // The loop exits with an OperationCanceledException when its token is cancelled.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Simulator event loop faulted");
                }
            }

            cts.Dispose();
        }

        if (_currentProducer is not null)
        {
            _currentProducer.Ready -= CurrentProducerOnReady;
            _currentProducer = null;
        }
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
                    HandleValueChanged(v);
                    break;
            }
        }
    }

    private void HandleValueChanged(SimEvent.ValueChanged valueChanged)
    {
        _logger.LogDebug("Simulator value changed {SimRef}:{Value}", valueChanged.Ref, valueChanged.Value);

        _simCache[valueChanged.Ref] = new SimValueCacheEntry(valueChanged.Value, _timeProvider.GetTimestamp());

        if (!_outputIndex.ByRef.TryGetValue(valueChanged.Ref, out var bindings))
            return;

        foreach (var binding in bindings)
        {
            try
            {
                Dispatch(binding, valueChanged.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling binding for {SimRef}:{Value}", valueChanged.Ref,
                    valueChanged.Value);
            }
        }
    }

    private void Dispatch(OutputBinding binding, SimValue value)
    {
        if (binding.Mapping.Sources.Count > 1)
        {
            _logger.LogWarning("Binding {Sink} has multiple sources; skipping", binding.Mapping.Sink);
            return; // TODO: Implement transformations
        }

        if (value is not SimValue.Number valueNumber)
        {
            _logger.LogWarning("Binding {Sink} skipped: value is not a number", binding.Mapping.Sink);
            return;
        }

        if (binding.Mapping.Sink is OutputSink.Indicator indicator
            && _deviceRegistry.TryGet(indicator.Device, out var driver)
            && driver is IIndicatorOutput output)
            output.Set(indicator.Output, (int)valueNumber.Value);
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

    /// <summary>
    /// Contains the commands that can be dispatched within the router.
    /// </summary>
    internal closed record RouterCommand
    {
        /// <summary>
        /// Represents a router command indicating that the active simulator producer has changing.
        /// </summary>
        internal sealed record SimChanging : RouterCommand
        {
            public static readonly Lazy<SimChanging> Instance = new(() => new());

            private SimChanging()
            {
            }
        }

        /// <summary>
        /// Represents a router command indicating that the active simulator producer has changed.
        /// </summary>
        internal sealed record SimChanged(ISimProducer Producer) : RouterCommand;

        /// <summary>
        /// Represents a router command indicating that the active simulator producer is ready.
        /// </summary>
        internal sealed record SimReady : RouterCommand
        {
            public static readonly Lazy<SimReady> Instance = new(() => new());

            private SimReady()
            {
            }
        }

        /// <summary>
        /// Represents a router command that carries an updated binding index for profile output mappings.
        /// </summary>
        internal sealed record ProfileBindingUpdate(OutputIndex Index) : RouterCommand;
    }
}