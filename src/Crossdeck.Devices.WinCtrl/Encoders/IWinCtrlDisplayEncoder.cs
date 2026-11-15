using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Devices.WinCtrl.Encoders;

/// <summary>
/// Encodes text screens into display frames for WinCtrl displays.
/// </summary>
public interface IWinCtrlDisplayEncoder
{
    /// <summary>
    /// Resets the encoder to its initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Encodes the initialisation frames for the display.
    /// </summary>
    /// <returns>The frames to send to the display before it can show content.</returns>
    byte[][] EncodeInit();

    /// <summary>
    /// Encodes a text screen into display frames.
    /// </summary>
    /// <param name="screen">The screen to encode.</param>
    /// <returns>The frames that represent the screen.</returns>
    byte[][] EncodeFrame(TextScreen screen);
}