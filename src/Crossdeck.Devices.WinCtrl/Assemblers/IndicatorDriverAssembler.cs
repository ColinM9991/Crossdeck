using Crossdeck.Core.Devices;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Assemblers;

public sealed class IndicatorDriverAssembler : IWinCtrlDriverAssembler
{
    public WinCtrlKind Kind => WinCtrlKind.Indicator;

    public IDeviceDriver Assemble(WinCtrlDefinition definition, HidDeviceDriver hid, IHidTransport transport)
    {
        var writer = new WinCtrlOutputWriter(transport, new WinCtrlHidReportEncoder(definition), new NoFrameSource());
        return new WinCtrlIndicatorDeviceDriver(hid, writer);
    }
}