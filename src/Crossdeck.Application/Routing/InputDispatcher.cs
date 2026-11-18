using System.Collections.Frozen;
using System.Threading.Channels;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
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
        Channel.CreateBounded<DeviceInputEvent>(new BoundedChannelOptions(256)
            { FullMode = BoundedChannelFullMode.DropOldest });

    private readonly Channel<DispatcherCommand> _commands =
        Channel.CreateUnbounded<DispatcherCommand>(new UnboundedChannelOptions { SingleReader = true });

    private readonly IActiveSim _activeSim;

    private ISimProducer? _currentProducer;
    private CancellationTokenSource? _inputEventsCancellationTokenSource;
    private Task? _inputEventsTask;

    private volatile bool _isSimReady;
    private volatile InputIndex _inputIndex = InputIndex.Empty;

    public InputDispatcher(
        IActiveSim activeSim)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _activeSim.Changing += ActiveSimOnChanging;
        _activeSim.Changed += ActiveSimOnChanged;
    }

    public IAsyncEnumerable<DeviceInputEvent> Recent => _recent.Reader.ReadAllAsync();

    public void Post(DeviceInputEvent inputEvent)
    {
        _queue.Writer.TryWrite(inputEvent);
        _recent.Writer.TryWrite(inputEvent);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await foreach (var command in _commands.Reader.ReadAllAsync(cancellationToken))
        {
            switch (command)
            {
                case DispatcherCommand.SimChanging:
                    await HandleSimulatorChanging();
                    break;
                case DispatcherCommand.SimChanged p:
                    HandleSimulatorChanged(p.Producer, cancellationToken);
                    break;
                case DispatcherCommand.SimReady:
                    _isSimReady = true;
                    break;
                case DispatcherCommand.ProfileBindingUpdate b:
                    break;
                case DispatcherCommand.DeviceRemoved d:
                    break;
                default:
                    break;
            }
        }
    }

    public void SetBindings(IReadOnlyList<InputMapping> bindings)
    {
        var byDevice = new Dictionary<(DeviceIdentity, InputId), InputMapping>();
        var requiredRefs = new HashSet<SimRef>();
        foreach (var binding in bindings)
        {
            byDevice[(binding.Device, binding.Control)] = binding;

            foreach (var inputAction in binding.Actions.Values.SelectMany(x => x))
            {
                if (inputAction is not InputAction.Set set)
                    continue;

                _ = requiredRefs.Add(set.Ref);
            }
        }

        var inputIndex = new InputIndex(
            byDevice.ToFrozenDictionary(),
            requiredRefs.ToFrozenSet());

        _inputIndex = inputIndex;

        _commands.Writer.TryWrite(new DispatcherCommand.ProfileBindingUpdate(_inputIndex));
    }

    private void HandleSimulatorChanged(ISimProducer producer, CancellationToken cancellationToken)
    {
        if (ReferenceEquals(producer, _currentProducer))
            return;

        _currentProducer = producer;
        _currentProducer.Ready += CurrentProducerOnReady;
        _inputEventsCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _inputEventsTask = RunInputEventsLoopAsync(_currentProducer!, _inputEventsCancellationTokenSource!.Token);
    }

    private async Task HandleSimulatorChanging()
    {
        if (_currentProducer is null)
            return;

        _isSimReady = false;
        _currentProducer.Ready -= CurrentProducerOnReady;

        if (_inputEventsCancellationTokenSource is null || _inputEventsTask is null)
            return;

        await _inputEventsCancellationTokenSource!.CancelAsync();
        await _inputEventsTask;

        _inputEventsCancellationTokenSource = null;
        _inputEventsTask = null;
        _currentProducer = null;
    }

    private async Task RunInputEventsLoopAsync(
        ISimProducer producer,
        CancellationToken cancellationToken)
    {
        await foreach (var inputEvent in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            if (!_isSimReady
                || !_inputIndex.ByDevice.TryGetValue((inputEvent.Identity, inputEvent.Control.Id), out var binding))
                continue;

            await DispatchInputEventAsync(producer, inputEvent, binding, cancellationToken);
        }
    }

    private async Task DispatchInputEventAsync(
        ISimProducer producer,
        DeviceInputEvent inputEvent,
        InputMapping binding,
        CancellationToken cancellationToken)
    {
        if (!binding.Actions.TryGetValue(inputEvent.Kind.ToString(), out var actions))
            return;

        foreach (var action in actions)
        {
            var simCommand = action.ToSimCommand();

            await producer.SendCommandAsync(simCommand, cancellationToken);
        }
    }

    private void CurrentProducerOnReady(object? sender, SimReady e)
        => _commands.Writer.TryWrite(new DispatcherCommand.SimReady());

    private void ActiveSimOnChanging(object? sender, ActiveSimChangingEventArgs e)
        => _commands.Writer.TryWrite(new DispatcherCommand.SimChanging());

    private void ActiveSimOnChanged(object? sender, ActiveSimChangedEventArgs e)
        => _commands.Writer.TryWrite(new DispatcherCommand.SimChanged(e.Current));

    internal closed record DispatcherCommand
    {
        internal sealed record SimChanging : DispatcherCommand;

        internal sealed record SimChanged(ISimProducer? Producer) : DispatcherCommand;

        internal sealed record SimReady : DispatcherCommand;

        internal sealed record ProfileBindingUpdate(InputIndex Index) : DispatcherCommand;

        internal sealed record DeviceRemoved : DispatcherCommand;
    }
}