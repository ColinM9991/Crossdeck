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

    private Dictionary<InputId, int>? _buttonNumbers;

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

        if (e.Kind is not (InputEventKind.Pressed or InputEventKind.Released))
            return;

        _buttonNumbers ??= BuildButtonNumbers();
        if (_buttonNumbers.TryGetValue(e.Input, out var button))
            _fcdSession.PostButton(button, e.Kind == InputEventKind.Pressed);
    }

    private Dictionary<InputId, int> BuildButtonNumbers()
    {
        var numbers = new Dictionary<InputId, int>();
        var next = 0;
        foreach (var control in Controls)
        {
            if (control is ButtonControl button && numbers.TryAdd(button.Id, next))
                next++;
        }

        return numbers;
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