using System.Threading.Channels;
using Crossdeck.Core.Devices;
using HidSharp;

namespace Crossdeck.Devices.Hid;

public sealed class HidDeviceWatcher : IDeviceWatcher
{
    private volatile Dictionary<string, DeviceDescriptor> _deviceCache = new();

    private readonly Channel<bool> _deviceChangeChannel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropWrite
    });

    public event EventHandler<DeviceArrivedEventArgs>? DeviceArrived;
    public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;

    public IReadOnlyCollection<DeviceDescriptor> ConnectedDevices =>
        [.. _deviceCache.Values];

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DeviceList.Local.Changed += OnChanged;
        try
        {
            _deviceChangeChannel.Writer.TryWrite(true);
            await foreach (var _ in _deviceChangeChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var newDeviceCache = BuildDeviceCache(_deviceCache, DeviceList.Local.GetHidDevices());
                var addedDevices = GetDevices(_deviceCache, newDeviceCache);
                var removedDevices = GetDevices(newDeviceCache, _deviceCache);

                Interlocked.Exchange(ref _deviceCache, newDeviceCache);

                RaiseDevicesChanged(addedDevices, removedDevices);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            DeviceList.Local.Changed -= OnChanged;
        }
    }

    private void OnChanged(object? sender, DeviceListChangedEventArgs e)
    {
        _deviceChangeChannel.Writer.TryWrite(true);
    }

    private void RaiseDevicesChanged(
        IEnumerable<DeviceDescriptor> added,
        IEnumerable<DeviceDescriptor> removed)
    {
        foreach (var device in removed)
        {
            DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(device.Identity));
        }

        foreach (var device in added)
        {
            DeviceArrived?.Invoke(this, new DeviceArrivedEventArgs(device));
        }
    }

    private static Dictionary<string, DeviceDescriptor> BuildDeviceCache(
        Dictionary<string, DeviceDescriptor> cache,
        IEnumerable<HidDevice> devices)
    {
        var localDevices = new Dictionary<string, DeviceDescriptor>();
        foreach (var device in devices)
        {
            var identity = new DeviceIdentity(DeviceTransportKind.Hid, device.DevicePath);
            if (cache.TryGetValue(device.DevicePath, out var existingDevice))
            {
                localDevices[device.DevicePath] = existingDevice;
                continue;
            }

            try
            {
                var descriptor = new DeviceDescriptor(identity, device.DevicePath, device.VendorID,
                    device.ProductID, device.GetSerialNumber(), device.GetProductName());

                localDevices[device.DevicePath] = descriptor;
            }
            catch (Exception)
            {
            }
        }

        return localDevices;
    }

    private static IEnumerable<DeviceDescriptor> GetDevices(Dictionary<string, DeviceDescriptor> left,
        Dictionary<string, DeviceDescriptor> right)
    {
        foreach (var (key, device) in right)
        {
            if (!left.ContainsKey(key))
            {
                yield return device;
            }
        }
    }
}