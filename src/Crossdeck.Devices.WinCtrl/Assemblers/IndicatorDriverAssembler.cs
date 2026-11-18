using Crossdeck.Core.Devices;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.WinCtrl.Assemblers;

public sealed class IndicatorDriverAssembler(ILoggerFactory loggerFactory) : IWinCtrlDriverAssembler
{
    public WinCtrlKind Kind => WinCtrlKind.Indicator;

    public IDeviceDriver Assemble(WinCtrlDefinition definition, HidDeviceDriver hid, IHidTransport transport)
    {
        var writer = new WinCtrlOutputWriter(transport, new WinCtrlHidReportEncoder(definition),
            new NoFrameSource(), loggerFactory.CreateLogger<WinCtrlOutputWriter>());
        return new WinCtrlIndicatorDeviceDriver(hid, writer);
    }
}