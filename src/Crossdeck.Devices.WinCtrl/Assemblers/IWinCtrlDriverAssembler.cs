using Crossdeck.Core.Devices;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.WinCtrl.Assemblers;

public interface IWinCtrlDriverAssembler
{
    WinCtrlKind Kind { get; }
    
    IDeviceDriver Assemble(WinCtrlDefinition definition, HidDeviceDriver hid, IHidTransport transport);
}