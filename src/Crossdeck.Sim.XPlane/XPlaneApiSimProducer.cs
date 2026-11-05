using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Crossdeck.Core;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Simulators.Models;
using Crossdeck.Sim.XPlane.Api;
using Crossdeck.Sim.XPlane.Models;

namespace Crossdeck.Sim.XPlane;

/// <summary>
/// Implements <see cref="ISimProducer"/> using the X-Plane API.
/// </summary>
/// <remarks>
/// Connects via the X-Plane WebSocket API.
/// </remarks>
public sealed class XPlaneApiSimProducer : ISimProducer
{
    private static readonly SimRef AircraftRef = new("dataref", "sim/aircraft/view/acf_relative_path");

    /// <summary>
    /// Internal channel for managing and distributing simulation events.
    /// Used to handle communication of <see cref="SimEvent"/> instances
    /// between producers and consumers within the X-Plane simulation API integration.
    /// </summary>
    private readonly Channel<SimEvent> _eventsChannel = Channel.CreateUnbounded<SimEvent>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    /// <summary>
    /// Internal signal for notifying of changes in subscription states
    /// within the X-Plane simulation API integration.
    /// </summary>
    private readonly Signal _subscriptionsChangedSignal = new();

    private readonly HttpClient _httpClient;
    private readonly string _host;
    private readonly int _port;

    private int _eventsConsumed;
    private int _requestId;

    private SessionState? _sessionState;

    private string? _aircraft;

    private IReadOnlyList<SimRef> _subscriptions = new List<SimRef>();

    private Uri WsUri => new($"ws://{_host}:{_port}/api/v3");

    /// <summary>
    /// Provides an implementation of <see cref="ISimProducer"/> using the X-Plane API for simulating events and commands.
    /// </summary>
    /// <remarks>
    /// Establishes connectivity to the X-Plane simulator via its WebSocket API.
    /// It manages event consumption and facilitates communication with the simulator.
    /// </remarks>
    public XPlaneApiSimProducer(
        IHttpClientFactory httpClientFactory,
        XPlaneOptions options)
    {
        _httpClient = httpClientFactory.CreateClient("xplane") ??
                      throw new ArgumentNullException(nameof(httpClientFactory));
        _host = options.Host ?? throw new ArgumentNullException(nameof(options.Host));
        _port = options.Port ?? throw new ArgumentNullException(nameof(options.Port));
    }

    /// <summary>
    /// Asynchronous stream of <see cref="SimEvent"/>s.
    /// </summary>
    /// <remarks>
    /// This property provides a consumable channel for simulation events. It can only be accessed by a single reader.
    /// Future attempts to access this property after the first consumption will result in an <see cref="InvalidOperationException"/>.
    /// </remarks>
    public IAsyncEnumerable<SimEvent> Events => Interlocked.Exchange(ref _eventsConsumed, 1) == 0
        ? _eventsChannel.Reader.ReadAllAsync()
        : throw new InvalidOperationException("Events can only be consumed by a single reader");

    /// <summary>
    /// Publishes a simulation event to the internal event channel for subscribers to process.
    /// </summary>
    /// <param name="e">The simulation event to be published.</param>
    private void Publish(SimEvent e) => _eventsChannel.Writer.TryWrite(e);

    /// <summary>
    /// Updates the current simulator status and publishes a corresponding status change event.
    /// </summary>
    /// <param name="status">The new simulator status to be set and published.</param>
    private void Publish(SimStatus status)
    {
        Publish(new SimEvent.StatusChanged(status));
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
            Publish(new SimStatus.Gone());
            _eventsChannel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Updates the active set of subscriptions for simulation events.
    /// </summary>
    /// <param name="subscriptions">The collection of <see cref="SimRef"/> representing the desired simulation data subscriptions.</param>
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

        return command switch
        {
            SimCommand.Trigger trigger => await SendCommandAsync(session, trigger.Action, true, 0, cancellationToken),
            SimCommand.Begin begin => await SendCommandAsync(session, begin.Action, true, null, cancellationToken),
            SimCommand.End end => await SendCommandAsync(session, end.Action, false, null, cancellationToken),
            SimCommand.Set set => await SendDataRefAsync(session, set.Target, set.Value, cancellationToken),
            _ => new CommandResult.Unsupported()
        };
    }

    private async Task<CommandResult> SendCommandAsync(
        SessionState session,
        SimRef action,
        bool isActive,
        int? duration,
        CancellationToken cancellationToken)
    {
        var commandId = await ResolveResourceAsync(action.Key, "commands", session.CommandCache, cancellationToken);
        if (commandId is null) return new CommandResult.Failure();

        object? command = duration is { } d
            ? new { id = commandId.Value, is_active = isActive, duration = d }
            : new { id = commandId.Value, is_active = isActive, };

        await EnqueueAsync(session, "command_set_is_active", command);

        return new CommandResult.Success();
    }

    private async Task<CommandResult> SendDataRefAsync(
        SessionState session,
        SimRef simRef,
        SimValue simValue,
        CancellationToken cancellationToken)
    {
        var datarefId = await ResolveResourceAsync(simRef.Key, "datarefs", session.DataRefCache, cancellationToken);
        if (datarefId is null) return new CommandResult.Failure();

        object value = simValue switch
        {
            SimValue.Number number => number.Value,
            SimValue.String str => Convert.ToBase64String(Encoding.UTF8.GetBytes(str.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(simValue), simValue, null),
        };

        await EnqueueAsync(session, "dataref_set_values ",
            new { datarefs = new[] { new { newid = datarefId.Value, value } } });

        return new CommandResult.Success();
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

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(WsUri, cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sessionState = new SessionState();
        _sessionState = sessionState;
        _aircraft = null;

        Publish(new SimStatus.Connected());

        await WaitForInitializationAsync(cancellationToken);
        await _subscriptionsChangedSignal.SetAsync(cancellationToken);

        var sendLoop = RunChildLoopAsync(SendLoopAsync(ws, sessionState, cts.Token), cts);
        var subscriptionLoop = RunChildLoopAsync(SubscriptionLoopAsync(sessionState, cts.Token), cts);

        try
        {
            await ReceiveLoopAsync(ws, sessionState, cts.Token);
        }
        catch
        {
            // Ignored
        }
        finally
        {
            await cts.CancelAsync();
            await Task.WhenAll(sendLoop, subscriptionLoop);
        }
    }

    private static async Task RunChildLoopAsync(Task loop, CancellationTokenSource cts)
    {
        try
        {
            await loop;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        catch
        {
            await cts.CancelAsync();
        }
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

            var toAddIds = new List<long>();
            var toRemoveIds = new List<long>();

            foreach (var add in toAddKeys)
            {
                var id = await ResolveResourceAsync(add.Key, "datarefs", session.DataRefCache, cancellationToken);
                if (id is null)
                {
                    continue;
                }

                session.IdToRef[id.Value] = add;
                session.RefToId[add] = id.Value;
                toAddIds.Add(id.Value);
            }

            foreach (var remove in toRemove)
            {
                if (session.RefToId.TryGetValue(remove, out var id))
                {
                    toRemoveIds.Add(id);
                }
            }

            if (toAddIds.Count > 0)
            {
                await EnqueueAsync(session, "dataref_subscribe_values",
                    new { datarefs = toAddIds.Select(x => new { id = x }) });
            }

            if (toRemoveIds.Count <= 0) continue;
            {
                await EnqueueAsync(session, "dataref_unsubscribe_values",
                    new { datarefs = toRemoveIds.Select(x => new { id = x }) });
                foreach (var id in toRemoveIds)
                {
                    session.IdToRef.TryRemove(id, out var _);
                }
            }
        }
    }

    private async Task EnqueueAsync(SessionState session, string type, object @params)
    {
        var request = new
            { req_id = Interlocked.Increment(ref _requestId), type, @params };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request);

        await session.Outgoing.Writer.WriteAsync(bytes);
    }

    private async Task<long?> ResolveResourceAsync(
        string key,
        string kind,
        ConcurrentDictionary<string, long> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out var cachedId))
        {
            return cachedId;
        }

        var url = $"{kind}?filter[name]={key}";
        JsonElement httpResult = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);

        var data = httpResult.GetProperty("data");
        if (data.GetArrayLength() == 0)
        {
            return null;
        }

        var id = data[0].GetProperty("id").GetInt64();
        cache.TryAdd(key, id);

        return id;
    }

    private void Dispatch(ReadOnlyMemory<byte> data, SessionState session)
    {
        var json = JsonDocument.Parse(data);
        var root = json.RootElement;
        var type = root.GetProperty("type").GetString();

        switch (type)
        {
            case "dataref_update_values":
                foreach (var entry in root.GetProperty("data").EnumerateObject())
                {
                    if (!long.TryParse(entry.Name, out var id) ||
                        !session.IdToRef.TryGetValue(id, out var simRef))
                    {
                        continue;
                    }

                    var value = ToSimValue(entry.Value);
                    if (simRef == AircraftRef)
                    {
                        PublishAircraftIfChanged(value);
                        continue;
                    }

                    Publish(new SimEvent.ValueChanged(simRef, value));
                }

                break;
            case "result":
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

    private static SimValue ToSimValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => new SimValue.String(
            Encoding.UTF8.GetString(Convert.FromBase64String(value.GetString()!))),
        JsonValueKind.Number => new SimValue.Number(value.GetDouble()),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value.ValueKind.ToString())
    };

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
        while (true)
        {
            try
            {
                var countResponse =
                    await _httpClient.GetFromJsonAsync<CountResponse>("datarefs/count", cancellationToken);
                if (countResponse is not null && countResponse.Data != 0)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
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

        public readonly ConcurrentDictionary<string, long> DataRefCache = new();

        public readonly ConcurrentDictionary<string, long> CommandCache = new();
    }
}