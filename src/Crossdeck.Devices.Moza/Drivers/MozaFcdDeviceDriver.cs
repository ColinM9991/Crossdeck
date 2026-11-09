using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.Moza.Drivers;

public sealed class MozaFcdDeviceDriver : IDeviceDriver
{
    private readonly HidDeviceDriver _hidDeviceDriver;

    public MozaFcdDeviceDriver(
        HidDeviceDriver hidDeviceDriver)
    {
        _hidDeviceDriver = hidDeviceDriver ?? throw new ArgumentNullException(nameof(hidDeviceDriver));
        _hidDeviceDriver.InputReceived += OnInputReceived;
    }

    public event EventHandler<DeviceInputEvent>? InputReceived;
    
    public DeviceIdentity Identity => _hidDeviceDriver.Identity;
    
    public IReadOnlyList<DeviceControl> Controls => _hidDeviceDriver.Controls;

    private void OnInputReceived(object? sender, DeviceInputEvent e)
    {
        InputReceived?.Invoke(this, e);
    }
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        List<Task> tasks =
        [
            _hidDeviceDriver.RunAsync(cts.Token)
        ];

        await Task.WhenAny(tasks);
        await cts.CancelAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _hidDeviceDriver.DisposeAsync();
    }
}