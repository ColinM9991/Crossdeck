namespace Crossdeck.Devices.WinCtrl.Models;

[Flags]
public enum SegmentBars : byte
{
    None = 0,
    Dot = 1 << 0,
    G = 1 << 1,
    F = 1 << 2,
    E = 1 << 3,
    D = 1 << 4,
    C = 1 << 5,
    B = 1 << 6,
    A = 1 << 7
}