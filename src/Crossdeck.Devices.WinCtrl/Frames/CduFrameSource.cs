using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.WinCtrl.Encoders;

namespace Crossdeck.Devices.WinCtrl.Frames;

public sealed class CduFrameSource(IWinCtrlDisplayEncoder encoder) : IWinCtrlFrameSource
{
    private TextScreen? _pending = TextScreen.WaitingForData;

    public void Show(TextScreen screen) => Interlocked.Exchange(ref _pending, screen);

    public byte[][] EncodeInit() => encoder.EncodeInit();

    public byte[][]? TakeFrame() =>
        Interlocked.Exchange(ref _pending, null) is { } screen ? encoder.EncodeFrame(screen) : null;
}