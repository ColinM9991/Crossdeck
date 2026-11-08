using System.Collections.Concurrent;
using System.Diagnostics;
using Crossdeck.Core;
using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.WinCtrl;

internal sealed class WinCtrlOutputWriter : IDisplayOutputWriter
{
    private readonly IHidTransport _transport;
    private readonly IHidReportEncoder _encoder;
    private readonly IScreenEncoder _screenEncoder;
    private readonly Signal _dirtySignal = new();

    private long _lastFrameStart;
    private TextScreen? _screen;
    private readonly ConcurrentDictionary<OutputId, int> _state = new();

    public WinCtrlOutputWriter(
        IHidTransport transport,
        IHidReportEncoder encoder,
        IScreenEncoder screenEncoder)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        _screenEncoder = screenEncoder ?? throw new ArgumentNullException(nameof(screenEncoder));
    }
    
    public void Set(OutputId id, int value)
    {
        _state[id] = value;
        _dirtySignal.Set();
    }

    public void Set(TextScreen screen)
    {
        Interlocked.Exchange(ref _screen, screen);
        _dirtySignal.Set();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await InitialiseScreenAsync(cts.Token);
        await _dirtySignal.SetAsync(cancellationToken);

        await foreach (var _ in _dirtySignal.WaitAsync(cts.Token))
        {
            await SendLedStatesAsync(cts.Token);
            await SendScreenAsync(cts.Token);
        }
    }
    
    private async Task InitialiseScreenAsync(CancellationToken cancellationToken)
    {
        _state[BacklightOutput()] = 255;
        await SendLedStatesAsync(cancellationToken);

        foreach (var report in _screenEncoder.EncodeInit())
        {
            await _transport.WriteAsync(report, cancellationToken);
        }
        
        foreach (var report in _screenEncoder.EncodeFrame(TextScreen.WaitingForData))
        {
            await _transport.WriteAsync(report, cancellationToken);
        }

        return;
        static OutputId BacklightOutput()
        {
            return new OutputId(OutputKind.Level, "screen");
        }
    }

    private async Task SendLedStatesAsync(CancellationToken cancellationToken)
    {
        var snapshot = _state.ToDictionary();

        foreach (var report in _encoder.Encode(snapshot))
        {
            await _transport.WriteAsync(report.Data, cancellationToken);
        }
    }

    private async Task SendScreenAsync(CancellationToken cancellationToken)
    {
        var current = Stopwatch.GetTimestamp();
        var elapsed = current - _lastFrameStart;
        if (elapsed < 66) return;
        
        var currentScreen = Interlocked.Exchange(ref _screen, null);
        if (currentScreen is null) return;

        foreach (var report in _screenEncoder.EncodeFrame(currentScreen))
        {
            await _transport.WriteAsync(report, cancellationToken);
        }

        _lastFrameStart = current;
    }
}