using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Serial;

public interface ISerialTransport : IDeviceTransport
{
    string PortName { get; }

    void Open();
}