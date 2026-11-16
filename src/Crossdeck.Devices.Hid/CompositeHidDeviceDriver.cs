using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Hid;

public sealed class CompositeHidDeviceDriver : IDeviceDriver
{
    private readonly IReadOnlyList<HidDeviceDriver> _drivers;
    private readonly IReadOnlyList<DeviceControl> _controls;

    public CompositeHidDeviceDriver(DeviceIdentity identity, IReadOnlyList<HidDeviceDriver> drivers)
    {
        Identity = identity;
        _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
        _controls = [.. _drivers.SelectMany(x => x.Controls)];

        foreach (var driver in _drivers)
        {
            driver.InputReceived += OnDriverInputReceived;
        }
    }

    public DeviceIdentity Identity { get; }

    public IReadOnlyList<DeviceControl> Controls => _controls;

    public event EventHandler<DeviceInputEvent>? InputReceived;

    public Task RunAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(_drivers.Select(x => x.RunAsync(cancellationToken)));

    public async ValueTask DisposeAsync()
    {
        foreach (var driver in _drivers)
        {
            driver.InputReceived -= OnDriverInputReceived;
            await driver.DisposeAsync();
        }
    }

    private void OnDriverInputReceived(object? sender, DeviceInputEvent e) => InputReceived?.Invoke(this, e);
}
