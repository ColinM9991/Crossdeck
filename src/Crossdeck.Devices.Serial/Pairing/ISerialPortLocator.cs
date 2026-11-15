using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Serial.Pairing;

/// <summary>
/// Locates the serial port paired with an HID device.
/// </summary>
public interface ISerialPortLocator
{
    /// <summary>
    /// Finds the serial port paired with the given HID device.
    /// </summary>
    /// <param name="hid">The HID device to find the paired serial port for.</param>
    /// <returns>The name of the paired serial port, or null if none was found.</returns>
    string? FindSiblingPort(DeviceDescriptor hid);
}