using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Core.Tests.Devices.Display.Models;

public sealed class TextScreenTests
{
    [Fact]
    public void WaitingForData_Always_HasOneCellPerRowAndColumn()
    {
        // Act
        var screen = TextScreen.WaitingForData;

        // Assert
        Assert.Equal(TextScreen.CellsCount, screen.Cells.Length);
    }

    [Fact]
    public void WaitingForData_Always_CentresTextOnMiddleRowInWhiteOnBlackLargeText()
    {
        // Arrange
        const string text = "Waiting for data...";
        const int row = (TextScreen.Rows - 1) / 2;
        var column = (TextScreen.Columns - text.Length) / 2;

        // Act
        var screen = TextScreen.WaitingForData;

        // Assert
        for (var i = 0; i < text.Length; i++)
        {
            var cell = screen.Cells[row * TextScreen.Columns + column + i];

            Assert.Equal(text[i], cell.Character);
            Assert.Equal(TextColour.White, cell.Foreground);
            Assert.Equal(TextColour.Black, cell.Background);
            Assert.Equal(TextSize.Large, cell.Size);
        }
    }

    [Fact]
    public void WaitingForData_Always_LeavesCellsOutsideTheTextBlank()
    {
        // Arrange
        const string text = "Waiting for data...";
        const int row = (TextScreen.Rows - 1) / 2;
        var column = (TextScreen.Columns - text.Length) / 2;
        var textRangeStart = row * TextScreen.Columns + column;
        var textRangeEnd = textRangeStart + text.Length;

        // Act
        var screen = TextScreen.WaitingForData;

        // Assert
        var blankCells = screen.Cells.Where((_, i) => i < textRangeStart || i >= textRangeEnd);
        Assert.All(blankCells, cell => Assert.Equal(' ', cell.Character));
    }
}
