namespace Crossdeck.Devices.WinCtrl.Models;

internal static class Slots
{
    private const int First = 25;

    private static readonly int[] FcuBit = [0, 2, 3, 1, 4, 5, 6, 7];
    private static readonly int[] EfisBit = [7, 1, 0, 2, 3, 6, 5, 4];

    public static DigitSlot Pap(int group, byte bit, bool hasDot = false) =>
        new(Build(k => k == 0 && !hasDot ? null : new BitAddr(k * 4 + group, bit)));

    public static DigitSlot Fcu(int packetByte, bool hasDot = false) =>
        new(Build(k => k == 0 && !hasDot
            ? null
            : new BitAddr(packetByte - First, (byte)(1 << FcuBit[k]))));

    public static DigitSlot FcuShifted(int start, int i) =>
        new(Build(k =>
        {
            if (k == 0) return null;
            var n = FcuBit[k];
            return n < 4
                ? new BitAddr(start + i - First, (byte)(1 << (n + 4)))
                : new BitAddr(start + i + 1 - First, (byte)(1 << (n - 4)));
        }));

    public static DigitSlot Efis(int packetByte, bool hasDot = false) =>
        new(Build(k => k == 0 && !hasDot
            ? null
            : new BitAddr(packetByte - First, (byte)(1 << EfisBit[k]))));

    private static BitAddr?[] Build(Func<int, BitAddr?> at)
    {
        var bars = new BitAddr?[8];
        for (var k = 0; k < 8; k++) bars[k] = at(k);
        return bars;
    }
}