using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.WinCtrl.Models;

namespace Crossdeck.Devices.WinCtrl;

public enum WinCtrlKind
{
    Cdu,
    Segment,
    Indicator
}

public enum DisplayRole
{
    Captain,
    FirstOfficer,
    Observer
}

public sealed record CduScreenSettings(ushort Left, ushort Top, uint LargeFontId, uint SmallFontId);

public sealed record WinCtrlLight(OutputId Id, string Label, byte Light, ushort Target, int Max = 1);

public sealed record WinCtrlDefinition(
    string Part,
    ushort BasePid,
    IReadOnlySet<ushort> Pids,
    IReadOnlyList<WinCtrlLight> Lights,
    WinCtrlKind Kind,
    IReadOnlyDictionary<ushort, DisplayRole> Roles,
    CduScreenSettings? Cdu = null,
    SegmentLayout? Segment = null);

/// <summary>
/// TODO: replace with a JSON catalogue describing the devices
/// </summary>
public static partial class WinCtrlDefinitions
{
    private static readonly IReadOnlyDictionary<ushort, DisplayRole> EmptyRoles = new Dictionary<ushort, DisplayRole>();

    private static WinCtrlLight Led(string name, string label, ushort target, byte light) =>
        new(new OutputId(OutputKind.Led, name), label, light, target);

    private static WinCtrlLight Level(string name, string label, ushort target, byte light) =>
        new(new OutputId(OutputKind.Level, name), label, light, target, 255);

    private static WinCtrlLight[] Backlights(ushort target) =>
    [
        Level("keys", "Key backlight", target, 0x00),
        Level("screen", "Screen backlight", target, 0x01),
        Level("leds", "LED brightness", target, 0x02),
    ];

    private static WinCtrlLight[] McduLights(ushort target) =>
    [
        .. Backlights(target),
        Led("fail", "FAIL", target, 0x08),
        Led("fm", "FM", target, 0x09),
        Led("mcdu", "MCDU", target, 0x0A),
        Led("menu", "MENU", target, 0x0B),
        Led("fm1", "FM1", target, 0x0C),
        Led("ind", "IND", target, 0x0D),
        Led("rdy", "RDY", target, 0x0E),
        Led("status", "Status bar", target, 0x0F),
        Led("fm2", "FM2", target, 0x10)
    ];

    private static WinCtrlLight[] PfpLights(ushort target) =>
    [
        .. Backlights(target),
        Led("call", "CALL", target, 0x03),
        Led("fail", "FAIL", target, 0x04),
        Led("msg", "MSG", target, 0x05),
        Led("ofst", "OFST", target, 0x06),
        Led("exec", "EXEC", target, 0x07),
    ];

    private static IReadOnlyDictionary<ushort, DisplayRole> CduRoles(ushort basePid, ushort captain,
        ushort firstOfficer, ushort observer) =>
        new Dictionary<ushort, DisplayRole>
        {
            [basePid] = DisplayRole.Captain,
            [captain] = DisplayRole.Captain,
            [firstOfficer] = DisplayRole.FirstOfficer,
            [observer] = DisplayRole.Observer,
        };

    private static readonly WinCtrlDefinition Mcdu = new(
        "mcdu", 0xBB32, new HashSet<ushort> { 0xBB32, 0xBB36, 0xBB3A, 0xBB3E }, McduLights(0xBB32),
        WinCtrlKind.Cdu, CduRoles(0xBB32, 0xBB36, 0xBB3E, 0xBB3A),
        Cdu: new CduScreenSettings(Left: 52, Top: 37, LargeFontId: 5, SmallFontId: 6));

    private static readonly WinCtrlDefinition Pfp3N = new(
        "pfp-3n", 0xBB31, new HashSet<ushort> { 0xBB31, 0xBB35, 0xBB3D, 0xBB39 }, PfpLights(0xBB31),
        WinCtrlKind.Cdu, CduRoles(0xBB31, 0xBB35, 0xBB3D, 0xBB39),
        Cdu: new CduScreenSettings(Left: 50, Top: 19, LargeFontId: 1, SmallFontId: 2));

    private static readonly WinCtrlDefinition Pfp4 = new(
        "pfp-4", 0xBB34, new HashSet<ushort> { 0xBB34, 0xBB38, 0xBB40, 0xBB3C }, PfpLights(0xBB34),
        WinCtrlKind.Cdu, CduRoles(0xBB34, 0xBB38, 0xBB40, 0xBB3C),
        Cdu: new CduScreenSettings(Left: 50, Top: 19, LargeFontId: 1, SmallFontId: 2));

    private static readonly WinCtrlDefinition Pfp7 = new(
        "pfp-7", 0xBB33, new HashSet<ushort> { 0xBB33, 0xBB37, 0xBB3F, 0xBB3B }, PfpLights(0xBB33),
        WinCtrlKind.Cdu, CduRoles(0xBB33, 0xBB37, 0xBB3F, 0xBB3B),
        Cdu: new CduScreenSettings(Left: 50, Top: 19, LargeFontId: 1, SmallFontId: 2));

    private static IReadOnlyList<WinCtrlDefinition> All() =>
        [Mcdu, Pfp3N, Pfp4, Pfp7, Pap3, Fcu, Pdc3N, Pdc3M];

    public static WinCtrlDefinition? ForPid(int pid) =>
        All().FirstOrDefault(d => d.Pids.Contains((ushort)pid));
}