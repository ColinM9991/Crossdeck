using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Channels;

internal static class McduFrameEncoder
{
    private const byte KeyframePackage = 0x30;
    private const byte DeltaPackage = 0x31;
    private const byte InitConfigPackage = 0x32;
    public const byte ClientCapabilityPackage = 0x33;

    private const byte FrameVersion = 2;
    private const byte TextFlag = 0x01;
    private const byte StyleFlag = 0x02;
    private const byte CompressedFlag = 0x08;
    private const byte LargeFont = 0x80;
    private const byte WhiteColourCode = 7;

    public static readonly IReadOnlyList<int> AllRows = [.. Enumerable.Range(0, TextScreen.Rows)];

    public static byte[] InitConfig() => NetworkPackage.Encode(InitConfigPackage,
        [0x02, 0x0E, 0x00, 0x18, 0x00, 0x02, 0x00, 0x00, 0x5B, 0x23, 0xEE, 0x02]);

    public static byte[] Encode(uint sequence, TextScreen screen, IReadOnlyList<int> rows, bool keyframe)
    {
        var body = EncodeBody(screen, rows);
        var payload = keyframe ? QCompress(body) : body;

        var frame = new byte[6 + payload.Length];
        frame[0] = FrameVersion;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(1), sequence);
        frame[5] = (byte)(TextFlag | StyleFlag | (keyframe ? CompressedFlag : 0));
        payload.CopyTo(frame.AsSpan(6));
        return NetworkPackage.Encode(keyframe ? KeyframePackage : DeltaPackage, frame);
    }

    private static byte[] EncodeBody(TextScreen screen, IReadOnlyList<int> rows)
    {
        var body = new ArrayBufferWriter<byte>();
        WriteTextRows(body, screen, rows);
        WriteStyleRows(body, screen, rows);
        return body.WrittenSpan.ToArray();
    }

    public static List<int> ChangedRows(TextScreen previous, TextScreen current)
    {
        var rows = new List<int>();
        for (var row = 0; row < TextScreen.Rows; row++)
        {
            if (RowChanged(previous, current, row))
                rows.Add(row);
        }

        return rows;
    }

    private static byte Style(TextCell cell) =>
        (byte)((cell.Size == TextSize.Large ? LargeFont : 0) | ColourCode(cell.Foreground));

    private static byte ColourCode(TextColour colour) => colour switch
    {
        TextColour.Black => 0,
        TextColour.Cyan => 1,
        TextColour.Red => 2,
        TextColour.Yellow => 3,
        TextColour.Green => 4,
        TextColour.Magenta => 5,
        TextColour.Amber => 6,
        TextColour.White => 7,
        TextColour.Gray or TextColour.Brown or TextColour.Khaki => 8,
        _ => WhiteColourCode
    };

    private static void WriteTextRows(ArrayBufferWriter<byte> body, TextScreen screen, IReadOnlyList<int> rows)
    {
        body.Write<byte>([(byte)rows.Count]);
        foreach (var row in rows)
        {
            var utf8 = Encoding.UTF8.GetBytes(RowText(screen, row));
            body.Write<byte>([(byte)row, 1, 0, (byte)TextScreen.Columns, (byte)utf8.Length, (byte)(utf8.Length >> 8)]);
            body.Write(utf8);
        }
    }

    private static void WriteStyleRows(ArrayBufferWriter<byte> body, TextScreen screen, IReadOnlyList<int> rows)
    {
        body.Write<byte>([(byte)rows.Count]);
        foreach (var row in rows)
        {
            body.Write<byte>([(byte)row, 1, 0, (byte)TextScreen.Columns]);
            for (var column = 0; column < TextScreen.Columns; column++)
                body.Write<byte>([Style(screen.Cells[row * TextScreen.Columns + column])]);
        }
    }

    private static bool RowChanged(TextScreen previous, TextScreen current, int row)
    {
        var start = row * TextScreen.Columns;
        for (var column = 0; column < TextScreen.Columns; column++)
        {
            if (previous.Cells[start + column] != current.Cells[start + column])
                return true;
        }

        return false;
    }

    private static string RowText(TextScreen screen, int row)
    {
        Span<char> text = stackalloc char[TextScreen.Columns];
        for (var column = 0; column < TextScreen.Columns; column++)
            text[column] = screen.Cells[row * TextScreen.Columns + column].Character;
        return new string(text);
    }

    private static byte[] QCompress(ReadOnlySpan<byte> raw)
    {
        using var stream = new MemoryStream();
        Span<byte> size = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(size, (uint)raw.Length);
        stream.Write(size);
        using (var zlib = new ZLibStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(raw);
        return stream.ToArray();
    }
}