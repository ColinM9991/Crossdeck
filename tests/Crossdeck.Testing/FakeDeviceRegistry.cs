using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Testing;

/// <summary>
/// An in-memory <see cref="IDeviceRegistry"/> that tests add drivers to directly.
/// NSubstitute cannot set the <c>out</c> driver of <see cref="IDeviceRegistry.TryGet"/>,
/// so a substitute cannot stand in for a registry with connected devices.
/// </summary>
public sealed class FakeDeviceRegistry : IDeviceRegistry
{
    private readonly Dictionary<DeviceIdentity, IDeviceDriver> _drivers = new();

    public event EventHandler<DriverAddedEventArgs>? DriverAdded
    {
        add { }
        remove { }
    }

    public event EventHandler<DriverRemovedEventArgs>? DriverRemoved
    {
        add { }
        remove { }
    }

    public bool TryGet(DeviceIdentity identity, out IDeviceDriver? driver) =>
        _drivers.TryGetValue(identity, out driver);

    public void Add(DeviceIdentity identity, IDeviceDriver driver) => _drivers[identity] = driver;

    public bool Remove(DeviceIdentity identity) => _drivers.Remove(identity);

    public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
