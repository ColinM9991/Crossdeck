using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Crossdeck.Devices.Interop;

[SupportedOSPlatform("windows")]
public static partial class SetupDi
{
    public const uint DigcfPresent = 0x02;
    public const uint DicsFlagGlobal = 0x01;
    public const uint DiregDev = 0x01;
    public const int KeyRead = 0x20019;
    public const uint SpdrpDeviceSerial = 17;

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetClassDevsW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint SetupDiGetClassDevsW(ref Guid classGuid, nint enumerator, nint hwndParent,
        uint flags);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetClassDevsW")]
    public static partial nint SetupDiGetAllClassDevs(nint classGuid, nint enumerator, nint hwndParent,
        uint flags);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiEnumDeviceInfo")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiEnumDeviceInfo(nint deviceInfoSet, uint memberIndex,
        ref SP_DEVINFO_DATA deviceInfoData);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiOpenDevRegKey")]
    public static partial nint SetupDiOpenDevRegKey(
        nint deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint scope, uint hwProfile, uint keyType,
        int samDesired);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceRegistryPropertyW",
        StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiGetDeviceRegistryPropertyW(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint regDataType,
        [MarshalAs(UnmanagedType.LPArray)] char[] propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiDestroyDeviceInfoList")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiCreateDeviceInfoList")]
    public static partial nint SetupDiCreateDeviceInfoList(nint classGuid, nint hwndParent);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiOpenDeviceInterfaceW",
        StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiOpenDeviceInterfaceW(
        nint deviceInfoSet,
        string devicePath,
        uint flags,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfaceDetailW",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiGetDeviceInterfaceDetailW(
        nint deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        nint deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        ref SP_DEVINFO_DATA deviceInfoData);
}

[SupportedOSPlatform("windows")]
[StructLayout(LayoutKind.Sequential)]
public struct SP_DEVICE_INTERFACE_DATA
{
    public uint cbSize;
    public Guid InterfaceClassGuid;
    public uint Flags;
    public nint Reserved;
}

[SupportedOSPlatform("windows")]
[StructLayout(LayoutKind.Sequential)]
public struct SP_DEVINFO_DATA
{
    public uint cbSize;
    public Guid ClassGuid;
    public uint DevInst;
    public nint Reserved;
}
