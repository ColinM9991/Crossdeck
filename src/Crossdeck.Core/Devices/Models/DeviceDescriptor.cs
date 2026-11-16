namespace Crossdeck.Core.Devices.Models;

public sealed record DeviceDescriptor(
    DeviceIdentity Identity,
    DeviceKind Kind,
    string Path,
    string? ProductName,
    IReadOnlyList<HidInterfaceDescriptor> HidInterfaces,
    IReadOnlyList<SerialInterfaceDescriptor> SerialInterfaces)
{
    public DeviceDescriptor(
        DeviceIdentity Identity,
        DeviceKind Kind,
        string Path,
        string? ProductName)
        : this(Identity, Kind, Path, ProductName, [], [])
    {
    }
}
