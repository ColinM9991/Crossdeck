using Crossdeck.Devices.WinCtrl.Models;

namespace Crossdeck.Devices.WinCtrl;

public static partial class WinCtrlDefinitions
{
    private const ushort Pap3Target = 0xBF0F;

    private static readonly SegmentWindow Pap3Spd = new([
        Slots.Pap(0, 0x08), Slots.Pap(0, 0x04, true), Slots.Pap(0, 0x02), Slots.Pap(0, 0x01)
    ]);

    private static readonly SegmentWindow Pap3CrsCapt =
        new([Slots.Pap(0, 0x80), Slots.Pap(0, 0x40), Slots.Pap(0, 0x20, true)]);

    private static readonly SegmentWindow Pap3Hdg = new([Slots.Pap(1, 0x40), Slots.Pap(1, 0x20), Slots.Pap(1, 0x10)]);

    private static readonly SegmentWindow Pap3Alt = new([
        Slots.Pap(1, 0x04), Slots.Pap(1, 0x02), Slots.Pap(1, 0x01, true), Slots.Pap(2, 0x80), Slots.Pap(2, 0x40)
    ]);

    private static readonly SegmentWindow Pap3Vs = new(
        [Slots.Pap(2, 0x08), Slots.Pap(2, 0x04, true), Slots.Pap(2, 0x02), Slots.Pap(2, 0x01)],
        Minus: [new BitAddr(6, 0x10)],
        Plus: [new BitAddr(6, 0x10), new BitAddr(19, 0x80), new BitAddr(15, 0x80)]);

    private static readonly SegmentWindow Pap3CrsFo =
        new([Slots.Pap(3, 0x40), Slots.Pap(3, 0x20), Slots.Pap(3, 0x10, true)]);

    private static readonly IReadOnlyDictionary<string, SegmentWindow> Pap3Windows =
        new Dictionary<string, SegmentWindow>
        {
            ["spd"] = Pap3Spd,
            ["crsCapt"] = Pap3CrsCapt,
            ["hdg"] = Pap3Hdg,
            ["alt"] = Pap3Alt,
            ["vs"] = Pap3Vs,
            ["crsFo"] = Pap3CrsFo,
        };

    private static readonly IReadOnlyDictionary<string, BitAddr[]> Pap3Labels = new Dictionary<string, BitAddr[]>
    {
        ["ias"] = [new BitAddr(29, 0x80)],
        ["mach"] = [new BitAddr(25, 0x80), new BitAddr(21, 0x80)],
        ["hdg"] = [new BitAddr(29, 0x08), new BitAddr(25, 0x08)],
        ["trk"] = [new BitAddr(21, 0x08), new BitAddr(17, 0x08)],
        ["vs"] = [new BitAddr(31, 0x80)],
        ["fpa"] = [new BitAddr(27, 0x80)],
        ["hdgDot"] = [new BitAddr(13, 0x08)],
        ["spdColonLower"] = [new BitAddr(5, 0x80)],
        ["spdColonUpper"] = [new BitAddr(9, 0x80)],
    };

    private static readonly SegmentPanel Pap3Panel = new(
        "pap3", Pap3Target, 32, Pap3Windows, Pap3Labels,
        new SegmentFraming(PayloadLength: 176, WriteId: 0x0050A2DF, CommitId: 0x0050A2DF, CommitInOwnPacket: false));

    private static readonly WinCtrlLight[] Pap3Lights =
    [
        Level("pap3.backlight", "Backlight", Pap3Target, 0x00),
        Level("pap3.lcd", "LCD", Pap3Target, 0x01),
        Level("pap3.backlight3", "Backlight 3", Pap3Target, 0x02),
        Led("pap3.n1", "N1", Pap3Target, 0x03),
        Led("pap3.speed", "Speed", Pap3Target, 0x04),
        Led("pap3.vnav", "VNAV", Pap3Target, 0x05),
        Led("pap3.lvlChg", "LVL CHG", Pap3Target, 0x06),
        Led("pap3.hdgSel", "HDG SEL", Pap3Target, 0x07),
        Led("pap3.lnav", "LNAV", Pap3Target, 0x08),
        Led("pap3.vorLoc", "VOR LOC", Pap3Target, 0x09),
        Led("pap3.app", "APP", Pap3Target, 0x0A),
        Led("pap3.altHld", "ALT HLD", Pap3Target, 0x0B),
        Led("pap3.vs", "V/S", Pap3Target, 0x0C),
        Led("pap3.cmdA", "CMD A", Pap3Target, 0x0D),
        Led("pap3.cwsA", "CWS A", Pap3Target, 0x0E),
        Led("pap3.cmdB", "CMD B", Pap3Target, 0x0F),
        Led("pap3.cwsB", "CWS B", Pap3Target, 0x10),
        Led("pap3.atArm", "A/T ARM", Pap3Target, 0x11),
        Led("pap3.maCapt", "MA CAPT", Pap3Target, 0x12),
        Led("pap3.maFo", "MA F/O", Pap3Target, 0x13),
        Led("pap3.atSolenoid", "A/T Solenoid", Pap3Target, 0x1E),
    ];

    private static readonly WinCtrlDefinition Pap3 = new(
        "pap3", Pap3Target, new HashSet<ushort> { Pap3Target }, Pap3Lights,
        WinCtrlKind.Segment, EmptyRoles, Segment: new SegmentLayout([Pap3Panel]));
}