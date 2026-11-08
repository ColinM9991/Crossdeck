using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.WinCtrl;

public sealed record WinCtrlLight(OutputId Id, string Label, byte Light, int Max = 1);

public sealed record WinCtrlDefinition(
    string Part,
    ushort BasePid,
    IReadOnlySet<ushort> Pids,
    IReadOnlyList<WinCtrlLight> Lights,
    ushort Left,
    ushort Top,
    uint LargeFontId,
    uint SmallFontId)
{
    public byte TargetLo => (byte)(BasePid & 0xFF);
    public byte TargetHi => (byte)(BasePid >> 8);
}

/// <summary>
/// TODO: replace with a JSON catalogue describing the devices
/// </summary>
public static class WinCtrlDefinitions
{
    private static WinCtrlLight Led(string name, string label, byte light) =>
        new(new OutputId(OutputKind.Led, name), label, light);

    private static WinCtrlLight Level(string name, string label, byte light) =>
        new(new OutputId(OutputKind.Level, name), label, light, 255);

    private static readonly WinCtrlLight[] Backlights =
    [
        Level("keys", "Key backlight", 0x00),
        Level("screen", "Screen backlight", 0x01),
        Level("leds", "LED brightness", 0x02),
    ];

    private static WinCtrlLight[] McduLights =>
    [
        .. Backlights,
        Led("fail", "FAIL", 0x08),
        Led("fm", "FM", 0x09),
        Led("mcdu", "MCDU", 0x0A),
        Led("menu", "MENU", 0x0B),
        Led("fm1", "FM1", 0x0C),
        Led("ind", "IND", 0x0D),
        Led("rdy", "RDY", 0x0E),
        Led("status", "Status bar", 0x0F),
        Led("fm2", "FM2", 0x10)
    ];

    private static WinCtrlLight[] PfpLights =>
    [
        .. Backlights,
        Led("call", "CALL", 0x03),
        Led("fail", "FAIL", 0x04),
        Led("msg", "MSG", 0x05),
        Led("ofst", "OFST", 0x06),
        Led("exec", "EXEC", 0x07),
    ];

    private static readonly WinCtrlDefinition Mcdu = new("mcdu", 0xBB32,
        new HashSet<ushort> { 0xBB32, 0xBB36, 0xBB3A, 0xBB3E }, McduLights,
        Left: 52, Top: 37, LargeFontId: 5, SmallFontId: 6);
    
    private static readonly WinCtrlDefinition Pfp3N = new("pfp-3n", 0xBB31,
        new HashSet<ushort> { 0xBB31, 0xBB35, 0xBB3D, 0xBB39 }, PfpLights,
        Left: 50, Top: 19, LargeFontId: 1, SmallFontId: 2);

    private static readonly WinCtrlDefinition Pfp4 = new("pfp-4", 0xBB34,
        new HashSet<ushort> { 0xBB34, 0xBB38, 0xBB40, 0xBB3C }, PfpLights,
        Left: 50, Top: 19, LargeFontId: 1, SmallFontId: 2);

    private static readonly WinCtrlDefinition Pfp7 = new("pfp-7", 0xBB33,
        new HashSet<ushort> { 0xBB33, 0xBB37, 0xBB3F, 0xBB3B }, PfpLights,
        Left: 50, Top: 19, LargeFontId: 1, SmallFontId: 2);

    private static readonly IReadOnlyList<WinCtrlDefinition> All = [Mcdu, Pfp3N, Pfp4, Pfp7];

    public static WinCtrlDefinition? ForPid(int pid) =>
        All.FirstOrDefault(d => d.Pids.Contains((ushort)pid));
}