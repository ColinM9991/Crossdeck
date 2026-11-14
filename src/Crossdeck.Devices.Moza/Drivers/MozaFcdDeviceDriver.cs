using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.Moza.Drivers;

public sealed class MozaFcdDeviceDriver : IDeviceDriver, ITextDisplayOutput
{
    private readonly HidDeviceDriver _hidDeviceDriver;
    private readonly FcdSession _fcdSession;

    public MozaFcdDeviceDriver(
        HidDeviceDriver hidDeviceDriver,
        FcdSession fcdSession)
    {
        _hidDeviceDriver = hidDeviceDriver ?? throw new ArgumentNullException(nameof(hidDeviceDriver));
        _fcdSession = fcdSession ?? throw new ArgumentNullException(nameof(fcdSession));
        _hidDeviceDriver.InputReceived += OnInputReceived;
    }

    public event EventHandler<DeviceInputEvent>? InputReceived;

    public DeviceIdentity Identity => _hidDeviceDriver.Identity;

    public IReadOnlyList<DeviceControl> Controls => _hidDeviceDriver.Controls;

    public void Show(TextScreen screen) => _fcdSession.Show(screen);

    private void OnInputReceived(object? sender, DeviceInputEvent e)
    {
        InputReceived?.Invoke(this, e);

        if (e.Control is not ButtonControl button ||
            e.Kind is not (InputEventKind.Pressed or InputEventKind.Released))
            return;

        _fcdSession.PostButton((int)button.ButtonId, e.Kind is InputEventKind.Pressed);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        List<Task> tasks =
        [
            _hidDeviceDriver.RunAsync(cts.Token),
            _fcdSession.RunAsync(cts.Token),
        ];

        await Task.WhenAny(tasks);
        await cts.CancelAsync();
        await Task.WhenAll(tasks);
    }

    public async ValueTask DisposeAsync()
    {
        await _hidDeviceDriver.DisposeAsync();
        await _fcdSession.DisposeAsync();
    }
}