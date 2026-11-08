using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Devices.WinCtrl.Encoders;

internal sealed class WinCtrlWinCtrlDisplayEncoder : IWinCtrlDisplayEncoder
{
    private const int F2StreamSize = 63;
    private const int NumberOfOperations = 3;

    private readonly CduScreenSettings _cdu;
    private readonly WinCtrlF0Stream _stream;
    private readonly uint[] _paletteArgb;

    private uint _id = 1;

    public WinCtrlWinCtrlDisplayEncoder(
        WinCtrlDefinition definition,
        TextPalette palette)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _cdu = definition.Cdu ?? throw new ArgumentException($"'{definition.Part}' has no CDU screen settings", nameof(definition));
        _stream = new WinCtrlF0Stream(definition.BasePid);
        _paletteArgb = [.. Enum.GetValues<TextColour>().Select(c => ToArgb(palette[c]))];
    }

    public void Reset()
    {
        _id = 1;
        _stream.Reset();
    }

    public byte[][] EncodeInit()
    {
        var id = _id++;
        ulong recordNumber = 1;

        List<byte[]> list =
        [
            _stream.Record(WinCtrlCduCommands.ClearStyleTable, id, []),
            _stream.Record(WinCtrlCduCommands.TextGrid, id, Grid(_cdu)),
            _stream.Record(WinCtrlCduCommands.AddStyle, id,
                Style(WinCtrlCduStyleFeatures.Font, _cdu.LargeFontId, ++recordNumber)),
            _stream.Record(WinCtrlCduCommands.AddStyle, id,
                Style(WinCtrlCduStyleFeatures.Font, _cdu.SmallFontId, ++recordNumber)),
            .. _paletteArgb.Select(c =>
                _stream.Record(WinCtrlCduCommands.AddStyle, id, Style(WinCtrlCduStyleFeatures.TextColour, c, ++recordNumber))),
            .. _paletteArgb.Select(c => _stream.Record(WinCtrlCduCommands.AddStyle, id,
                Style(WinCtrlCduStyleFeatures.BackgroundColour, c, ++recordNumber))),
            .. Enumerable.Range(0, NumberOfOperations)
                .Select(op => _stream.Record(WinCtrlCduCommands.AddStyle, id,
                    Style(WinCtrlCduStyleFeatures.Operations, (uint)op, ++recordNumber))),
            _stream.Record(WinCtrlCduCommands.StyleIndexWidth, id, [02]),
            _stream.Record(WinCtrlCduCommands.BuildStyleTable, id, []),
            _stream.Record(WinCtrlCduCommands.FontColour, id, [0xFF, 0x00, 0x00, 0x00]),
            _stream.Record(WinCtrlCduCommands.BackgroundColour, id, [0xFF, 0x00, 0x00, 0x00]),
            _stream.Record(WinCtrlCduCommands.FillRectangle, id, Rect(0, 0, 640, 480)),
            _stream.Record(WinCtrlCduCommands.RefreshLcd, id, [])
        ];

        var records = list.SelectMany(x => x).ToArray();
        return _stream.Packetise(records);
    }

    public byte[][] EncodeFrame(TextScreen screen)
    {
        var buffer = new ArrayBufferWriter<byte>(F2StreamSize * 4);
        Span<byte> utf8 = stackalloc byte[4];

        for (var i = 0; i < TextScreen.CellsCount; i++)
        {
            var screenCell = screen.Cells[i];
            var op = i switch
            {
                0 => 1,
                TextScreen.CellsCount - 1 => 2,
                _ => 0,
            };

            BinaryPrimitives.WriteUInt16LittleEndian(buffer.GetSpan(2), Style(screenCell, op));
            buffer.Advance(2);

            var character = screenCell.Character == '\0' ? ' ' : screenCell.Character;
            var length = new Rune(character).EncodeToUtf8(utf8);
            buffer.Write(utf8[..length]);
        }

        return PacketiseFrame(buffer.WrittenSpan, WinCtrlCduReports.Screen, F2StreamSize, 1);
    }

    private static byte[] Grid(CduScreenSettings cdu)
    {
        var bytes = new byte[8];

        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0), cdu.Left);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2), cdu.Top);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4), TextScreen.Rows);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6), TextScreen.Columns);

        return bytes;
    }

    private static byte[] Rect(ushort x, ushort y, ushort width, ushort height)
    {
        var bytes = new byte[8];

        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0), x);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2), y);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4), width);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6), height);

        return bytes;
    }

    private static byte[] Style(ushort feature, uint value, ulong recordNum)
    {
        var bytes = new byte[14];

        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0), feature);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2), value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(6), recordNum);

        return bytes;
    }

    private static ushort Style(TextCell cell, int operation)
    {
        const int perFg = 11 * NumberOfOperations;
        const int perFont = 11 * perFg;

        var font = cell.Size is TextSize.Small ? 1 : 0;
        var foreground = (int)cell.Foreground;
        var background = (int)cell.Background;

        return (ushort)(font * perFont + foreground * perFg + background * NumberOfOperations + operation);
    }

    private static uint ToArgb(Rgb c) => 0xFF000000u | (uint)(c.R << 16 | c.G << 8 | c.B);

    private static byte[][] PacketiseFrame(
        ReadOnlySpan<byte> data,
        byte id,
        int packetSize,
        int offset)
    {
        var numberOfPackets = (data.Length + packetSize - 1) / packetSize;
        var packets = new byte[numberOfPackets][];

        for (var i = 0; i < numberOfPackets; i++)
        {
            var start = i * packetSize;
            var length = Math.Min(packetSize, data.Length - start);

            var buffer = new byte[64];
            buffer[0] = id;

            data.Slice(start, length).CopyTo(buffer.AsSpan(offset));
            packets[i] = buffer;
        }

        return packets;
    }
}
