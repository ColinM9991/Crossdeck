using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Interop;

internal sealed class NullUsbDeviceResolver : IUsbDeviceResolver
{
    public UsbDeviceDescriptor? Resolve(string hidPath)
    {
        return null;
    }
}
