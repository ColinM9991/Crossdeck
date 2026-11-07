using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Hid;

internal sealed class HidDeviceDriverFactory : IDeviceDriverFactory
{
    private readonly IHidTransportFactory _hidTransportFactory;

    public HidDeviceDriverFactory(
        IHidTransportFactory hidTransportFactory)
    {
        _hidTransportFactory = hidTransportFactory ?? throw new ArgumentNullException(nameof(hidTransportFactory));
    }

    public int Priority => 0;

    public bool CanCreate(DeviceDescriptor deviceDescriptor) =>
        deviceDescriptor.Kind is DeviceKind.Hid;

    public async Task<IDeviceDriver?> CreateAsync(
        DeviceDescriptor deviceDescriptor,
        CancellationToken cancellationToken)
    {
        try
        {
            var hidConnection = await _hidTransportFactory.CreateAsync(deviceDescriptor.Path);

            return new HidDeviceDriver(
                deviceDescriptor.Identity,
                hidConnection.Transport,
                new HidReportDecoder(hidConnection.ReportDescriptor));
        }
        catch (DeviceNotUsableException)
        {
            return null;
        }
        catch (DeviceNotFoundException)
        {
            return null;
        }
    }
}