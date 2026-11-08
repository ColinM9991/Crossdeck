namespace Crossdeck.Devices.WinCtrl;

public static partial class WinCtrlDefinitions
{
    private static WinCtrlLight[] PdcLights(ushort target) =>
    [
        Level("backlight", "Backlight", target, 0x00),
        Level("screenBacklight", "Screen backlight", target, 0x01),
    ];

    private static readonly WinCtrlDefinition Pdc3N = new(
        "pdc-3n", 0xBB60, new HashSet<ushort> { 0xBB60, 0xBB61, 0xBB62 }, PdcLights(0xBB60),
        WinCtrlKind.Indicator, EmptyRoles, Displays: []);

    private static readonly WinCtrlDefinition Pdc3M = new(
        "pdc-3m", 0xBB50, new HashSet<ushort> { 0xBB50, 0xBB51, 0xBB52 }, PdcLights(0xBB50),
        WinCtrlKind.Indicator, EmptyRoles, Displays: []);
}