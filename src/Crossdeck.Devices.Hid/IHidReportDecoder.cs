using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

/// <summary>
/// Decodes HID input reports into input changes.
/// </summary>
public interface IHidReportDecoder
{
    /// <summary>
    /// The controls available on the device.
    /// </summary>
    IReadOnlyList<DeviceControl> Controls { get; }

    /// <summary>
    /// Decodes an input report and appends the input changes it contains.
    /// </summary>
    /// <param name="report">The HID report to decode.</param>
    /// <param name="inputChanges">The list to append the input changes to.</param>
    void Decode(byte[] report, List<HidInputChange> inputChanges);
}