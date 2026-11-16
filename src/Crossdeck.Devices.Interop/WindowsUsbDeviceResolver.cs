using System.Runtime.Versioning;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Interop;

[SupportedOSPlatform("windows")]
internal sealed class WindowsUsbDeviceResolver : IUsbDeviceResolver
{
    public UsbDeviceDescriptor? Resolve(string hidPath)
    {
        if (!OperatingSystem.IsWindows()) return null;

        var devInst = DeviceNode.ResolveInterfaceDevInst(hidPath);
        if (devInst is null) return null;

        var usbParent = DeviceNode.GetUsbParent(devInst.Value);
        if (usbParent is null) return null;

        var (rootDevInst, rootId) = usbParent.Value;
        var rootIdentity = DeviceNode.ParseUsbRootId(rootId);
        if (rootIdentity is null) return null;

        var (vendorId, productId, serial) = rootIdentity.Value;
        var productName = DeviceNode.ReadDevNodeStringProperty(rootDevInst, SetupDi.SpdrpDeviceDesc);

        return new UsbDeviceDescriptor(
            new UsbDeviceIdentity(vendorId, productId, serial, productName),
            [new HidInterfaceDescriptor(hidPath)],
            FindSiblingPorts(rootId));
    }

    private static IReadOnlyList<SerialInterfaceDescriptor> FindSiblingPorts(string usbParentId)
    {
        var ports = new List<SerialInterfaceDescriptor>();

        foreach (var (devInst, portName) in DeviceNode.EnumeratePorts())
        {
            if (portName is null) continue;
            if (!string.Equals(DeviceNode.GetUsbParentId(devInst), usbParentId, StringComparison.OrdinalIgnoreCase)) continue;

            ports.Add(new SerialInterfaceDescriptor(portName));
        }

        return ports;
    }
}
