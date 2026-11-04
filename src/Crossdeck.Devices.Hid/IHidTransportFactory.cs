using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

internal interface IHidTransportFactory
{
    Task<HidConnection> CreateAsync(DeviceDescriptor deviceDescriptor, CancellationToken cancellationToken);
}