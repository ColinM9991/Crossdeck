using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Serial.Pairing;

internal sealed class NullSerialPortLocator : ISerialPortLocator
{
    public string? FindSiblingPort(DeviceDescriptor hid)
    {
        return null;
    }
}