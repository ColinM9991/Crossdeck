namespace Crossdeck.Core.Devices.Models;

public sealed record DeviceDescriptor(
    DeviceIdentity Identity,
    string Path,
    uint[] Usages,
    int? VendorId,
    int? ProductId,
    string? SerialNumber,
    string? ProductName);
