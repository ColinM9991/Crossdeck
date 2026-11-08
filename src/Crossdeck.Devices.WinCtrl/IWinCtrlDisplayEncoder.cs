using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Devices.WinCtrl;

internal interface IScreenEncoder
{
    void Reset();
    
    byte[][] EncodeInit();
    
    byte[][] EncodeFrame(TextScreen screen);
}