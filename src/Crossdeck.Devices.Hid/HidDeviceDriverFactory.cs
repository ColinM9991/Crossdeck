using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;

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
        deviceDescriptor.Identity.DeviceKind is DeviceTransportKind.Hid;

    public async Task<IDeviceDriver?> CreateAsync(DeviceDescriptor deviceDescriptor, CancellationToken cancellationToken)
    {
        try
        {
            var hidConnection = await _hidTransportFactory.CreateAsync(deviceDescriptor, cancellationToken);

            return new HidJoystickDriver(deviceDescriptor.Identity, hidConnection.Transport, hidConnection.Decoder);
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