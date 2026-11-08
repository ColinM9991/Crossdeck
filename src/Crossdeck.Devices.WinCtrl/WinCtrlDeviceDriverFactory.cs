using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Drivers;
using Crossdeck.Devices.WinCtrl.Encoders;
using Crossdeck.Devices.WinCtrl.Frames;

namespace Crossdeck.Devices.WinCtrl;

public sealed class WinCtrlDeviceDriverFactory : IDeviceDriverFactory
{
    private readonly IHidTransportFactory _hidTransportFactory;

    public WinCtrlDeviceDriverFactory(IHidTransportFactory hidTransportFactory)
    {
        _hidTransportFactory = hidTransportFactory ?? throw new ArgumentNullException(nameof(hidTransportFactory));
    }

    public int Priority => 100;

    public bool CanCreate(DeviceDescriptor deviceDescriptor) => deviceDescriptor.Identity.VendorId == 0x4098;

    public async Task<IDeviceDriver?> CreateAsync(DeviceDescriptor deviceDescriptor,
        CancellationToken cancellationToken)
    {
        try
        {
            var hidConnection = await _hidTransportFactory.CreateAsync(deviceDescriptor.Path);

            var hidDevice = new HidDeviceDriver(
                deviceDescriptor.Identity,
                hidConnection.Transport,
                new HidReportDecoder(hidConnection.ReportDescriptor));

            var definition = WinCtrlDefinitions.ForPid(deviceDescriptor.Identity.ProductId);
            if (definition == null)
            {
                return null;
            }

            var frameSource = new CduFrameSource(new WinCtrlWinCtrlDisplayEncoder(definition, TextPalette.Default));
            var outputWriter = new WinCtrlOutputWriter(
                hidConnection.Transport,
                new WinCtrlHidReportEncoder(definition),
                frameSource);

            return new WinCtrlCduDeviceDriver(hidDevice, outputWriter, frameSource);
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