namespace Crossdeck.Devices.Serial;

public interface ISerialTransportFactory
{
    ISerialTransport? Create(SerialSettings serialSettings);
}