using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl.Drivers;

public sealed class WinCtrlCduDeviceDriver :
    WinCtrlDeviceDriverBase,
    ITextDisplayOutput
{
    private readonly CduFrameSource _source;
    public WinCtrlCduDeviceDriver(
        HidDeviceDriver driver,
        WinCtrlOutputWriter writer,
        CduFrameSource source)
        : base(driver, writer)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public void Show(TextScreen screen)
    {
        _source.Show(screen);
        Writer.MarkDirty();
    }
}