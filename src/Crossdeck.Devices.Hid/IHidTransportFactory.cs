using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

public interface IHidTransportFactory
{
    Task<HidConnection> CreateAsync(string path);
}