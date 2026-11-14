namespace Crossdeck.Testing;

/// <summary>
/// Known-valid HID report descriptor bytes for test data that must parse in <c>HidSharp.Reports.ReportDescriptor</c>.
/// </summary>
public static class TestReportDescriptor
{
    /// <summary>
    /// A valid Moza FCD button report descriptor.
    /// </summary>
    public static readonly byte[] Fcd =
    [
        0x05, 0x0D,         // Usage Page (Buttons)
        0x09, 0x01,         // Usage (Button 1)
        0x15, 0x00,         // Logical Minimum (0)
        0x25, 0x01,         // Logical Maximum (1)
        0x19, 0x01,         // Physical Minimum (1)
        0x2A, 0x01, 0x00,   // Physical Maximum (1)
        0x75, 0x01,         // Report Size (1)
        0x95, 0x01,         // Report Count (1)
        0x81, 0x02,         // Input (Data, Variable, Absolute)
    ];
}
