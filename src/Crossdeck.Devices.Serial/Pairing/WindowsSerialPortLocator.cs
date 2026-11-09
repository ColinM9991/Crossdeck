using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Crossdeck.Core.Devices.Models;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Crossdeck.Devices.Serial.Pairing;

[SupportedOSPlatform("windows")]
internal sealed partial class WindowsSerialPortLocator : ISerialPortLocator
{
    private const uint CrSuccess = 0;
    private const uint DigcfPresent = 0x02;
    private const uint DicsFlagGlobal = 0x01;
    private const uint DiregDev = 0x01;
    private const int KeyRead = 0x20019;
    private static readonly Guid PortsClassGuid = new("4d36e978-e325-11ce-bfc1-08002be10318");

    public string? FindSiblingPort(DeviceDescriptor hid)
    {
        if (!OperatingSystem.IsWindows()) return null;

        var instanceId = ToInstanceId(hid.Path);
        if (CM_Locate_DevNodeW(out var devInst, instanceId, 0) != CrSuccess) return null;

        var usbParentId = GetUsbParentId(devInst);
        return usbParentId is null ? null : FindMatchingPort(usbParentId);
    }

    private static string ToInstanceId(string hidPath)
    {
        var path = hidPath.StartsWith(@"\\?\", StringComparison.Ordinal) ? hidPath[4..] : hidPath;
        var hashIndex = path.LastIndexOf('#');
        if (hashIndex >= 0 && path.Length > hashIndex + 1 && path[hashIndex + 1] == '{')
            path = path[..hashIndex];
        return path.Replace('#', '\\');
    }

    private static string? GetUsbParentId(uint devInst)
    {
        var current = devInst;
        for (var i = 0; i < 10; i++)
        {
            var id = GetDeviceId(current);
            if (id is not null &&
                id.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase) &&
                !id.Contains("&MI_", StringComparison.OrdinalIgnoreCase))
                return id;

            if (CM_Get_Parent(out var parent, current, 0) != CrSuccess) return null;
            current = parent;
        }

        return null;
    }

    private static string? FindMatchingPort(string usbParentId)
    {
        var portsClass = PortsClassGuid;
        var handle = SetupDiGetClassDevsW(ref portsClass, null, nint.Zero, DigcfPresent);
        if (handle == nint.Zero || handle == new nint(-1)) return null;

        try
        {
            for (var index = 0u;; index++)
            {
                var data = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                if (!SetupDiEnumDeviceInfo(handle, index, ref data)) break;
                if (!string.Equals(GetUsbParentId(data.DevInst), usbParentId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (ReadPortName(handle, ref data) is { } portName) return portName;
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(handle);
        }

        return null;
    }

    private static string? ReadPortName(nint deviceInfoSet, ref SP_DEVINFO_DATA data)
    {
        var key = SetupDiOpenDevRegKey(deviceInfoSet, ref data, DicsFlagGlobal, 0, DiregDev, KeyRead);
        if (key == nint.Zero || key == new nint(-1)) return null;

        using var safeHandle = new SafeRegistryHandle(key, true);
        using var registryKey = RegistryKey.FromHandle(safeHandle);
        return registryKey.GetValue("PortName") as string;
    }

    private static string? GetDeviceId(uint devInst)
    {
        var buffer = new char[512];
        if (CM_Get_Device_IDW(devInst, buffer, (uint)buffer.Length, 0) != CrSuccess) return null;
        var nullIndex = Array.IndexOf(buffer, '\0');
        return new string(buffer, 0, nullIndex < 0 ? buffer.Length : nullIndex);
    }

    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Locate_DevNodeW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint CM_Locate_DevNodeW(out uint pdnDevInst, string deviceId, uint flags);

    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Parent")]
    private static partial uint CM_Get_Parent(out uint parent, uint devInst, uint flags);

    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_IDW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint CM_Get_Device_IDW(
        uint devInst, [MarshalAs(UnmanagedType.LPArray)] char[] buffer, uint bufferLen, uint flags);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetClassDevsW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint SetupDiGetClassDevsW(ref Guid classGuid, string? enumerator, nint hwndParent,
        uint flags);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiEnumDeviceInfo")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiEnumDeviceInfo(nint deviceInfoSet, uint memberIndex,
        ref SP_DEVINFO_DATA deviceInfoData);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiOpenDevRegKey")]
    private static partial nint SetupDiOpenDevRegKey(
        nint deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint scope, uint hwProfile, uint keyType,
        int samDesired);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiDestroyDeviceInfoList")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public nint Reserved;
    }
}