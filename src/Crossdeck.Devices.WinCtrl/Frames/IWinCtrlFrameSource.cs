namespace Crossdeck.Devices.WinCtrl.Frames;

public interface IWinCtrlFrameSource
{
    byte[][] EncodeInit();
    byte[][]? TakeFrame();
}
