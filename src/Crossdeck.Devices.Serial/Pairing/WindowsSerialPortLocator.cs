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

        var usbParentId = GetUsbParentId(devInst.Value);
        return usbParentId is null ? null : FindMatchingPort(usbParentId);
    }

    private static string? GetUsbParentId(uint devInst) => DeviceNode.Walk(devInst, 10)
        .Select(DeviceNode.GetDeviceId).OfType<string>().FirstOrDefault(id =>
            id.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase) &&
            !id.Contains("&MI_", StringComparison.OrdinalIgnoreCase));

    private static string? FindMatchingPort(string usbParentId)
    {
        foreach (var (devInst, portName) in DeviceNode.EnumeratePorts())
        {
            if (!string.Equals(GetUsbParentId(devInst), usbParentId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (portName is not null) return portName;
        }

        return null;
    }
}