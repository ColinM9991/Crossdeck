using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Crossdeck.Devices.Interop;

[SupportedOSPlatform("windows")]
public static partial class CfgMgr32
{
    public const uint CrSuccess = 0;

    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Parent")]
    public static partial uint CM_Get_Parent(out uint parent, uint devInst, uint flags);

    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_IDW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint CM_Get_Device_IDW(
        uint devInst, [MarshalAs(UnmanagedType.LPArray)] char[] buffer, uint bufferLen, uint flags);
}
