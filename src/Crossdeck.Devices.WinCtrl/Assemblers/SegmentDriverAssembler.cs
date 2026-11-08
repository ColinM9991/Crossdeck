using Crossdeck.Core.Devices;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Assemblers;

public sealed class SegmentDriverAssembler : IWinCtrlDriverAssembler
{
    public WinCtrlKind Kind => WinCtrlKind.Segment;

    public IDeviceDriver Assemble(WinCtrlDefinition definition, HidDeviceDriver hid, IHidTransport transport)
    {
        var layout = definition.Segment ??
                     throw new InvalidOperationException($"'{definition.Part}' has no segment layout");
        var pid = (ushort)hid.Identity.ProductId;
        var names = WinCtrlDefinitions.FcuPanelNames(pid) ?? layout.Panels.Select(p => p.Name).ToHashSet();

        var panels = layout.Panels.Where(p => names.Contains(p.Name)).ToList();
        var targets = panels.Select(p => p.Target).ToHashSet();
        var lights = definition.Lights.Where(l => targets.Contains(l.Target)).ToList();

        var frameSource = new SegmentFrameSource(new WinCtrlSegmentEncoder(panels));
        var writer = new WinCtrlOutputWriter(transport,
            new WinCtrlHidReportEncoder(definition with { Lights = lights }), frameSource);
        return new WinCtrlSegmentDeviceDriver(hid, writer, frameSource);
    }
}