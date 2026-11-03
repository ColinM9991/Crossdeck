using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Hid;

internal interface IHidTransportFactory
{
    Task<HidConnection> CreateAsync(DeviceDescriptor deviceDescriptor, CancellationToken cancellationToken);
}