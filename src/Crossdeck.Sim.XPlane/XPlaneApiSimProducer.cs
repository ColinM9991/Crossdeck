using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Crossdeck.Core.Simulators;

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
    /// Channel for publishing <see cref="SimEvent"/>s.
    /// </summary>
    private readonly Channel<SimEvent> _eventsChannel = Channel.CreateUnbounded<SimEvent>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    /// <summary>
    /// Channel for announcing subscription changes.
    /// </summary>
    private readonly Channel<bool> _subscriptionsChangedChannel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });

    private readonly HttpClient _httpClient;
    private readonly string _host;
    private readonly int _port;

    private int _eventsConsumed;
    private int _requestId;

    private SimStatus? _simStatus;
    private SessionState? _sessionState;

    private string? _aircraft;

    private IReadOnlyList<SimRef> _subscriptions = new List<SimRef>();

    private Uri _wsUri => new($"ws://{_host}:{_port}/api/v3");

    public XPlaneApiSimProducer(
        IHttpClientFactory httpClientFactory,
        XPlaneOptions options)
    {
        _httpClient = httpClientFactory?.CreateClient("xplane") ??
                      throw new ArgumentNullException(nameof(httpClientFactory));
        _host = options.Host ?? throw new ArgumentNullException(nameof(options.Host));
        _port = options.Port ?? throw new ArgumentNullException(nameof(options.Port));
    }

    public IAsyncEnumerable<SimEvent> Events => Interlocked.Exchange(ref _eventsConsumed, 1) == 0
        ? _eventsChannel.Reader.ReadAllAsync()
        : throw new InvalidOperationException("Events can only be consumed by a single reader");

    private void Publish(SimEvent e) => _eventsChannel.Writer.TryWrite(e);

    private void Publish(SimStatus status)
    {
        _simStatus = status;
        Publish(new SimEvent.StatusChanged(status));
    }

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
            _eventsChannel.Writer.TryComplete();
        }
    }

    public async Task SetSubscriptionsAsync(IReadOnlySet<SimRef> subscriptions)
    {
        _subscriptions = [.. subscriptions];
        await _subscriptionsChangedChannel.Writer.WriteAsync(true);
    }

    public Task SendCommandAsync(SimCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        _eventsChannel.Writer.TryComplete();
        _sessionState?.Outgoing.Writer.TryComplete();

        return ValueTask.CompletedTask;
    }

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(_wsUri, cancellationToken);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sessionState = new SessionState();
        _sessionState = sessionState;
        _aircraft = null;

        Publish(new SimStatus.Connected());

        await _subscriptionsChangedChannel.Writer.WriteAsync(true, cancellationToken);

        var sendLoop = SendLoopAsync(ws, sessionState, cts.Token);
        var subscriptionLoop = SubscriptionLoopAsync(sessionState, cts.Token);

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
            await Task.WhenAll(sendLoop, subscriptionLoop);
        }
    }

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

    private async Task SendLoopAsync(ClientWebSocket ws, SessionState session, CancellationToken cancellationToken)
    {
        await foreach (var message in session.Outgoing.Reader.ReadAllAsync(cancellationToken))
        {
            await ws.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    private async Task SubscriptionLoopAsync(SessionState session, CancellationToken cancellationToken)
    {
        await foreach (var _ in _subscriptionsChangedChannel.Reader.ReadAllAsync(cancellationToken))
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
                await Enqueue(session, "dataref_subscribe_values",
                    new { datarefs = toAddIds.Select(x => new { id = x }) });
            }

            if (toRemoveIds.Count <= 0) continue;
            {
                await Enqueue(session, "dataref_unsubscribe_values",
                    new { datarefs = toRemoveIds.Select(x => new { id = x }) });
                foreach (var id in toRemoveIds)
                {
                    session.IdToRef.TryRemove(id, out var _);
                }
            }
        }
    }

    private async Task Enqueue(SessionState session, string type, object @params)
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

        JsonElement httpResult;
        var url = $"{kind}?filter[name]={key}";
        try
        {
            httpResult = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw;
        }

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