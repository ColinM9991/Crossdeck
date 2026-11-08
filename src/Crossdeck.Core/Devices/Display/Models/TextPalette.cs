namespace Crossdeck.Core.Devices.Display.Models;

public sealed record TextPalette(
    Rgb Black,
    Rgb Amber,
    Rgb White,
    Rgb Cyan,
    Rgb Green,
    Rgb Magenta,
    Rgb Red,
    Rgb Yellow,
    Rgb Brown,
    Rgb Gray,
    Rgb Khaki)
{
    public static readonly TextPalette Default = new(
        new Rgb(0x00, 0x00, 0x00), new Rgb(0xFF, 0xA5, 0x00), new Rgb(0xFF, 0xFF, 0xFF),
        new Rgb(0x00, 0xFF, 0xFF), new Rgb(0x00, 0xFF, 0x3D), new Rgb(0xFF, 0x63, 0xFF),
        new Rgb(0xFF, 0x00, 0x00), new Rgb(0xFF, 0xFF, 0x00), new Rgb(0x61, 0x5C, 0x42),
        new Rgb(0x77, 0x77, 0x77), new Rgb(0x79, 0x73, 0x5E));

    public Rgb this[TextColour colour] => colour switch
    {
        TextColour.Black => Black,
        TextColour.Amber => Amber,
        TextColour.White => White,
        TextColour.Cyan => Cyan,
        TextColour.Green => Green,
        TextColour.Magenta => Magenta,
        TextColour.Red => Red,
        TextColour.Yellow => Yellow,
        TextColour.Brown => Brown,
        TextColour.Gray => Gray,
        TextColour.Khaki => Khaki,
        _ => throw new ArgumentOutOfRangeException(nameof(colour)),
    };
}