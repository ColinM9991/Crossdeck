using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Core.Tests.Devices.Display.Models;

public sealed class TextPaletteTests
{
    [Theory, AutoNSubstituteData]
    public void Indexer_ForEachKnownColour_ReturnsMatchingPaletteProperty(TextPalette palette)
    {
        // Act / Assert
        Assert.Equal(palette.Black, palette[TextColour.Black]);
        Assert.Equal(palette.Amber, palette[TextColour.Amber]);
        Assert.Equal(palette.White, palette[TextColour.White]);
        Assert.Equal(palette.Cyan, palette[TextColour.Cyan]);
        Assert.Equal(palette.Green, palette[TextColour.Green]);
        Assert.Equal(palette.Magenta, palette[TextColour.Magenta]);
        Assert.Equal(palette.Red, palette[TextColour.Red]);
        Assert.Equal(palette.Yellow, palette[TextColour.Yellow]);
        Assert.Equal(palette.Brown, palette[TextColour.Brown]);
        Assert.Equal(palette.Gray, palette[TextColour.Gray]);
        Assert.Equal(palette.Khaki, palette[TextColour.Khaki]);
    }

    [Theory, AutoNSubstituteData]
    public void Indexer_WithUnmappedColour_ThrowsArgumentOutOfRangeException(TextPalette palette)
    {
        // Arrange
        const TextColour unmapped = (TextColour)999;

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => palette[unmapped]);
    }
}
