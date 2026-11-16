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
        var hidInterfaces = DeviceNode.EnumerateHidInterfaces(rootDevInst, rootId)
            .Select(x => new HidInterfaceDescriptor(x.InterfaceNumber, x.HidPath))
            .OrderBy(x => x.Key)
            .ToList();

        // A single-interface device has no MI_ child nodes, and its interface number is 0 by definition.
        if (hidInterfaces.Count == 0)
            hidInterfaces = [new HidInterfaceDescriptor(0, hidPath)];

        return new UsbDeviceDescriptor(
            new UsbDeviceIdentity(vendorId, productId, serial, productName),
            hidInterfaces,
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
