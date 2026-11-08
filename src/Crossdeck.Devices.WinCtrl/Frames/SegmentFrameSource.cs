using System.Collections.Concurrent;
using Crossdeck.Devices.WinCtrl.Encoders;

namespace Crossdeck.Devices.WinCtrl.Frames;

public sealed class SegmentFrameSource(WinCtrlSegmentEncoder encoder) : IWinCtrlFrameSource
{
    private readonly ConcurrentDictionary<string, string> _texts = new();
    private readonly ConcurrentDictionary<string, bool> _flags = new();
    private int _dirty;

    public void SetText(string window, string text)
    {
        _texts[window] = text;
        _dirty = 1;
    }

    public void SetFlag(string label, bool on)
    {
        _flags[label] = on;
        _dirty = 1;
    }

    public byte[][] EncodeInit() => [];

    public byte[][]? TakeFrame() =>
        Interlocked.Exchange(ref _dirty, 0) == 1
            ? encoder.EncodeFrame(_texts.ToDictionary(), _flags.Where(f => f.Value).Select(f => f.Key).ToHashSet())
            : null;
}