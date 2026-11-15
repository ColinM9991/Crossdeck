namespace Crossdeck.Devices.WinCtrl.Frames;

/// <summary>
/// Provides display frames for a WinCtrl display.
/// </summary>
public interface IWinCtrlFrameSource
{
    /// <summary>
    /// Encodes the initialisation frames for the display.
    /// </summary>
    /// <returns>The frames to send to the display before it can show content.</returns>
    byte[][] EncodeInit();

    /// <summary>
    /// Takes the next display frame.
    /// </summary>
    /// <returns>The next frame to send to the display, or null when no frame is available.</returns>
    byte[][]? TakeFrame();
}
