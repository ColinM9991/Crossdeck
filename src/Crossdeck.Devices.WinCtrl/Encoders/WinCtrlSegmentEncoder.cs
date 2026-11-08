using Crossdeck.Devices.WinCtrl.Models;

namespace Crossdeck.Devices.WinCtrl.Encoders;

public sealed class WinCtrlSegmentEncoder(IReadOnlyList<SegmentPanel> panels)
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

    private readonly Dictionary<string, WinCtrlF0Stream> _streams =
        panels.ToDictionary(p => p.Name, p => new WinCtrlF0Stream(p.Target));

    private readonly Dictionary<string, byte[]> _lastSent = new();

    public byte[][] EncodeFrame(IReadOnlyDictionary<string, string> texts, IReadOnlySet<string> flags)
    {
        var packets = new List<byte[]>();

        foreach (var panel in panels)
        {
            var buffer = new byte[panel.BufferSize];

            foreach (var (name, window) in panel.Windows)
                if (texts.TryGetValue($"{panel.Name}.{name}", out var text))
                    DrawWindow(buffer, window, text);

            foreach (var (name, addresses) in panel.Labels)
                if (flags.Contains($"{panel.Name}.{name}"))
                    foreach (var address in addresses)
                        buffer[address.Byte] |= address.Mask;

            if (_lastSent.TryGetValue(panel.Name, out var last) && last.AsSpan().SequenceEqual(buffer))
                continue;

            _lastSent[panel.Name] = buffer;
            packets.AddRange(Wrap(panel, buffer));
        }

        return packets.ToArray();
    }

    private byte[][] Wrap(SegmentPanel panel, byte[] buffer)
    {
        var stream = _streams[panel.Name];
        var payload = new byte[panel.Framing.PayloadLength];
        buffer.AsSpan().CopyTo(payload.AsSpan(4));

        var write = stream.Record(0x0102, panel.Framing.WriteId, payload);
        var commit = stream.Record(0x0103, panel.Framing.CommitId, []);

        return panel.Framing.CommitInOwnPacket
            ? stream.PacketisePerRecord([write, commit])
            : stream.Packetise([.. write, .. commit]);
    }

    private static void DrawWindow(Span<byte> buffer, SegmentWindow window, string text)
    {
        var sign = text.Length > 0 && text[0] is '-' or '+' ? text[0] : (char?)null;
        var body = (sign is null ? text : text[1..]).ToUpperInvariant();

        if (sign == '-' && window.Minus is { } minus)
            foreach (var address in minus) buffer[address.Byte] |= address.Mask;
        if (sign == '+' && window.Plus is { } plus)
            foreach (var address in plus) buffer[address.Byte] |= address.Mask;

        Draw(buffer, window.Digits, body);
    }

    private static void Draw(Span<byte> buffer, DigitSlot[] window, string text)
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

    private static List<SegmentBars> ToCells(string text)
    {
        var cells = new List<SegmentBars>();
        foreach (var ch in text)
            if (ch == '.' && cells.Count > 0) cells[^1] |= SegmentBars.Dot;
            else cells.Add(Glyphs[ch]);
        return cells;
    }
}
