namespace Crossdeck.Testing;

/// <summary>
/// Known-valid HID report descriptor bytes for test data that must parse in <c>HidSharp.Reports.ReportDescriptor</c>.
/// </summary>
public static class TestReportDescriptor
{
    /// <summary>
    /// A valid Moza FCD button report descriptor on the buttons usage page (0x0D), which <c>HidControlFactory</c>
    /// does not recognise, so a decoder built from it exposes no controls.
    /// </summary>
    public static readonly byte[] Fcd =
    [
        0x05, 0x0D,        // Usage Page (Buttons)
        0xA1, 0x01,        // Collection (Application)
        0x09, 0x01,        // Usage (Button 1)
        0x15, 0x00,        // Logical Minimum (0)
        0x25, 0x01,        // Logical Maximum (1)
        0x35, 0x01,        // Physical Minimum (1)
        0x45, 0x01,        // Physical Maximum (1)
        0x75, 0x01,        // Report Size (1)
        0x95, 0x01,        // Report Count (1)
        0x81, 0x02,        // Input (Data, Variable, Absolute)
        0xC0,              // End Collection
    ];

    /// <summary>
    /// A valid button report descriptor with one button on the buttons usage page (0x09), which
    /// <c>HidControlFactory</c> recognises as a button control.
    /// </summary>
    public static readonly byte[] Button =
    [
        0x05, 0x09,        // Usage Page (Buttons)
        0xA1, 0x01,        // Collection (Application)
        0x09, 0x01,        // Usage (Button 1)
        0x15, 0x00,        // Logical Minimum (0)
        0x25, 0x01,        // Logical Maximum (1)
        0x35, 0x01,        // Physical Minimum (1)
        0x45, 0x01,        // Physical Maximum (1)
        0x75, 0x01,        // Report Size (1)
        0x95, 0x01,        // Report Count (1)
        0x81, 0x02,        // Input (Data, Variable, Absolute)
        0xC0,              // End Collection
    ];
}
