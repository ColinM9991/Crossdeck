using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

internal interface IHidTransportFactory
{
    Task<HidConnection> CreateAsync(string path);
}