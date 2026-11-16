namespace Crossdeck.Core.Devices.Models;

public sealed record UsbDeviceDescriptor(
    UsbDeviceIdentity Identity,
    IReadOnlyList<HidInterfaceDescriptor> HidInterfaces,
    IReadOnlyList<SerialInterfaceDescriptor> SerialInterfaces);
