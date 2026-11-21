using System.Collections.Frozen;
using System.Threading.Channels;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Output;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Routing.Adapters;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Routing.Output;

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
public sealed class SimValueRouter : ISimValueRouter
{
    private readonly IActiveSim _activeSim;
    private readonly IDeviceRegistry _deviceRegistry;
    private readonly ISimValueCache _simValueCache;
    private readonly ISubscriptionAggregator _subscriptionAggregator;
    private readonly IAircraftScreenAdapterFactory _aircraftScreenAdapterFactory;
    private readonly ILogger<SimValueRouter> _logger;

    private readonly Channel<RouterCommand> _commands = Channel.CreateUnbounded<RouterCommand>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
        });

    private volatile OutputIndex _outputIndex = OutputIndex.Empty;
    private volatile ScreenIndex _screenIndex = ScreenIndex.Empty;

    private CancellationTokenSource? _simulatorEventsCancellationTokenSource;
    private Task? _simulatorEventsTask;
    private ISimProducer? _currentProducer;

    public SimValueRouter(
        IActiveSim activeSim,
        IDeviceRegistry deviceRegistry,
        ISimValueCache simValueCache,
        ISubscriptionAggregator subscriptionAggregator,
        IAircraftScreenAdapterFactory aircraftScreenAdapterFactory,
        ILogger<SimValueRouter> logger)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));
        _simValueCache = simValueCache ?? throw new ArgumentNullException(nameof(simValueCache));
        _subscriptionAggregator =
            subscriptionAggregator ?? throw new ArgumentNullException(nameof(subscriptionAggregator));
        _aircraftScreenAdapterFactory = aircraftScreenAdapterFactory ??
                                        throw new ArgumentNullException(nameof(aircraftScreenAdapterFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _activeSim.Changing += ActiveSimOnChanging;
        _activeSim.Changed += ActiveSimOnChanged;

        deviceRegistry.DriverAdded += DeviceRegistryOnDriverAdded;
        deviceRegistry.DriverRemoved += DeviceRegistryOnDriverRemoved;
    }

    public event EventHandler<string?>? AircraftChanged;
    public event EventHandler<ScreenRenderedEventArgs>? ScreenRendered;

    public void SetBindings(IReadOnlyList<OutputMapping> bindings)
    {
        var bindingIndex = BuildOutputIndex(bindings);

        _outputIndex = bindingIndex;
        _logger.LogInformation("Output bindings set {Count}", bindings.Count);
        _commands.Writer.TryWrite(new RouterCommand.BindingsUpdated());
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
                    case RouterCommand.AircraftChanged ac:
                        HandleAircraftChanged(ac);
                        break;
                    case RouterCommand.ScreenDriverAdded driver:
                        HandleScreenDriverAdded(driver);
                        break;
                    case RouterCommand.ScreenDriverRemoved removed:
                        HandleScreenDriverRemoved(removed.Identity);
                        break;
                    case RouterCommand.BindingsUpdated:
                        RebuildScreenIndex();
                        break;
                }
            }
        }
        finally
        {
            await StopSimEventLoopAsync();
        }
    }

    private void PublishRequiredRefs()
    {
        var requiredRefs = _outputIndex
            .RequiredRefs
            .Union(_screenIndex.RequiredRefs)
            .ToFrozenSet();

        _subscriptionAggregator.SetOutputRefs(requiredRefs);
    }

    private void RebuildScreenIndex()
    {
        var requiredRefs = new HashSet<SimRef>();
        var byRef = new Dictionary<SimRef, HashSet<DisplayRole>>();

        if (_screenIndex.Adapter is { } adapter)
        {
            foreach (var role in _screenIndex.ScreenRoles.Values.Distinct())
            {
                foreach (var simRef in adapter.GetRequiredRefs(role))
                {
                    requiredRefs.Add(simRef);
                    byRef.TryAdd(simRef, []);
                    byRef[simRef].Add(role);
                }
            }
        }

        var valueBuffers = _screenIndex.ScreenRoles.Values
            .Distinct()
            .ToDictionary(
                role => role,
                role =>
                {
                    var buffer = new Dictionary<SimRef, SimValue>();
                    foreach (var simRef in _screenIndex.Adapter?.GetRequiredRefs(role) ?? Enumerable.Empty<SimRef>())
                    {
                        if (_simValueCache.TryGet(simRef, out var entry))
                            buffer[simRef] = entry?.Value ?? new SimValue.None();
                    }

                    return buffer;
                })
            .ToFrozenDictionary();

        _screenIndex = _screenIndex with
        {
            ValueBuffers = valueBuffers.ToFrozenDictionary(),
            RequiredRefs = requiredRefs.ToFrozenSet(),
            ByRef = byRef.ToFrozenDictionary(x => x.Key, x => x.Value.ToFrozenSet())
        };

        PublishRequiredRefs();
    }

    private static OutputIndex BuildOutputIndex(IReadOnlyList<OutputMapping> mappings)
    {
        var byRef = new Dictionary<SimRef, List<OutputBinding>>();
        var byDevice = new Dictionary<DeviceIdentity, List<OutputBinding>>();
        var screenByDevice = new Dictionary<DeviceIdentity, List<OutputSink.Screen>>();
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

            switch (mapping.Sink)
            {
                case OutputSink.Indicator indicator:
                {
                    if (!byDevice.TryGetValue(indicator.Device, out var deviceBindings))
                    {
                        deviceBindings = [];
                        byDevice[indicator.Device] = deviceBindings;
                    }

                    deviceBindings.Add(outputBinding);
                    break;
                }
                case OutputSink.Screen screen:
                {
                    if (!screenByDevice.TryGetValue(screen.Device, out var deviceScreens))
                    {
                        deviceScreens = [];
                        screenByDevice[screen.Device] = deviceScreens;
                    }

                    deviceScreens.Add(screen);
                    break;
                }
            }
        }

        var byRefFrozen =
            byRef.ToFrozenDictionary(
                x => x.Key,
                x => x.Value.Distinct().ToArray());
        var byDeviceFrozen = byDevice.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Distinct().ToArray());
        var screenByDeviceFrozen = screenByDevice.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Distinct().ToArray());

        return new OutputIndex(byRefFrozen, byDeviceFrozen, screenByDeviceFrozen, requiredRefs.ToFrozenSet());
    }

    private void HandleScreenDriverAdded(RouterCommand.ScreenDriverAdded driver)
    {
        if (driver.Device is not ITextDisplayOutput screen)
            return;

        var screenRoles = _screenIndex.ScreenRoles.ToDictionary();
        screenRoles[driver.Device.Identity] = screen.Role;

        _screenIndex = _screenIndex with
        {
            ScreenRoles = screenRoles.ToFrozenDictionary(),
        };

        _commands.Writer.TryWrite(new RouterCommand.BindingsUpdated());
    }

    private void HandleAircraftChanged(RouterCommand.AircraftChanged ac)
    {
        if (string.IsNullOrWhiteSpace(ac.Aircraft) || _currentProducer is null)
            return;

        if (!_aircraftScreenAdapterFactory.TryGet(
                _currentProducer.Simulator,
                new SimulatorAircraft(ac.Aircraft),
                out var aircraftAdapter))
            return;

        _screenIndex = _screenIndex with { Adapter = aircraftAdapter };

        _commands.Writer.TryWrite(new RouterCommand.BindingsUpdated());
    }

    private void HandleScreenDriverRemoved(DeviceIdentity identity)
    {
        if (!_screenIndex.ScreenRoles.ContainsKey(identity))
            return;

        var screenRoles = _screenIndex.ScreenRoles.ToDictionary();
        screenRoles.Remove(identity);

        _screenIndex = _screenIndex with { ScreenRoles = screenRoles.ToFrozenDictionary() };
        _commands.Writer.TryWrite(new RouterCommand.BindingsUpdated());
    }

    private void HandleSimulatorChanged(RouterCommand.SimChanged p, CancellationToken cancellationToken)
    {
        if (ReferenceEquals(p.Producer, _currentProducer))
            return;

        _logger.LogInformation("Simulator changed {Producer}", p.Producer);
        _simValueCache.Clear();

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

    private void HandleValuesChanged(SimEvent.ValuesChanged batch)
    {
        var screenIndex = _screenIndex;
        var dirtyRoles = new HashSet<DisplayRole>();
        var dirtyBindings = new HashSet<OutputBinding>();

        foreach (var change in batch.Changes)
        {
            _simValueCache.TrySet(change.Ref, change.Value);
            BufferScreenValue(screenIndex, change, dirtyRoles);

            if (_outputIndex.ByRef.TryGetValue(change.Ref, out var bindings))
                dirtyBindings.UnionWith(bindings);
        }

        foreach (var binding in dirtyBindings)
            Dispatch(binding);

        foreach (var role in dirtyRoles)
            ShowScreen(screenIndex, role);
    }

    private static void BufferScreenValue(
        ScreenIndex screenIndex,
        SimEvent.ValueChanged change,
        HashSet<DisplayRole> dirtyRoles)
    {
        if (!screenIndex.ByRef.TryGetValue(change.Ref, out var roles))
            return;

        foreach (var role in roles)
        {
            if (!screenIndex.ValueBuffers.TryGetValue(role, out var buffer))
                continue;

            buffer[change.Ref] = change.Value;
            dirtyRoles.Add(role);
        }
    }

    private void ShowScreen(ScreenIndex screenIndex, DisplayRole role)
    {
        if (screenIndex.Adapter?.BuildScreen(role, screenIndex.ValueBuffers[role]) is not { } screen)
            return;

        foreach (var (identity, screenRole) in screenIndex.ScreenRoles)
        {
            if (screenRole != role || !_deviceRegistry.TryGet(identity, out var driver) ||
                driver is not ITextDisplayOutput output)
                continue;

            output.Show(screen);
        }
    }

    private void Dispatch(OutputBinding binding)
    {
        if (binding.Mapping.Sources.Count > 1)
        {
            _logger.LogWarning("Binding {Sink} has multiple sources; skipping", binding.Mapping.Sink);
            return; // TODO: Implement transformations
        }

        var change = _simValueCache.TryGet(binding.Mapping.Sources.First().Value, out var value);
        if (!change || value is null)
            return;

        if (value.Value is not SimValue.Number valueNumber)
        {
            _logger.LogWarning("Binding {Sink} skipped: value is not a number", binding.Mapping.Sink);
            return;
        }

        if (binding.Mapping.Sink is OutputSink.Indicator indicator
            && _deviceRegistry.TryGet(indicator.Device, out var driver)
            && driver is IIndicatorOutput output)
            output.Set(indicator.Output, (int)valueNumber.Value);
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

                    _commands.Writer.TryWrite(new RouterCommand.AircraftChanged(ac.AircraftId));
                    AircraftChanged?.Invoke(this, ac.AircraftId);
                    break;
                case SimEvent.ValuesChanged batch:
                    HandleValuesChanged(batch);
                    break;
            }
        }
    }

    private void DeviceRegistryOnDriverRemoved(object? sender, DriverRemovedEventArgs e) =>
        _commands.Writer.TryWrite(new RouterCommand.ScreenDriverRemoved(e.Identity));

    private void DeviceRegistryOnDriverAdded(object? sender, DriverAddedEventArgs e)
    {
        if (e.Driver is not ITextDisplayOutput)
            return;

        _commands.Writer.TryWrite(new RouterCommand.ScreenDriverAdded(e.Driver));
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

        internal sealed record ScreenDriverAdded(IDeviceDriver Device) : RouterCommand;

        internal sealed record ScreenDriverRemoved(DeviceIdentity Identity) : RouterCommand;

        internal sealed record AircraftChanged(string? Aircraft) : RouterCommand;

        internal sealed record BindingsUpdated : RouterCommand;
    }
}