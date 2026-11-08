using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Devices.WinCtrl.Encoders;

internal sealed class WinCtrlWinCtrlDisplayEncoder : IWinCtrlDisplayEncoder
{
    private const int F0StreamSize = 56;
    private const int F2StreamSize = 63;
    private const int NumberOfOperations = 3;

    private readonly WinCtrlDefinition _definition;
    private readonly uint[] _paletteArgb;

    private uint _id = 1;
    private byte _sequence = 1;

    public WinCtrlWinCtrlDisplayEncoder(
        WinCtrlDefinition definition,
        TextPalette palette)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _paletteArgb = [.. Enum.GetValues<TextColour>().Select(c => ToArgb(palette[c]))];
    }

    public void Reset()
    {
        _id = 1;
        _sequence = 1;
    }

    public byte[][] EncodeInit()
    {
        var id = _id++;
        ulong recordNumber = 1;

        List<byte[]> list =
        [
            Record(WinCtrlCduCommands.ClearStyleTable, id, []),
            Record(WinCtrlCduCommands.TextGrid, id, Grid(_definition)),
            Record(WinCtrlCduCommands.AddStyle, id,
                Style(WinCtrlCduStyleFeatures.Font, _definition.LargeFontId, ++recordNumber)),
            Record(WinCtrlCduCommands.AddStyle, id,
                Style(WinCtrlCduStyleFeatures.Font, _definition.SmallFontId, ++recordNumber)),
            .. _paletteArgb.Select(c =>
                Record(WinCtrlCduCommands.AddStyle, id, Style(WinCtrlCduStyleFeatures.TextColour, c, ++recordNumber))),
            .. _paletteArgb.Select(c => Record(WinCtrlCduCommands.AddStyle, id,
                Style(WinCtrlCduStyleFeatures.BackgroundColour, c, ++recordNumber))),
            .. Enumerable.Range(0, NumberOfOperations)
                .Select(op => Record(WinCtrlCduCommands.AddStyle, id,
                    Style(WinCtrlCduStyleFeatures.Operations, (uint)op, ++recordNumber))),
            Record(WinCtrlCduCommands.StyleIndexWidth, id, [02]),
            Record(WinCtrlCduCommands.BuildStyleTable, id, []),
            Record(WinCtrlCduCommands.FontColour, id, [0xFF, 0x00, 0x00, 0x00]),
            Record(WinCtrlCduCommands.BackgroundColour, id, [0xFF, 0x00, 0x00, 0x00]),
            Record(WinCtrlCduCommands.FillRectangle, id, Rect(0, 0, 640, 480)),
            Record(WinCtrlCduCommands.RefreshLcd, id, [])
        ];

        var records = list.SelectMany(x => x).ToArray();
        return Packetise(
            records.AsSpan(),
            WinCtrlCduReports.Setup,
            F0StreamSize,
            4,
            (buffer, length) =>
            {
                buffer[1] = 0x00;
                buffer[2] = _sequence++;
                buffer[3] = (byte)length;
            });
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

        return Packetise(buffer.WrittenSpan, WinCtrlCduReports.Screen, F2StreamSize, 1);
    }

    private static byte[] Grid(WinCtrlDefinition definition)
    {
        var bytes = new byte[8];

        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0), definition.Left);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2), definition.Top);
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

    private static byte[][] Packetise(
        ReadOnlySpan<byte> data,
        byte id,
        int packetSize,
        int offset,
        Action<byte[], int>? packetAction = null)
    {
        var numberOfPackets = (data.Length + packetSize - 1) / packetSize;
        var packets = new byte[numberOfPackets][];

        for (var i = 0; i < numberOfPackets; i++)
        {
            var start = i * packetSize;
            var length = Math.Min(packetSize, data.Length - start);

            var buffer = new byte[64];
            buffer[0] = id;

            packetAction?.Invoke(buffer, length);

            data.Slice(start, length).CopyTo(buffer.AsSpan(offset));
            packets[i] = buffer;
        }

        return packets;
    }

    private byte[] Record(uint command, uint id, byte[] payload)
    {
        const int recordHeaderSize = 17;
        var record = new byte[recordHeaderSize + payload.Length];
        record[0] = _definition.TargetLo;
        record[1] = _definition.TargetHi;

        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(4), command);
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(8), id);

        record[12] = 0x00;

        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(13), (uint)payload.Length);

        payload.CopyTo(record.AsSpan(recordHeaderSize));

        return record;
    }
}