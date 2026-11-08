using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Devices.WinCtrl.Models;

namespace Crossdeck.Devices.WinCtrl.Encoders;

internal class WinCtrlSegmentEncoder : IHidReportEncoder
{
    private static readonly Dictionary<char, SegmentBars> Glyphs = new()
    {
        ['0'] = SegmentBars.A | SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.F,
        ['1'] = SegmentBars.B | SegmentBars.C,
        ['2'] = SegmentBars.A | SegmentBars.B | SegmentBars.D | SegmentBars.E | SegmentBars.G,
        ['3'] = SegmentBars.A | SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.G,
        ['4'] = SegmentBars.B | SegmentBars.C | SegmentBars.F | SegmentBars.G,
        ['5'] = SegmentBars.A | SegmentBars.C | SegmentBars.D | SegmentBars.F | SegmentBars.G,
        ['6'] = SegmentBars.A | SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['7'] = SegmentBars.A | SegmentBars.B | SegmentBars.C,
        ['8'] = SegmentBars.A | SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.F |
                SegmentBars.G,
        ['9'] = SegmentBars.A | SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.F | SegmentBars.G,
        ['A'] = SegmentBars.A | SegmentBars.B | SegmentBars.C | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['B'] = SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['C'] = SegmentBars.A | SegmentBars.D | SegmentBars.E | SegmentBars.F,
        ['D'] = SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.G,
        ['E'] = SegmentBars.A | SegmentBars.D | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['F'] = SegmentBars.A | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['G'] = SegmentBars.A | SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.F,
        ['H'] = SegmentBars.B | SegmentBars.C | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['I'] = SegmentBars.E | SegmentBars.F,
        ['J'] = SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.E,
        ['K'] = SegmentBars.A | SegmentBars.C | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['L'] = SegmentBars.D | SegmentBars.E | SegmentBars.F,
        ['M'] = SegmentBars.A | SegmentBars.C | SegmentBars.E,
        ['N'] = SegmentBars.C | SegmentBars.E | SegmentBars.G,
        ['O'] = SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.G,
        ['P'] = SegmentBars.A | SegmentBars.B | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['Q'] = SegmentBars.A | SegmentBars.B | SegmentBars.C | SegmentBars.F | SegmentBars.G,
        ['R'] = SegmentBars.E | SegmentBars.G,
        ['S'] = SegmentBars.A | SegmentBars.C | SegmentBars.D | SegmentBars.F | SegmentBars.G,
        ['T'] = SegmentBars.D | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['U'] = SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.E | SegmentBars.F,
        ['V'] = SegmentBars.C | SegmentBars.D | SegmentBars.E,
        ['W'] = SegmentBars.B | SegmentBars.D | SegmentBars.F,
        ['X'] = SegmentBars.B | SegmentBars.C | SegmentBars.E | SegmentBars.F | SegmentBars.G,
        ['Y'] = SegmentBars.B | SegmentBars.C | SegmentBars.D | SegmentBars.F | SegmentBars.G,
        ['Z'] = SegmentBars.A | SegmentBars.B | SegmentBars.D | SegmentBars.E | SegmentBars.G,
        ['-'] = SegmentBars.G,
        [' '] = SegmentBars.None,
    };

    static List<SegmentBars> ToCells(string text)
    {
        var cells = new List<SegmentBars>();
        foreach (var ch in text)
            if (ch == '.' && cells.Count > 0) cells[^1] |= SegmentBars.Dot;
            else cells.Add(Glyphs[ch]);
        return cells;
    }

    public void Draw(Span<byte> buffer, DigitSlot[] window, string text)
    {
        var cells = ToCells(text);
        if (cells.Count > window.Length)
            throw new ArgumentException($"'{text}' doesn't fit");

        var pad = window.Length - cells.Count;
        for (var i = 0; i < cells.Count; i++)
        {
            var slot = window[pad + i];
            for (var k = 0; k < 8; k++)
                if (((int)cells[i] & (1 << k)) != 0 && slot.Bars[k] is { } at)
                    buffer[at.Byte] |= at.Mask;
        }
    }

    public IReadOnlyList<HidOutputReport> Encode(IReadOnlyDictionary<OutputId, int> state)
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }
}