using System.Threading.Channels;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing;

public sealed class InputDispatcher :
    IInputSink,
    IInputMonitor,
    IInputDispatcher
{
    private readonly Channel<DeviceInputEvent> _queue =
        Channel.CreateUnbounded<DeviceInputEvent>(new UnboundedChannelOptions { SingleReader = true });

    private readonly Channel<DeviceInputEvent> _recent =
        Channel.CreateBounded<DeviceInputEvent>(new BoundedChannelOptions(256));

    private readonly IActiveSim _activeSim;
    private volatile bool _isSimReady;

    public IAsyncEnumerable<DeviceInputEvent> Recent => _recent.Reader.ReadAllAsync();

    public InputDispatcher(
        IActiveSim activeSim)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _activeSim.Changing += ActiveSimOnChanging;
        _activeSim.Changed += ActiveSimOnChanged;
    }

    private void ActiveSimOnChanging(object? sender, ActiveSimChanging e)
    {
        if (e.Current is { } outgoing)
        {
            outgoing.Ready -= SimulatorReady;
        }
        
        _isSimReady = false;
    }

    private void ActiveSimOnChanged(object? sender, ActiveSimChanged e)
    {
        if (e.Current is { } incoming)
        {
            incoming.Ready += SimulatorReady;
        }
    }

    private void SimulatorReady(object? sender, SimReady e)
    {
        _isSimReady = true;
    }

    public void Post(DeviceInputEvent inputEvent)
    {
        _queue.Writer.TryWrite(inputEvent);
        _recent.Writer.TryWrite(inputEvent);
    }

    /*
     * TODO
     * 1. Handle:
     *    Null
     *    Unloaded Profile
     *    Disconnected Sim
     * 2. Commands:
     *    Writing Values
     *    Invoking Commands
     */
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await foreach (var inputEvent in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            if (!_isSimReady || _activeSim.Current is null)
                continue;

            var simRef = new SimRef("laminar/B738/pfd/dh_pilot_dn");
            
            // TODO: Hardcoded for now. Should come from profile bindings
            SimCommand? simCommand = inputEvent.Kind switch
            {
                InputEventKind.Pressed => new SimCommand.Begin(simRef),
                InputEventKind.Released => new SimCommand.End(simRef),
                _ => null,
            };
            
            if (simCommand is null)
                continue;
            
            await _activeSim.Current.SendCommandAsync(simCommand, cancellationToken);
        }
    }
}