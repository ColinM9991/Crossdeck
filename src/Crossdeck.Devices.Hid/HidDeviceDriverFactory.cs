using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;

namespace Crossdeck.Devices.Hid;

internal sealed class HidDeviceDriverFactory : IDeviceDriverFactory
{
    private static readonly HashSet<HidUsage> GameControllers =
        [HidUsage.Joystick, HidUsage.Gamepad, HidUsage.MultiAxis, HidUsage.FlightSimDevice];
    private readonly IHidTransportFactory _hidTransportFactory;

    public HidDeviceDriverFactory(
        IHidTransportFactory hidTransportFactory)
    {
        _hidTransportFactory = hidTransportFactory ?? throw new ArgumentNullException(nameof(hidTransportFactory));
    }

    public int Priority => 0;

    public bool CanCreate(DeviceDescriptor deviceDescriptor) =>
        deviceDescriptor.Identity.DeviceKind is DeviceTransportKind.Hid &&
        deviceDescriptor.Usages.Any(u => GameControllers.Contains(u));

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