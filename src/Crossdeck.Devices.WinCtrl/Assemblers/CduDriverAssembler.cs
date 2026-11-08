using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Assemblers;

public sealed class CduDriverAssembler : IWinCtrlDriverAssembler
{
    public WinCtrlKind Kind => WinCtrlKind.Cdu;

    public IDeviceDriver Assemble(WinCtrlDefinition definition, HidDeviceDriver hid, IHidTransport transport)
    {
        var frameSource = new CduFrameSource(new WinCtrlWinCtrlDisplayEncoder(definition, TextPalette.Default));
        var writer = new WinCtrlOutputWriter(transport, new WinCtrlHidReportEncoder(definition), frameSource);
        return new WinCtrlCduDeviceDriver(hid, writer, frameSource);
    }
}