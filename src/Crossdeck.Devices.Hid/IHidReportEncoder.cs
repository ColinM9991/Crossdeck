using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

/// <summary>
/// Encodes output state into HID reports.
/// </summary>
public interface IHidReportEncoder
{
    /// <summary>
    /// Encodes the current output state into HID reports.
    /// </summary>
    /// <param name="state">The current output values, keyed by output identity.</param>
    /// <returns>The HID reports to send to the device.</returns>
    IReadOnlyList<HidOutputReport> Encode(IReadOnlyDictionary<OutputId, int> state);

    /// <summary>
    /// Resets the encoder to its initial state.
    /// </summary>
    void Reset();
}