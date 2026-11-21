using System.Collections.Frozen;
using System.Threading.Channels;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
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
    private readonly InputBindingFactory _inputBindingFactory;

    private ISimProducer? _currentProducer;
    private CancellationTokenSource? _inputEventsCancellationTokenSource;
    private Task? _inputEventsTask;

    private volatile bool _isSimReady;
    private volatile InputIndex _inputIndex = InputIndex.Empty;

    public InputDispatcher(
        IActiveSim activeSim,
        ISubscriptionAggregator subscriptionAggregator,
        IExpressionCompiler expressionCompiler,
        ISimValueCache simValueCache,
        ILogger<InputDispatcher> logger)
    {
        _activeSim = activeSim ?? throw new ArgumentNullException(nameof(activeSim));
        _subscriptionAggregator =
            subscriptionAggregator ?? throw new ArgumentNullException(nameof(subscriptionAggregator));
        _simValueCache = simValueCache ?? throw new ArgumentNullException(nameof(simValueCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _inputBindingFactory = new InputBindingFactory(expressionCompiler);
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
        var byDevice = bindings.ToFrozenDictionary(
            b => (b.Device, b.Control),
            _inputBindingFactory.Create);

        var requiredRefs = byDevice.Values
            .SelectMany(b => b.RequiredRefs)
            .Where(r => r is SimRef.Plain)
            .ToFrozenSet();

        var inputIndex = new InputIndex(byDevice, requiredRefs);

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
        InputBinding binding,
        CancellationToken cancellationToken)
    {
        if (!binding.Events.TryGetValue(inputEvent.Kind.ToString(), out var eventBinding))
            return;

        Dictionary<string, SimValue?> sources = [];

        foreach (var (key, simRef) in binding.Sources)
        {
            if (!_simValueCache.TryGet(simRef, out var value))
                continue;

            sources[key] = value?.Value;
        }

        if (eventBinding.Guard is { } guard)
        {
            var outputValue = guard.Apply(sources);

            if (outputValue is OutputValue.Boolean { Value: false }) return;
        }

        foreach (var action in eventBinding.Actions)
        {
            try
            {
                //await producer.SendCommandAsync(simCommand, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error sending command for {Identity} {Control} {Kind}",
                    inputEvent.Identity, inputEvent.Control.Id, inputEvent.Kind);
            }
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

        internal sealed record DeviceRemoved : DispatcherCommand;
    }
}