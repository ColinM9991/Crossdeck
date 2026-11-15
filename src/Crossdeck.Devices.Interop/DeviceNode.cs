using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Crossdeck.Devices.Interop;

[SupportedOSPlatform("windows")]
public static class DeviceNode
{
    private static readonly Guid PortsClassGuid = new("4d36e978-e325-11ce-bfc1-08002be10318");
    private const int ErrorInsufficientBuffer = 122;

    public static uint? ResolveInterfaceDevInst(string devicePath)
    {
        var handle = SetupDi.SetupDiCreateDeviceInfoList(nint.Zero, nint.Zero);

        if (handle == nint.Zero || handle == new nint(-1))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        try
        {
            var interfaceData = new SP_DEVICE_INTERFACE_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>()
            };

            if (!SetupDi.SetupDiOpenDeviceInterfaceW(handle, devicePath, 0, ref interfaceData))
                return null;

            var deviceInfo = new SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>()
            };

            var success = SetupDi.SetupDiGetDeviceInterfaceDetailW(
                handle,
                ref interfaceData,
                nint.Zero,
                0,
                out _,
                ref deviceInfo);

            if (success)
                return deviceInfo.DevInst;

            var error = Marshal.GetLastPInvokeError();

            return error != ErrorInsufficientBuffer ? throw new Win32Exception(error) : deviceInfo.DevInst;
        }
        finally
        {
            SetupDi.SetupDiDestroyDeviceInfoList(handle);
        }
    }

    public static IEnumerable<uint> Walk(uint devInst, int maxParentSteps)
    {
        yield return devInst;

        var current = devInst;
        for (var i = 0; i < maxParentSteps; i++)
        {
            if (CfgMgr32.CM_Get_Parent(out var parent, current, 0) != CfgMgr32.CrSuccess) yield break;
            current = parent;
            yield return current;
        }
    }

    public static string? GetDeviceId(uint devInst)
    {
        var buffer = new char[512];
        if (CfgMgr32.CM_Get_Device_IDW(devInst, buffer, (uint)buffer.Length, 0) != CfgMgr32.CrSuccess) return null;
        var nullIndex = Array.IndexOf(buffer, '\0');
        return new string(buffer, 0, nullIndex < 0 ? buffer.Length : nullIndex);
    }

    public static string? ReadDevNodeStringProperty(uint devInst, uint spdrpProperty)
    {
        var handle = SetupDi.SetupDiGetAllClassDevs(nint.Zero, nint.Zero, nint.Zero, SetupDi.DigcfPresent);
        if (handle == nint.Zero || handle == new nint(-1)) return null;

        try
        {
            for (var index = 0u;; index++)
            {
                var data = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                if (!SetupDi.SetupDiEnumDeviceInfo(handle, index, ref data)) break;
                if (data.DevInst != devInst) continue;

                return ReadStringProperty(handle, ref data, spdrpProperty);
            }
        }
        finally
        {
            SetupDi.SetupDiDestroyDeviceInfoList(handle);
        }

        return null;
    }

    public static string? ReadPortName(nint deviceInfoSet, ref SP_DEVINFO_DATA data)
    {
        var key = SetupDi.SetupDiOpenDevRegKey(
            deviceInfoSet, ref data, SetupDi.DicsFlagGlobal, 0, SetupDi.DiregDev, SetupDi.KeyRead);
        if (key == nint.Zero || key == new nint(-1)) return null;

        using var safeHandle = new SafeRegistryHandle(key, true);
        using var registryKey = RegistryKey.FromHandle(safeHandle);
        return registryKey.GetValue("PortName") as string;
    }

    public static IEnumerable<(uint DevInst, string? PortName)> EnumeratePorts()
    {
        var classGuid = PortsClassGuid;
        var handle = SetupDi.SetupDiGetClassDevsW(ref classGuid, nint.Zero, nint.Zero, SetupDi.DigcfPresent);
        if (handle == nint.Zero || handle == new nint(-1)) yield break;

        try
        {
            for (var index = 0u;; index++)
            {
                var data = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                if (!SetupDi.SetupDiEnumDeviceInfo(handle, index, ref data)) break;
                yield return (data.DevInst, ReadPortName(handle, ref data));
            }
        }
        finally
        {
            SetupDi.SetupDiDestroyDeviceInfoList(handle);
        }
    }

    private static string? ReadStringProperty(nint deviceInfoSet, ref SP_DEVINFO_DATA data, uint spdrpProperty)
    {
        var buffer = new char[128];

        while (true)
        {
            var bufferSize = (uint)buffer.Length * 2;
            var success = SetupDi.SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet, ref data, spdrpProperty, out _, buffer, bufferSize, out var requiredSize);
            if (success)
            {
                var nullIndex = Array.IndexOf(buffer, '\0');
                return new string(buffer, 0, nullIndex < 0 ? buffer.Length : nullIndex);
            }

            if (Marshal.GetLastPInvokeError() != ErrorInsufficientBuffer) return null;
            buffer = new char[requiredSize];
        }
    }
}
