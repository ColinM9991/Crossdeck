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
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Routing.Input;

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
    private readonly ISubscriptionAggregator _subscriptionAggregator;
    private readonly ISimValueCache _simValueCache;
    private readonly ILogger<InputDispatcher> _logger;

    private ISimProducer? _currentProducer;
    private CancellationTokenSource? _inputEventsCancellationTokenSource;
    private Task? _inputEventsTask;

    private volatile bool _isSimReady;
    private volatile InputIndex _inputIndex = InputIndex.Empty;

    public InputDispatcher(
        IActiveSim activeSim,
        ISubscriptionAggregator subscriptionAggregator,
        ISimValueCache simValueCache,
        ILogger<InputDispatcher> logger)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _subscriptionAggregator =
            subscriptionAggregator ?? throw new ArgumentNullException(nameof(subscriptionAggregator));
        _simValueCache = simValueCache ?? throw new ArgumentNullException(nameof(simValueCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        try
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
                        _logger.LogInformation("Simulator ready");
                        break;
                    case DispatcherCommand.DeviceRemoved d:
                        break;
                    default:
                        break;
                }
            }
        }
        finally
        {
            await StopInputEventsLoopAsync();
        }
    }

    public void SetBindings(IReadOnlyList<InputMapping> bindings)
    {
        var byDevice = new Dictionary<(DeviceIdentity, InputId), InputMapping>();
        var requiredRefs = new HashSet<SimRef>();
        foreach (var binding in bindings)
        {
            byDevice[(binding.Device, binding.Control)] = binding;

            // foreach (var inputAction in binding.Actions.Values.SelectMany(x => x))
            // {
            //     if (inputAction is not InputAction.Step set)
            //         continue;
            //
            //     _ = requiredRefs.Add(set.Ref);
            // }
        }

        var inputIndex = new InputIndex(
            byDevice.ToFrozenDictionary(),
            requiredRefs.ToFrozenSet());

        _inputIndex = inputIndex;
        _logger.LogInformation("Input bindings set {Count}", bindings.Count);
        _subscriptionAggregator.SetInputRefs(inputIndex.RequiredRefs);
    }

    private void HandleSimulatorChanged(ISimProducer? producer, CancellationToken cancellationToken)
    {
        if (producer is null)
        {
            _logger.LogInformation("Simulator disconnected");
            _currentProducer = null;
            _isSimReady = false;
            return;
        }

        if (ReferenceEquals(producer, _currentProducer))
            return;

        _logger.LogInformation("Simulator changed");
        _currentProducer = producer;
        producer.Ready += CurrentProducerOnReady;

        var inputEventsCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _inputEventsCancellationTokenSource = inputEventsCancellationTokenSource;
        _inputEventsTask = RunInputEventsLoopAsync(producer, inputEventsCancellationTokenSource.Token);
    }

    private async Task HandleSimulatorChanging()
    {
        if (_currentProducer is null && _inputEventsTask is null)
            return;

        _logger.LogInformation("Simulator changing");

        _isSimReady = false;
        await StopInputEventsLoopAsync();
    }

    private async Task StopInputEventsLoopAsync()
    {
        if (_inputEventsCancellationTokenSource is not null)
        {
            var cts = _inputEventsCancellationTokenSource;
            _inputEventsCancellationTokenSource = null;

            await cts.CancelAsync();

            if (_inputEventsTask is not null)
            {
                var task = _inputEventsTask;
                _inputEventsTask = null;

                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Input events loop faulted");
                }
            }

            cts.Dispose();
        }

        if (_currentProducer is not null)
        {
            _currentProducer.Ready -= CurrentProducerOnReady;
            _currentProducer = null;
        }

        _isSimReady = false;
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

            _logger.LogDebug("Dispatching input event {Identity} {Control} {Kind}",
                inputEvent.Identity, inputEvent.Control.Id, inputEvent.Kind);
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

        // foreach (var action in actions)
        // {
        //     if (action is InputAction.Step step)
        //     {
        //         await ExecuteStepAsync(producer, step, cancellationToken);
        //         continue;
        //     }
        //
        //     var simCommand = action.ToSimCommand();
        //     if (simCommand is null)
        //         continue;
        //
        //     try
        //     {
        //         await producer.SendCommandAsync(simCommand, cancellationToken);
        //     }
        //     catch (Exception ex) when (ex is not OperationCanceledException)
        //     {
        //         _logger.LogError(ex, "Error sending command for {Identity} {Control} {Kind}",
        //             inputEvent.Identity, inputEvent.Control.Id, inputEvent.Kind);
        //     }
        // }
    }

    private async Task ExecuteStepAsync(
        ISimProducer producer,
        InputAction.Step step,
        CancellationToken cancellationToken)
    {
        var baseValue = double.NaN;

        if (_simValueCache.TryGet(step.Ref, out var entry)
            && entry?.Value is SimValue.Number number)
        {
            baseValue = number.Value;
        }

        if (double.IsNaN(baseValue))
        {
            _logger.LogInformation("Step on {Ref} skipped: no current value", step.Ref);
            return;
        }

        var result = Math.Clamp(baseValue + step.Amount, step.Min, step.Max);

        await producer.SendCommandAsync(
            new SimCommand.Set(step.Ref, new SimValue.Number(result)),
            cancellationToken);
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

        internal sealed record DeviceRemoved : DispatcherCommand;
    }
}