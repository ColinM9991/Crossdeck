using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Devices.WinCtrl.Encoders;

public interface IWinCtrlDisplayEncoder
{
    void Reset();
    
    byte[][] EncodeInit();
    
    byte[][] EncodeFrame(TextScreen screen);
}