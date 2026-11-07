namespace Crossdeck.Devices.Hid.Models;

public readonly record struct HidOutputReport(HidReportType Type, byte[] Data);