using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Crossdeck.Core;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Crossdeck.Sim.XPlane.Messages;
using Crossdeck.Sim.XPlane.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Sim.XPlane;

/// <summary>
/// Implements <see cref="ISimProducer"/> using the X-Plane API.
/// </summary>
/// <remarks>
/// Connects via the X-Plane WebSocket API and uses <see cref="IXPlaneApiService"/> for HTTP operations.
/// </remarks>
public sealed class XPlaneApiSimProducer : ISimProducer
{
    private static readonly SimRef AircraftRef = new(XPlaneConstants.AircraftRefKey);

    /// <summary>
    /// Internal channel for managing and distributing simulation events.
    /// Used to handle communication of <see cref="SimEvent"/> instances
    /// between producers and consumers within the X-Plane simulation API integration.
    /// </summary>
    private readonly Channel<SimEvent> _eventsChannel = Channel.CreateBounded<SimEvent>(new BoundedChannelOptions(512)
    {
        SingleReader = true,
        SingleWriter = true
    });

    /// <summary>
    /// Internal signal for notifying of changes in subscription states
    /// within the X-Plane simulation API integration.
    /// </summary>
    private readonly Signal _subscriptionsChangedSignal = new();

    private readonly IXPlaneApiService _api;
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger<XPlaneApiSimProducer> _logger;

    private int _eventsConsumed;
    private int _requestId;

    private volatile bool _isReady;

    private SimStatus _simStatus = new SimStatus.Disconnected();
    private SessionState? _sessionState;

    private string? _aircraft;

    private IReadOnlyList<SimRef> _subscriptions = new List<SimRef>();

    /// <summary>
    /// Provides an implementation of <see cref="ISimProducer"/> using the X-Plane API for simulating events and commands.
    /// </summary>
    /// <remarks>
    /// Establishes connectivity to the X-Plane simulator via its WebSocket API.
    /// It manages event consumption and facilitates communication with the simulator.
    /// </remarks>
    public XPlaneApiSimProducer(
        IXPlaneApiService api,
        XPlaneOptions options,
        ILogger<XPlaneApiSimProducer> logger)
    {
        _api = api ??
               throw new ArgumentNullException(nameof(api));
        _host = options.Host ?? throw new ArgumentNullException(nameof(options.Host));
        _port = options.Port ?? throw new ArgumentNullException(nameof(options.Port));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private Uri WsUri => new($"ws://{_host}:{_port}{XPlaneConstants.WebSocketPath}");

    public event EventHandler<SimReady>? Ready;

    /// <summary>
    /// Gets an asynchronous stream of <see cref="SimEvent"/>s.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests, allowing the operation to be interrupted gracefully.</param>
    /// <returns>An asynchronous stream of simulation events from the internal event channel.</returns>
    /// <remarks>
    /// The stream can only be consumed by a single reader at a time.
    /// A second call while the first enumeration is still active throws an <see cref="InvalidOperationException"/> on its first <c>MoveNextAsync</c>.
    /// The single-reader flag is cleared when the enumeration is disposed, so the stream can be re-consumed once the first reader has finished.
    /// The token can also be supplied through <c>IAsyncEnumerable.GetAsyncEnumerator</c>, in which case it is combined with the token passed to this method and cancelling either one cancels the enumeration.
    /// </remarks>
    public async IAsyncEnumerable<SimEvent> GetEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _eventsConsumed, 1) != 0)
            throw new InvalidOperationException("Events can only be consumed by a single reader");

        try
        {
            await foreach (var e in _eventsChannel.Reader.ReadAllAsync(cancellationToken)) yield return e;
        }
        finally
        {
            Interlocked.Exchange(ref _eventsConsumed, 0);
        }
    }

    /// <summary>
    /// Starts the continuous operation of the simulation producer.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests, allowing the operation to be interrupted gracefully.</param>
    /// <returns>A task that represents the asynchronous operation of running the simulation producer.</returns>
    /// <remarks>
    /// This method runs a loop to maintain the connection to the simulator and retrieve simulation events
    /// until the cancellation is requested. Upon completion, it publishes a "gone" status and completes
    /// the events channel.
    /// </remarks>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await RunSessionAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            SetStatus(new SimStatus.Gone());
            _eventsChannel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Updates the active set of subscriptions for simulation events.
    /// </summary>
    /// <param name="subscriptions">The collection of <see cref="SimRef"/> representing the desired simulation data subscriptions.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation of updating the subscriptions.</returns>
    /// <remarks>
    /// The method updates the simulator with the specified set of subscriptions
    /// and broadcasts a notification to indicate that the subscriptions have changed.
    /// </remarks>
    public async Task SetSubscriptionsAsync(IReadOnlySet<SimRef> subscriptions, CancellationToken cancellationToken)
    {
        _subscriptions = [.. subscriptions];
        await _subscriptionsChangedSignal.SetAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a command to the simulator.
    /// </summary>
    /// <param name="command">The simulator command to be sent.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the result of the command execution.</returns>
    /// <remarks>
    /// The 'command' in this context is not an X-Plane command.
    /// 'Command' is a request for sending data to the simulator.
    /// This can be a command or a dataref for X-Plane.
    /// </remarks>
    public async Task<CommandResult> SendCommandAsync(SimCommand command, CancellationToken cancellationToken)
    {
        var session = _sessionState;
        if (session is null) return new CommandResult.NoConnection();
        if (!_isReady) return new CommandResult.NotReady();

        return command switch
        {
            SimCommand.Trigger trigger => await SendCommandAsync(session, trigger.Action, true, 0, cancellationToken),
            SimCommand.Begin begin => await SendCommandAsync(session, begin.Action, true, null, cancellationToken),
            SimCommand.End end => await SendCommandAsync(session, end.Action, false, null, cancellationToken),
            SimCommand.Set set => await SendDataRefAsync(session, set.Target, set.Value, cancellationToken),
            _ => Unsupported(command)
        };
    }

    /// <summary>
    /// Releases unmanaged resources asynchronously.
    /// </summary>
    /// <returns>A value task that represents the asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        _eventsChannel.Writer.TryComplete();
        _sessionState?.Outgoing.Writer.TryComplete();

        return ValueTask.CompletedTask;
    }

    private static async Task RunChildLoopAsync(Task loop, CancellationTokenSource cts,
        ILogger logger)
    {
        try
        {
            await loop;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "X-Plane session loop faulted");
            await cts.CancelAsync();
        }
    }

    /// <summary>
    /// Sends outgoing messages from the session's outgoing channel to the WebSocket.
    /// </summary>
    /// <param name="ws">The WebSocket connection to send messages through.</param>
    /// <param name="session">The session state containing the outgoing message channel.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous send loop operation.</returns>
    private static async Task SendLoopAsync(ClientWebSocket ws, SessionState session,
        CancellationToken cancellationToken)
    {
        await foreach (var message in session.Outgoing.Reader.ReadAllAsync(cancellationToken))
        {
            await ws.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    private static SimValue ToSimValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => new SimValue.String(
            Encoding.UTF8.GetString(Convert.FromBase64String(value.GetString()!))),
        JsonValueKind.Number => new SimValue.Number(value.GetDouble()),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value.ValueKind.ToString())
    };

    /// <summary>
    /// Publishes a simulation event to the internal event channel for subscribers to process.
    /// </summary>
    /// <param name="e">The simulation event to be published.</param>
    private void Publish(SimEvent e) => _eventsChannel.Writer.TryWrite(e);

    /// <summary>
    /// Updates the current simulator status.
    /// </summary>
    /// <param name="status">The new simulator status to be set.</param>
    private void SetStatus(SimStatus status)
    {
        _simStatus = status;
    }

    private async Task<CommandResult> SendCommandAsync(
        SessionState session,
        SimRef action,
        bool isActive,
        int? duration,
        CancellationToken cancellationToken)
    {
        var commandId = await ResolveCommandIdAsync(action.Key, session.CommandCache, cancellationToken);
        if (commandId is null)
        {
            _logger.LogWarning("X-Plane command {Command} could not be resolved", action);
            return new CommandResult.Failure();
        }

        var command = new CommandSetIsActive
        {
            Id = commandId.Value,
            IsActive = isActive,
            Duration = duration
        };

        await EnqueueAsync(session, XPlaneConstants.CommandSetIsActive,
            new CommandSetIsActiveParams { Commands = [command] },
            cancellationToken);

        return new CommandResult.Success();
    }

    private async Task<CommandResult> SendDataRefAsync(
        SessionState session,
        SimRef simRef,
        SimValue simValue,
        CancellationToken cancellationToken)
    {
        if (!TrySplitDataRefKey(simRef.Key, out var dataRef, out var index))
        {
            _logger.LogWarning("X-Plane dataref {DataRef} has an unparseable index; not sent", simRef);
            return new CommandResult.Failure();
        }

        var dataRefId = await ResolveDataRefIdAsync(dataRef, session.DataRefCache, cancellationToken);
        if (dataRefId is null)
        {
            _logger.LogWarning("X-Plane dataref {DataRef} could not be resolved", simRef);
            return new CommandResult.Failure();
        }

        object value = simValue switch
        {
            SimValue.Number number => number.Value,
            SimValue.String str => Convert.ToBase64String(Encoding.UTF8.GetBytes(str.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(simValue), simValue, null)
        };

        await EnqueueAsync(session, XPlaneConstants.DatarefSetValues,
            new DatarefSetValueParams
                { Datarefs = [new DatarefSetValue { Id = dataRefId.Value, Value = value, Index = index }] },
            cancellationToken);

        return new CommandResult.Success();
    }

    /// <summary>
    /// Splits a dataref key into its registered name and element index, for example
    /// "path/to/ref[10]" into ("path/to/ref", 10).
    /// </summary>
    private static bool TrySplitDataRefKey(string key, out string name, out int? index)
    {
        name = key;
        index = null;

        var open = key.IndexOf('[');
        if (open == -1)
            return true;

        var close = key.LastIndexOf(']');
        if (close <= open)
            return false;

        if (!int.TryParse(key.AsSpan(open + 1, close - open - 1), out var elementIndex))
            return false;

        name = key[..open];
        index = elementIndex;
        return true;
    }

    private CommandResult Unsupported(SimCommand command)
    {
        _logger.LogWarning("Simulator command type {CommandType} is not supported", command.GetType().Name);
        return new CommandResult.Unsupported();
    }

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        _isReady = false;
        SetStatus(new SimStatus.Disconnected());

        using var ws = new ClientWebSocket();
        try
        {
            await ws.ConnectAsync(WsUri, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "X-Plane connection to {Host}:{Port} failed", _host, _port);
            throw;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _logger.LogInformation("X-Plane session connected to {Host}:{Port}", _host, _port);
        SetStatus(new SimStatus.Connected());

        await WaitForInitializationAsync(cancellationToken);
        await _subscriptionsChangedSignal.SetAsync(cancellationToken);

        var sessionState = new SessionState();
        _sessionState = sessionState;
        _aircraft = null;

        var sendLoop = RunChildLoopAsync(SendLoopAsync(ws, sessionState, cts.Token), cts, _logger);
        var subscriptionLoop = RunChildLoopAsync(SubscriptionLoopAsync(sessionState, cts.Token), cts, _logger);

        try
        {
            await ReceiveLoopAsync(ws, sessionState, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Session teardown: the linked token was cancelled.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "X-Plane receive loop faulted");
        }
        finally
        {
            await cts.CancelAsync();
            await Task.WhenAll(sendLoop, subscriptionLoop);
        }

        _logger.LogInformation("X-Plane session on {Host}:{Port} ended", _host, _port);
    }

    /// <summary>
    /// Receives incoming messages from the WebSocket and dispatches them.
    /// </summary>
    /// <param name="ws">The WebSocket connection to receive messages from.</param>
    /// <param name="session">The session state that manages incoming messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous receive loop operation.</returns>
    private async Task ReceiveLoopAsync(ClientWebSocket ws, SessionState session, CancellationToken cancellationToken)
    {
        var buffer = new ArrayBufferWriter<byte>(16 * 1024);
        while (ws.State is WebSocketState.Open)
        {
            buffer.Clear();
            ValueWebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(buffer.GetMemory(8 * 1024), cancellationToken);
                buffer.Advance(result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            Dispatch(buffer.WrittenMemory, session);
        }
    }

    /// <summary>
    /// Manages subscription updates in response to changes in the session state and the set of active subscriptions.
    /// </summary>
    /// <param name="session">The session state that tracks active subscriptions and their identifiers.</param>
    /// <param name="cancellationToken">The token used to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous subscription loop operation.</returns>
    private async Task SubscriptionLoopAsync(SessionState session, CancellationToken cancellationToken)
    {
        await foreach (var _ in _subscriptionsChangedSignal.WaitAsync(cancellationToken))
        {
            var allSubscriptions = new HashSet<SimRef>(_subscriptions) { AircraftRef };
            var existingSubscriptions = new HashSet<SimRef>(session.IdToRef.Values);

            var toAddKeys = allSubscriptions
                .Except(existingSubscriptions)
                .Distinct()
                .ToHashSet();

            var toRemove = existingSubscriptions.Except(allSubscriptions).ToHashSet();

            var toAdd = new List<DatarefSubscription>();
            var toRemoveIds = new List<long>();

            foreach (var add in toAddKeys)
            {
                if (!TrySplitDataRefKey(add.Key, out var name, out var index))
                {
                    _logger.LogWarning("X-Plane dataref {DataRef} has an unparseable index; not subscribed", add.Key);
                    continue;
                }

                var id = await ResolveDataRefIdAsync(name, session.DataRefCache, cancellationToken);
                if (id is null)
                {
                    _logger.LogWarning("X-Plane dataref {DataRef} could not be subscribed; not found", add.Key);
                    continue;
                }

                if (session.IdToRef.TryGetValue(id.Value, out var existing) && existing != add)
                {
                    _logger.LogWarning("X-Plane dataref id {Id} already subscribed as {Existing}; {Ref} skipped",
                        id.Value, existing, add);
                    continue;
                }

                session.IdToRef[id.Value] = add;
                session.RefToId[add] = id.Value;
                session.RefToIndex[add] = index;
                toAdd.Add(new DatarefSubscription { Id = id.Value, Index = index });
            }

            foreach (var remove in toRemove)
            {
                if (!session.RefToId.TryGetValue(remove, out var id)) continue;

                session.RefToIndex.TryRemove(remove, out var _);
                toRemoveIds.Add(id);
            }

            if (toAdd.Count > 0)
            {
                await EnqueueAsync(session, XPlaneConstants.DatarefSubscribeValues,
                    new DatarefSubscriptionParams
                    {
                        Datarefs = toAdd
                    },
                    cancellationToken);
            }

            if (toRemoveIds.Count <= 0) continue;
            {
                await EnqueueAsync(session, XPlaneConstants.DatarefUnsubscribeValues,
                    new DatarefSubscriptionParams
                    {
                        Datarefs = toRemoveIds.Select(x => new DatarefSubscription { Id = x })
                    },
                    cancellationToken);
                foreach (var id in toRemoveIds)
                {
                    session.IdToRef.TryRemove(id, out var _);
                }
            }
        }
    }

    private Task EnqueueAsync(SessionState session, string type, object? parameters,
        CancellationToken cancellationToken)
    {
        var message = new ClientMessage
        {
            ReqId = Interlocked.Increment(ref _requestId),
            Type = type,
            Params = parameters
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
        return session.Outgoing.Writer.WriteAsync(bytes, cancellationToken).AsTask();
    }

    private async Task<long?> ResolveDataRefIdAsync(
        string key,
        ConcurrentDictionary<string, long> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out var cachedId))
        {
            return cachedId;
        }

        var response = await _api.GetDatarefsAsync(key, null, null, null, cancellationToken);
        var dataref = response?.Data.FirstOrDefault();
        if (dataref is null)
        {
            return null;
        }

        cache.TryAdd(key, dataref.Id);
        return dataref.Id;
    }

    private async Task<long?> ResolveCommandIdAsync(
        string key,
        ConcurrentDictionary<string, long> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out var cachedId))
        {
            return cachedId;
        }

        var response = await _api.GetCommandsAsync(key, null, null, null, cancellationToken);
        var command = response?.Data.FirstOrDefault();
        if (command is null)
        {
            return null;
        }

        cache.TryAdd(key, command.Id);
        return command.Id;
    }

    private void Dispatch(ReadOnlyMemory<byte> data, SessionState session)
    {
        var json = JsonDocument.Parse(data);
        var root = json.RootElement;
        var type = root.GetProperty("type").GetString();

        switch (type)
        {
            case XPlaneConstants.DatarefUpdateValues:
                foreach (var entry in root.GetProperty("data").EnumerateObject())
                {
                    if (!long.TryParse(entry.Name, out var id) ||
                        !session.IdToRef.TryGetValue(id, out var simRef))
                    {
                        continue;
                    }

                    if (simRef == AircraftRef)
                    {
                        PublishAircraftIfChanged(ToSimValue(entry.Value));
                        continue;
                    }

                    var element = entry.Value;
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        var elementIndex = session.RefToIndex.GetValueOrDefault(simRef);
                        if (elementIndex is null)
                        {
                            if (session.WholeArrayWarned.Add(id))
                                _logger.LogWarning("X-Plane dataref id {Id} ({Ref}) is an array subscribed without an index; values skipped", id, simRef);
                            continue;
                        }

                        var values = element.EnumerateArray();
                        if (!values.MoveNext())
                            continue;

                        element = values.Current;
                    }

                    Publish(new SimEvent.ValueChanged(simRef, ToSimValue(element)));
                }

                break;
            case XPlaneConstants.ResultMessage:
            {
                // Handle errors
                break;
            }
        }
    }

    private void PublishAircraftIfChanged(SimValue value)
    {
        var path = value switch
        {
            SimValue.String t => t.Value,
            _ => null
        };

        if (path is null || path == _aircraft) return;

        _aircraft = path;
        Publish(new SimEvent.AircraftChanged(path));
    }

    /// <summary>
    /// Waits for the X-Plane simulator initialization to complete by polling the data reference count.
    /// </summary>
    /// <remarks>
    /// Continuously checks the simulator's data reference count until it becomes non-zero,
    /// indicating that the simulator has completed its initialization process.
    /// This is required to ensure the producer can properly communicate with the simulator.
    /// </remarks>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    private async Task WaitForInitializationAsync(CancellationToken cancellationToken)
    {
        var lastCount = -1;
        while (true)
        {
            try
            {
                var countResponse = await _api.GetDatarefCountAsync(cancellationToken);

                if (countResponse is { Data: > 0 })
                {
                    if (countResponse.Data == lastCount)
                    {
                        _isReady = true;
                        _logger.LogInformation("X-Plane session ready");
                        Ready?.Invoke(this, new SimReady());
                        SetStatus(new SimStatus.Ready());
                        return;
                    }

                    lastCount = countResponse.Data;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private sealed class SessionState
    {
        /// <summary>
        /// A queue for the outgoing messages
        /// </summary>
        public readonly Channel<byte[]> Outgoing =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });

        public readonly ConcurrentDictionary<long, SimRef> IdToRef = new();

        public readonly ConcurrentDictionary<SimRef, long> RefToId = new();

        public readonly ConcurrentDictionary<SimRef, int?> RefToIndex = new();

        public readonly HashSet<long> WholeArrayWarned = new();

        public readonly ConcurrentDictionary<string, long> DataRefCache = new();

        public readonly ConcurrentDictionary<string, long> CommandCache = new();
    }
}