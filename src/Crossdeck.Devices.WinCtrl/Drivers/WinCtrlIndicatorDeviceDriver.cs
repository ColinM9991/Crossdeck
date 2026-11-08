using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.WinCtrl.Drivers;

public sealed class WinCtrlIndicatorDeviceDriver(HidDeviceDriver hid, WinCtrlOutputWriter writer)
    : WinCtrlDeviceDriverBase(hid, writer);