using Crossdeck.Core;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using HidSharp;
using HidSharp.Reports;

namespace Crossdeck.Devices.Hid;

public sealed class HidDeviceWatcher : IDeviceWatcher
{
    private static readonly HashSet<uint> SupportedDevices = new()
        { 0x0001_0004, 0x0001_0005, 0x0001_0008, 0x0002_0001 };

    private volatile Dictionary<string, DeviceDescriptor> _deviceCache = new();

    private readonly Signal _deviceChangedSignal = new();

    public event EventHandler<DeviceArrivedEventArgs>? DeviceArrived;
    public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;

    public IReadOnlyCollection<DeviceDescriptor> ConnectedDevices =>
        [.. _deviceCache.Values];

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DeviceList.Local.Changed += OnChanged;
        try
        {
            await _deviceChangedSignal.SetAsync(cancellationToken);
            await foreach (var _ in _deviceChangedSignal.WaitAsync(cancellationToken))
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
        _deviceChangedSignal.Set();
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
            if (cache.TryGetValue(device.DevicePath, out var existingDevice))
            {
                localDevices[device.DevicePath] = existingDevice;
                continue;
            }

            var deviceFact = BuildDeviceFact(device);
            if (deviceFact is null || !deviceFact.Usages.All(x => SupportedDevices.Contains(x)))
            {
                continue;
            }

            var deviceIdentity = BuildDeviceIdentity(device, deviceFact);
            localDevices[device.DevicePath] = new DeviceDescriptor(
                deviceIdentity,
                DeviceKind.Hid,
                device.DevicePath,
                GetSafe(device, x => x.GetProductName()));
        }

        return localDevices;
    }

    private static DeviceFact? BuildDeviceFact(HidDevice device)
    {
        ReportDescriptor? descriptor;
        try
        {
            descriptor = device.GetReportDescriptor();
        }
        catch (Exception)
        {
            return null;
        }

        return new DeviceFact([.. descriptor.DeviceItems[0].Usages.GetAllValues()]); // TODO: Implement composite devices to handle child items
    }

    private static DeviceIdentity BuildDeviceIdentity(HidDevice device, DeviceFact deviceFact)
    {
        var serial = GetSafe(device, x => x.GetSerialNumber());
        var usage = deviceFact.Usages.First();

        var (kind, value) =
            serial is not null
                ? (DeviceIdentitySource.Serial, serial)
                : (DeviceIdentitySource.Usage, $"{usage >> 16:X4}_{usage & 0xFFFF:X4}");

        return new DeviceIdentity(kind, device.VendorID, device.ProductID, value);
    }

    private static TOut? GetSafe<TOut>(HidDevice device, Func<HidDevice, TOut> func) where TOut : class
    {
        try
        {
            return func(device);
        }
        catch
        {
            return null;
        }
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
    
    private record DeviceFact(ICollection<uint> Usages);
}