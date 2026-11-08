using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Drivers;

public sealed class WinCtrlSegmentDeviceDriver(
    HidDeviceDriver hid,
    WinCtrlOutputWriter writer,
    SegmentFrameSource source)
    : WinCtrlDeviceDriverBase(hid, writer), ISegmentDisplayOutput
{
    public void SetText(OutputId id, string text)
    {
        source.SetText(id, text);
        Writer.MarkDirty();
    }

    public override void Set(OutputId id, int value)
    {
        if (id.Kind == OutputKind.Flag)
        {
            source.SetFlag(id.Name, value != 0);
            Writer.MarkDirty();
        }
        else
        {
            base.Set(id, value);
        }
    }
}