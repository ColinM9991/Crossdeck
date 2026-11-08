using Crossdeck.Devices.WinCtrl.Models;

namespace Crossdeck.Devices.WinCtrl;

public static partial class WinCtrlDefinitions
{
    private const ushort FcuTarget = 0xBB10;
    private const ushort EfisLTarget = 0xBF0D;
    private const ushort EfisRTarget = 0xBF0E;

    private static readonly SegmentWindow FcuSpd = new([Slots.Fcu(25), Slots.Fcu(26, true), Slots.Fcu(27)]);

    private static readonly SegmentWindow FcuHdg = new([
        Slots.FcuShifted(28, 0), Slots.FcuShifted(28, 1), Slots.FcuShifted(28, 2)
    ]);

    private static readonly SegmentWindow FcuAlt = new([
        Slots.FcuShifted(32, 0), Slots.FcuShifted(32, 1), Slots.FcuShifted(32, 2), Slots.FcuShifted(32, 3),
        Slots.FcuShifted(32, 4)
    ]);

    private static readonly SegmentWindow FcuVs = new(
        [Slots.FcuShifted(37, 0), Slots.FcuShifted(37, 1), Slots.FcuShifted(37, 2), Slots.FcuShifted(37, 3)],
        Minus: [new BitAddr(12, 0x10)],
        Plus: [new BitAddr(12, 0x10), new BitAddr(14, 0x10)]);

    private static readonly IReadOnlyDictionary<string, SegmentWindow> FcuWindows =
        new Dictionary<string, SegmentWindow>
        {
            ["spd"] = FcuSpd,
            ["hdg"] = FcuHdg,
            ["alt"] = FcuAlt,
            ["vs"] = FcuVs,
        };

    private static readonly IReadOnlyDictionary<string, BitAddr[]> FcuLabels = new Dictionary<string, BitAddr[]>
    {
        ["spd"] = [new BitAddr(3, 0x08)],
        ["mach"] = [new BitAddr(3, 0x04)],
        ["spdManaged"] = [new BitAddr(3, 0x02)],
        ["hdg"] = [new BitAddr(6, 0x80)],
        ["trk"] = [new BitAddr(6, 0x40)],
        ["lat"] = [new BitAddr(6, 0x20)],
        ["hdgManaged"] = [new BitAddr(6, 0x10)],
        ["hdgVs"] = [new BitAddr(7, 0x08)],
        ["vsCentre"] = [new BitAddr(7, 0x04)],
        ["trkFpa"] = [new BitAddr(7, 0x02)],
        ["fpaCentre"] = [new BitAddr(7, 0x01)],
        ["alt"] = [new BitAddr(8, 0x10)],
        ["lvlChLeft"] = [new BitAddr(9, 0x10)],
        ["lvlCh"] = [new BitAddr(10, 0x10)],
        ["lvlChRight"] = [new BitAddr(11, 0x10)],
        ["fpaDot"] = [new BitAddr(13, 0x10)],
        ["vsLine2"] = [new BitAddr(14, 0x20)],
        ["altManaged"] = [new BitAddr(15, 0x10)],
        ["vs"] = [new BitAddr(16, 0x40)],
        ["fpa"] = [new BitAddr(16, 0x80)],
    };

    private static readonly SegmentPanel FcuPanel = new(
        "fcu", FcuTarget, 17, FcuWindows, FcuLabels,
        new SegmentFraming(PayloadLength: 32, WriteId: 0x0002FFFF, CommitId: 0x0002FFFF, CommitInOwnPacket: true));

    private static readonly SegmentWindow EfisBaro = new([
        Slots.Efis(25), Slots.Efis(26, true), Slots.Efis(27), Slots.Efis(28)
    ]);

    private static IReadOnlyDictionary<string, SegmentWindow> EfisWindows(string panel) =>
        new Dictionary<string, SegmentWindow>
        {
            [$"{panel}.baro"] = EfisBaro,
        };

    private static readonly IReadOnlyDictionary<string, BitAddr[]> EfisLabels = new Dictionary<string, BitAddr[]>
    {
        ["qnh"] = [new BitAddr(4, 0x02)],
        ["qfe"] = [new BitAddr(4, 0x01)],
    };

    private static SegmentPanel EfisPanel(string name, ushort target) => new(
        name, target, 5, EfisWindows(name), EfisLabels,
        new SegmentFraming(PayloadLength: 9, WriteId: 0x001DFFFF, CommitId: 0x001D0C4C, CommitInOwnPacket: true));

    private static readonly SegmentPanel EfisLPanel = EfisPanel("efisL", EfisLTarget);
    private static readonly SegmentPanel EfisRPanel = EfisPanel("efisR", EfisRTarget);

    private static readonly WinCtrlLight[] FcuLights =
    [
        Level("backlight", "Backlight", FcuTarget, 0x00),
        Level("lcd", "LCD", FcuTarget, 0x01),
        Level("indicatorBrightness", "Indicator brightness", FcuTarget, 0x11),
        Level("expedBacklight", "EXPED backlight", FcuTarget, 0x1E),
        Led("loc", "LOC", FcuTarget, 0x03),
        Led("locYellow", "LOC (yellow)", FcuTarget, 0x04),
        Led("ap1", "AP1", FcuTarget, 0x05),
        Led("ap1Yellow", "AP1 (yellow)", FcuTarget, 0x06),
        Led("ap2", "AP2", FcuTarget, 0x07),
        Led("ap2Yellow", "AP2 (yellow)", FcuTarget, 0x08),
        Led("athr", "A/THR", FcuTarget, 0x09),
        Led("athrYellow", "A/THR (yellow)", FcuTarget, 0x0A),
        Led("exped", "EXPED", FcuTarget, 0x0B),
        Led("expedYellow", "EXPED (yellow)", FcuTarget, 0x0C),
        Led("appr", "APPR", FcuTarget, 0x0D),
        Led("apprYellow", "APPR (yellow)", FcuTarget, 0x0E),
    ];

    private static readonly WinCtrlDisplay[] FcuDisplays =
    [
        Display("spd", "Speed", FcuTarget),
        Display("hdg", "Heading", FcuTarget),
        Display("alt", "Altitude", FcuTarget),
        Display("vs", "V/S", FcuTarget),
    ];

    private static WinCtrlDisplay[] EfisDisplays(string panel, ushort target) =>
    [
        Display($"{panel}.baro", "Baro", target),
    ];

    private static WinCtrlLight[] EfisLights(string panel, ushort target) =>
    [
        Level($"{panel}.backlight", "Backlight", target, 0x00),
        Level($"{panel}.lcd", "LCD", target, 0x01),
        Level($"{panel}.indicatorBrightness", "Indicator brightness", target, 0x11),
        Led($"{panel}.fd", "FD", target, 0x03),
        Led($"{panel}.ls", "LS", target, 0x04),
        Led($"{panel}.cstr", "CSTR", target, 0x05),
        Led($"{panel}.wpt", "WPT", target, 0x06),
        Led($"{panel}.vorD", "VOR/D", target, 0x07),
        Led($"{panel}.ndb", "NDB", target, 0x08),
        Led($"{panel}.arpt", "ARPT", target, 0x09),
    ];

    private static readonly WinCtrlDefinition Fcu = new(
        "fcu", FcuTarget, new HashSet<ushort> { 0xBB10, 0xBC1D, 0xBC1E, 0xBA01 },
        [.. FcuLights, .. EfisLights("efisL", EfisLTarget), .. EfisLights("efisR", EfisRTarget)],
        WinCtrlKind.Segment, EmptyRoles,
        Segment: new SegmentLayout([FcuPanel, EfisLPanel, EfisRPanel]),
        Displays: [.. FcuDisplays, .. EfisDisplays("efisL", EfisLTarget), .. EfisDisplays("efisR", EfisRTarget)]);

    private static readonly IReadOnlyDictionary<ushort, string[]> FcuPanelsByPid = new Dictionary<ushort, string[]>
    {
        [0xBB10] = ["fcu"],
        [0xBC1D] = ["fcu", "efisL"],
        [0xBC1E] = ["fcu", "efisR"],
        [0xBA01] = ["fcu", "efisL", "efisR"],
    };

    internal static IReadOnlySet<string>? FcuPanelNames(ushort pid) =>
        FcuPanelsByPid.TryGetValue(pid, out var names) ? names.ToHashSet() : null;
}