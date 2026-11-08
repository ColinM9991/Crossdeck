namespace Crossdeck.Core.Devices.Display.Models;

public sealed record TextScreen(TextCell[] Cells)
{
    public const int Rows = 14;
    public const int Columns = 24;
    public const int CellsCount = Rows * Columns;

    public static readonly TextScreen WaitingForData = Centred("Waiting for data...");

    private static TextScreen Centred(string text)
    {
        var blank = new TextCell(' ', TextColour.White, TextColour.Black, TextSize.Large);
        var cells = Enumerable.Repeat(blank, Rows * Columns).ToArray();

        const int row = (Rows - 1) / 2;
        var column = (Columns - text.Length) / 2;

        for (var i = 0; i < text.Length; i++)
            cells[row * Columns + column + i] =
                blank with { Character = text[i] };

        return new TextScreen(cells);
    }
}