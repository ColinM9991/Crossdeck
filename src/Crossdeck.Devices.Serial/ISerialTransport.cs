using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Serial;

/// <summary>
/// Provides a serial transport for a device.
/// </summary>
public interface ISerialTransport : IDeviceTransport
{
    /// <summary>
    /// The name of the serial port.
    /// </summary>
    string PortName { get; }

    /// <summary>
    /// Opens the serial port.
    /// </summary>
    void Open();
}