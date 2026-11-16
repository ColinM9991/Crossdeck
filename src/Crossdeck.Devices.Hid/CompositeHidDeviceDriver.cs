using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Hid;

public sealed class CompositeHidDeviceDriver : IDeviceDriver
{
    private readonly IReadOnlyList<HidDeviceDriver> _drivers;
    private readonly IReadOnlyList<DeviceControl> _controls;
    private readonly Dictionary<(HidDeviceDriver Driver, InputId Id), DeviceControl> _controlRemap;

    public CompositeHidDeviceDriver(DeviceIdentity identity, IReadOnlyList<HidDeviceDriver> drivers)
    {
        Identity = identity;
        _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));

        var controls = new List<DeviceControl>();
        var controlRemap = new Dictionary<(HidDeviceDriver, InputId), DeviceControl>();
        foreach (var driver in _drivers)
        {
            var key = GetInterfaceKey(driver.Identity);
            foreach (var control in driver.Controls)
            {
                var remapped = key is null ? control : RemapControl(control, key);
                controls.Add(remapped);
                controlRemap[(driver, control.Id)] = remapped;
            }
        }

        _controls = controls;
        _controlRemap = controlRemap;

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

    private void OnDriverInputReceived(object? sender, DeviceInputEvent e)
    {
        if (sender is HidDeviceDriver driver
            && _controlRemap.TryGetValue((driver, e.Control.Id), out var control)
            && !ReferenceEquals(control, e.Control))
        {
            e = e with { Control = control };
        }

        InputReceived?.Invoke(this, e);
    }

    private static DeviceControl RemapControl(DeviceControl control, string key) =>
        control with
        {
            Id = control.Id with { Name = $"{control.Id.Name}@{key}" },
            Name = $"{control.Name} ({key.Replace("=", " ")})"
        };

    private static string? GetInterfaceKey(DeviceIdentity identity)
    {
        var value = identity.Value.AsSpan();
        var index = value.LastIndexOf('@');
        if (index < 0) return null;

        var key = value[(index + 1)..];
        return key.StartsWith("if=", StringComparison.Ordinal) ? key.ToString() : null;
    }
}