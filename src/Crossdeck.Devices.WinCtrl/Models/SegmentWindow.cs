namespace Crossdeck.Devices.WinCtrl.Models;

public sealed record SegmentWindow(DigitSlot[] Digits, BitAddr[]? Minus = null, BitAddr[]? Plus = null);