using System.Threading.Channels;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing;

/*
 * TODO: Logic to handle
 * 1. Simulator connect/disconnects
 *  a. Wipe all state. Wait for next connection
 * 2. Profile updates
 *  a. New ref added - update subscriptions, append reference
 *  b. New ref removed - update subscriptions, remove reference
 * 3. Profile changes
 *  a. New profile loaded - wipe subscriptions. Rebuild references
 *  b. Profile unloaded - wipe subscriptions
 * 4. Device changes (added, removed)
 *  a. Added - replay events for device
 *  b. Removed - removed subscriptions for that particular device
 */
public sealed class SimValueRouter : ISimValueRouter, ISimValueCache
{
    private readonly IActiveSim _activeSim;
    private readonly IDeviceRegistry _deviceRegistry;

    private readonly Channel<ISimProducer> _producerChannel = Channel.CreateBounded<ISimProducer>(
        new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = true,
        });

    private readonly Channel<RouterCommand> _commands = Channel.CreateUnbounded<RouterCommand>(
        new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
        });

    private ISimProducer? _currentProducer;

    public SimValueRouter(
        IActiveSim activeSim,
        IDeviceRegistry deviceRegistry)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));

        _activeSim.Changing += ActiveSimOnChanging;
        _activeSim.Changed += ActiveSimOnChanged;
        _deviceRegistry.DriverAdded += DeviceRegistryOnDriverAdded;
    }

    public event EventHandler<string>? AircraftChanged;

    public void SetBindings(IReadOnlyList<OutputMapping> bindings) => throw new NotImplementedException();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_activeSim.Current is { } simProducer)
            _producerChannel.Writer.TryWrite(simProducer);

        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var producer in _producerChannel.Reader.ReadAllAsync(cancellationToken))
            {
                _currentProducer = producer;
            }
        }
    }

    public bool TryGet(SimRef reference, out SimValue? value) => throw new NotImplementedException();

    public IReadOnlyDictionary<SimRef, (SimValue Value, long UpdatedAt)> GetSnapshot() => throw new NotImplementedException();

    private void DeviceRegistryOnDriverAdded(object? sender, DriverAddedEventArgs e)
        => _commands.Writer.TryWrite(new RouterCommand.Replay(e.Identity));

    private void ActiveSimOnChanged(object? sender, ActiveSimChangedEventArgs e)
    {
        if (e.Current is null) return;

        _producerChannel.Writer.TryWrite(e.Current);
    }

    private void ActiveSimOnChanging(object? sender, ActiveSimChangingEventArgs e)
    {
        // Simulator connection changing
        // Gone or being changed. TODO: handle the cleanup
    }

    internal closed record RouterCommand
    {
        internal sealed record Replay(DeviceIdentity Identity) : RouterCommand;
    }
}