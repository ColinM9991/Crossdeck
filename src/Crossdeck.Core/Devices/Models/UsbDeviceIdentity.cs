namespace Crossdeck.Core.Devices.Models;

public sealed record UsbDeviceIdentity(int VendorId, int ProductId, string? Serial, string? ProductName);
