using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Moza.Drivers;

namespace Crossdeck.Devices.Moza;

public sealed class MozaDeviceDriverFactory : IDeviceDriverFactory
{
    private readonly IHidTransportFactory _hidTransportFactory;
    
    public MozaDeviceDriverFactory(
        IHidTransportFactory hidTransportFactory)
    {
        _hidTransportFactory = hidTransportFactory ?? throw new ArgumentNullException(nameof(hidTransportFactory));
    }
    
    public int Priority => 101;

    public bool CanCreate(DeviceDescriptor deviceDescriptor) => MozaIds.IsFcdDisplay(deviceDescriptor.Identity.VendorId,
        deviceDescriptor.Identity.ProductId);

    public async Task<IDeviceDriver?> CreateAsync(DeviceDescriptor deviceDescriptor, CancellationToken cancellationToken)
    {
        try
        {
            var hidConnection = await _hidTransportFactory.CreateAsync(deviceDescriptor.Path);

            var hidDeviceDriver = new HidDeviceDriver(
                deviceDescriptor.Identity,
                hidConnection.Transport,
                new HidReportDecoder(hidConnection.ReportDescriptor));

            return new MozaFcdDeviceDriver(hidDeviceDriver);
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