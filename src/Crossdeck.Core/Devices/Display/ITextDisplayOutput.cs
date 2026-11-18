using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Core.Devices.Display;

/// <summary>
/// Renders text screens to displays.
/// </summary>
public interface ITextDisplayOutput
{
    /// <summary>
    /// Gets the role of the display.
    /// </summary>
    DisplayRole Role { get; }

    /// <summary>
    /// Shows a screen on the display.
    /// </summary>
    /// <param name="screen">The screen to show.</param>
    void Show(TextScreen screen);
}