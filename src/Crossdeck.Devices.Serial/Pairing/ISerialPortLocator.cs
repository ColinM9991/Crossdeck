using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Serial.Pairing;

public interface ISerialPortLocator
{
    string? FindSiblingPort(DeviceDescriptor hid);
}