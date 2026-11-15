using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

/// <summary>
/// Creates transports for HID devices.
/// </summary>
public interface IHidTransportFactory
{
    /// <summary>
    /// Creates a transport for the HID device at the given path.
    /// </summary>
    /// <param name="path">The path of the HID device to connect to.</param>
    /// <returns>A connection to the HID device.</returns>
    Task<HidConnection> CreateAsync(string path);
}