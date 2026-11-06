namespace Crossdeck.Core.Devices.Models;

public sealed record DeviceDescriptor(
    DeviceIdentity Identity,
    DeviceKind Kind,
    string Path,
    string? ProductName);
