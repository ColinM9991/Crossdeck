namespace Crossdeck.Devices.Serial;

/// <summary>
/// Creates transports for serial devices.
/// </summary>
public interface ISerialTransportFactory
{
    /// <summary>
    /// Creates a serial transport for the given settings.
    /// </summary>
    /// <param name="serialSettings">The settings to create the transport with.</param>
    /// <returns>A serial transport, or null if one cannot be created.</returns>
    ISerialTransport? Create(SerialSettings serialSettings);
}