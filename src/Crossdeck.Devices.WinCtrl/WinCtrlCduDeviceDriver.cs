using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.WinCtrl;

public sealed class WinCtrlCduDeviceDriver : 
    IDeviceDriver,
    IIndicatorOutput,
    ITextDisplayOutput
{
    private readonly HidDeviceDriver _driver;
    private readonly IDisplayOutputWriter _writer;

    public WinCtrlCduDeviceDriver(
        HidDeviceDriver driver,
        IDisplayOutputWriter writer)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));

        _driver.InputReceived += DriverOnInputReceived;

        Controls = [.. _driver.Controls];
    }

    public event EventHandler<DeviceInputEvent>? InputReceived;

    public DeviceIdentity Identity => _driver.Identity;

    public IReadOnlyList<DeviceControl> Controls { get; }

    private void DriverOnInputReceived(object? sender, DeviceInputEvent e)
    {
        InputReceived?.Invoke(this, e);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var tasks = new[]
        {
            _driver.RunAsync(cts.Token),
            _writer.RunAsync(cts.Token)
        };

        await Task.WhenAny(tasks);
        await cts.CancelAsync();
        await Task.WhenAll(tasks);
    }

    public void Set(OutputId id, int value) => _writer.Set(id, value);

    public void Show(TextScreen screen) => _writer.Set(screen);

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }
}