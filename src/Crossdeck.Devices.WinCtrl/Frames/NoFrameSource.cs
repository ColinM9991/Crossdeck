namespace Crossdeck.Devices.WinCtrl.Frames;

public sealed class NoFrameSource : IWinCtrlFrameSource
{
    public byte[][] EncodeInit() => [];
    public byte[][]? TakeFrame() => null;
}