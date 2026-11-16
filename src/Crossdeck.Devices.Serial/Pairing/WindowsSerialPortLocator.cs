using System.Runtime.Versioning;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Interop;

namespace Crossdeck.Devices.Serial.Pairing;

[SupportedOSPlatform("windows")]
internal sealed class WindowsSerialPortLocator : ISerialPortLocator
{
    public string? FindSiblingPort(DeviceDescriptor hid)
    {
        if (!OperatingSystem.IsWindows()) return null;

        var devInst = DeviceNode.ResolveInterfaceDevInst(hid.Path);
        if (devInst is null) return null;

        var usbParentId = DeviceNode.GetUsbParentId(devInst.Value);
        return usbParentId is null ? null : FindMatchingPort(usbParentId);
    }

    private static string? FindMatchingPort(string usbParentId)
    {
        foreach (var (devInst, portName) in DeviceNode.EnumeratePorts())
        {
            if (portName is null) continue;
            if (!string.Equals(DeviceNode.GetUsbParentId(devInst), usbParentId, StringComparison.OrdinalIgnoreCase))
                continue;

            return portName;
        }

        return null;
    }
}