using System.Threading.Channels;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.Moza.Channels;
using Crossdeck.Devices.Moza.Protocol;
using Crossdeck.Devices.Serial;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza;

public sealed class FcdSession : IAsyncDisposable
{
    private const int ReadBufferSize = 512;
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    private readonly ISerialTransport _transport;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FcdSession> _logger;

    private readonly Channel<SessionEvent> _events =
        Channel.CreateUnbounded<SessionEvent>(new UnboundedChannelOptions { SingleReader = true });

    private readonly List<SerialLinkFrame> _frames = [];
    private readonly List<byte[]> _outgoing = [];
    private readonly Queue<byte[]> _outbox = new();
    private readonly SerialLinkDecoder _decoder = new();
    private readonly SerialLink _serialLink;
    private readonly StreamMux _streamMux;
    private readonly SettingsChannel _settingsChannel;
    private readonly ButtonEcho _buttonEcho;
    private readonly McduChannel _mcduChannel;

    private TextScreen _latestScreen = TextScreen.WaitingForData;
    private bool _closing;

    public FcdSession(
        ISerialTransport transport,
        TimeProvider timeProvider,
        ILogger<FcdSession> logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serialLink = new SerialLink(_timeProvider);
        _streamMux = new StreamMux(_timeProvider, _logger);
        _settingsChannel = new SettingsChannel(_timeProvider, _logger);
        _buttonEcho = new ButtonEcho();
        _mcduChannel = new McduChannel(_settingsChannel);
        _streamMux.Opened += OnStreamOpened;
    }

    public void Show(TextScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _events.Writer.TryWrite(new ScreenChangedEvent(screen));
    }

    public void PostButton(int button, bool pressed) =>
        _events.Writer.TryWrite(new ButtonChangedEvent(button, pressed));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var readerCts = new CancellationTokenSource();
        ITimer? timer = null;
        var reader = Task.CompletedTask;

        try
        {
            _transport.Open();
            timer = StartTicker();
            reader = Task.Run(() => ReadAsync(readerCts.Token), CancellationToken.None);
            await ProcessEventsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CloseAsync();
        }
        catch (FcdSessionLostException ex)
        {
            _logger.LogError(ex, "FCD session on {Port} lost", _transport.PortName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCD session on {Port} failed", _transport.PortName);
            await CloseAsync();
            throw new FcdSessionLostException($"FCD session on {_transport.PortName} failed.", ex);
        }
        finally
        {
            await StopAsync(timer, readerCts, reader);
        }
    }

    private ITimer StartTicker() =>
        _timeProvider.CreateTimer(_ => _events.Writer.TryWrite(TickEvent.Instance.Value), null, TickInterval,
            TickInterval);

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        Enqueue(_serialLink.Start(_timeProvider.GetTimestamp()));
        await FlushAsync(cancellationToken);

        await foreach (var e in _events.Reader.ReadAllAsync(cancellationToken))
        {
            Handle(e);
            await FlushAsync(cancellationToken);
        }
    }

    private async Task StopAsync(ITimer? timer, CancellationTokenSource readerCts, Task reader)
    {
        if (timer is not null)
            await timer.DisposeAsync();

        await readerCts.CancelAsync();
        await _transport.DisposeAsync();
        await reader;
    }

    private async Task ReadAsync(CancellationToken cancellationToken)
    {
        var error = await ReadUntilStoppedAsync(cancellationToken);
        _events.Writer.TryWrite(new ReaderStoppedEvent(error));
    }

    private async Task<Exception?> ReadUntilStoppedAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[ReadBufferSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await _transport.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                    return new EndOfStreamException($"Serial port {_transport.PortName} returned no data.");

                _events.Writer.TryWrite(new BytesReceivedEvent(buffer.AsSpan(0, bytesRead).ToArray()));
            }

            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private void Handle(SessionEvent e)
    {
        var now = _timeProvider.GetTimestamp();
        Dispatch(e, now);
        ServiceChannels(now);
    }

    private void Dispatch(SessionEvent e, long now)
    {
        switch (e)
        {
            case BytesReceivedEvent bytesReceived:
                HandleBytesReceived(bytesReceived.Payload, now);
                break;
            case TickEvent:
                HandleTick(now);
                break;
            case ScreenChangedEvent screenChanged when !_closing:
                _latestScreen = screenChanged.Screen;
                break;
            case ButtonChangedEvent buttonChanged when !_closing:
                _buttonEcho.OnButton(buttonChanged.Button, buttonChanged.Pressed);
                break;
            case ReaderStoppedEvent { Error: null }:
                throw new FcdSessionLostException($"Serial read on {_transport.PortName} stopped.");
            case ReaderStoppedEvent { Error: { } error }:
                throw new FcdSessionLostException($"Serial read on {_transport.PortName} failed.", error);
        }
    }

    private void ServiceChannels(long now)
    {
        _settingsChannel.Poll(now);
        _buttonEcho.Poll();
        _mcduChannel.Poll();

        if (!_closing)
            _mcduChannel.TrySend(_latestScreen);

        PumpStreams(now);
    }

    private void HandleBytesReceived(byte[] payload, long now)
    {
        _frames.Clear();
        _decoder.Decode(payload, _frames);

        foreach (var frame in _frames)
            DispatchFrame(frame, now);
    }

    private void DispatchFrame(SerialLinkFrame frame, long now)
    {
        if (_serialLink.OnFrame(frame, now) is { } reply)
            Enqueue(reply);

        if (!Wire.IsTunnel(frame) || _serialLink.State != LinkState.Linked)
            return;

        _outgoing.Clear();
        _streamMux.OnTunnel(frame.Payload, now, _outgoing);
        EnqueueOutgoing();
    }

    private void HandleTick(long now)
    {
        if (_serialLink.OnTick(now) == LinkState.Lost)
            throw new FcdSessionLostException($"SerialLink handshake on {_transport.PortName} timed out.");

        _settingsChannel.OnTick(now);
        _streamMux.CheckTimeouts(now);
    }

    private void PumpStreams(long now)
    {
        _outgoing.Clear();
        _streamMux.Next(now, _outgoing);
        EnqueueOutgoing();
    }

    private void OnStreamOpened(StreamConnection connection)
    {
        switch (connection.Service)
        {
            case StreamMux.TelemetryService:
                _buttonEcho.Attach(connection);
                break;
            case StreamMux.SettingsService:
                _settingsChannel.Attach(connection);
                break;
            case StreamMux.McduService:
                _mcduChannel.Attach(connection);
                break;
        }
    }

    private async Task CloseAsync()
    {
        if (_serialLink.State != LinkState.Linked || !_streamMux.HasConnections)
            return;

        try
        {
            using var deadline = new CancellationTokenSource(CloseTimeout, _timeProvider);
            BeginClose();
            await FlushAsync(deadline.Token);
            await DrainUntilClosedAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("FCD close on {Port} did not complete within {Timeout}", _transport.PortName,
                CloseTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCD close on {Port} failed", _transport.PortName);
        }
    }

    private void BeginClose()
    {
        _closing = true;
        _streamMux.Closing = true;
        _buttonEcho.ReleaseAll();
        _settingsChannel.RestoreDisplayMode();
        _streamMux.FinishAll();
        PumpStreams(_timeProvider.GetTimestamp());
    }

    private async Task DrainUntilClosedAsync(CancellationToken cancellationToken)
    {
        while (_streamMux.HasConnections)
        {
            var e = await _events.Reader.ReadAsync(cancellationToken);
            Handle(e);
            await FlushAsync(cancellationToken);
        }
    }

    private void EnqueueOutgoing()
    {
        foreach (var buffer in _outgoing)
            Enqueue(buffer);
    }

    private void Enqueue(byte[] buffer) => _outbox.Enqueue(buffer);

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        while (_outbox.TryDequeue(out var buffer))
            await _transport.WriteAsync(buffer, cancellationToken);
    }

    public async ValueTask DisposeAsync() => await _transport.DisposeAsync();
}