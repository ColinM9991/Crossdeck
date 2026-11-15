using System.Collections.Concurrent;
using System.Diagnostics;
using Crossdeck.Core;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl;

public sealed class WinCtrlOutputWriter(
    IHidTransport transport,
    IHidReportEncoder ledEncoder,
    IWinCtrlFrameSource frames) : IDeviceOutputWriter
{
    private readonly IHidTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    private readonly IHidReportEncoder _ledEncoder = ledEncoder ?? throw new ArgumentNullException(nameof(ledEncoder));
    private readonly IWinCtrlFrameSource _frames = frames ?? throw new ArgumentNullException(nameof(frames));
    private readonly ConcurrentDictionary<OutputId, int> _state = new();
    private readonly Signal _dirty = new();
    private long _lastFrame;

    public void Set(OutputId id, int value)
    {
        _state[id] = value;
        _dirty.Set();
    }

    public void MarkDirty() => _dirty.Set();

    public async Task RunAsync(CancellationToken ct)
    {
        await SendLedsAsync(ct);
        await WriteAllAsync(_frames.EncodeInit(), ct);
        await _dirty.SetAsync(ct);

        await foreach (var _ in _dirty.WaitAsync(ct))
        {
            await SendLedsAsync(ct);

            var wait = TimeSpan.FromMilliseconds(66) - Stopwatch.GetElapsedTime(_lastFrame);
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);

            if (_frames.TakeFrame() is not { } reports) continue;

            await WriteAllAsync(reports, ct);
            _lastFrame = Stopwatch.GetTimestamp();
        }
    }

    private Task SendLedsAsync(CancellationToken ct) =>
        WriteAllAsync(_ledEncoder.Encode(_state.ToDictionary()).Select(r => r.Data), ct);

    private async Task WriteAllAsync(IEnumerable<byte[]> reports, CancellationToken ct)
    {
        foreach (var report in reports)
            await _transport.WriteAsync(report, ct);
    }
}