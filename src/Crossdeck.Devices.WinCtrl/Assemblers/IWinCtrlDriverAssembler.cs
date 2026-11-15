using Crossdeck.Core.Devices;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.WinCtrl.Assemblers;

/// <summary>
/// Assembles drivers for WinCtrl devices.
/// </summary>
public interface IWinCtrlDriverAssembler
{
    /// <summary>
    /// The kind of WinCtrl device this assembler handles.
    /// </summary>
    WinCtrlKind Kind { get; }

    /// <summary>
    /// Assembles a driver for a WinCtrl device.
    /// </summary>
    /// <param name="definition">The definition of the device to assemble.</param>
    /// <param name="hid">The HID driver of the underlying device.</param>
    /// <param name="transport">The transport used to send and receive HID reports.</param>
    /// <returns>The assembled device driver.</returns>
    IDeviceDriver Assemble(WinCtrlDefinition definition, HidDeviceDriver hid, IHidTransport transport);
}