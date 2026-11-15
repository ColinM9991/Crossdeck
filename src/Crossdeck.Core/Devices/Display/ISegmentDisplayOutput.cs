using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices.Display;

/// <summary>
/// Renders text to segment displays.
/// </summary>
public interface ISegmentDisplayOutput
{
    /// <summary>
    /// Renders text on a segment display.
    /// </summary>
    /// <param name="id">The identity of the display to render on.</param>
    /// <param name="text">The text to render.</param>
    void SetText(OutputId id, string text);
}