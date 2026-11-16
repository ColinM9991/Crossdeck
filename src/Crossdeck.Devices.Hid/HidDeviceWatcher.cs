using Crossdeck.Core;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using HidSharp;
using HidSharp.Reports;

namespace Crossdeck.Devices.Hid;

public sealed class HidDeviceWatcher : IDeviceWatcher
{
    private static readonly HashSet<uint> SupportedDevices = [0x0001_0004, 0x0001_0005, 0x0001_0008, 0x0002_0001];

    private readonly Signal _deviceChangedSignal = new();
    private readonly IUsbDeviceResolver _usbDeviceResolver;

    private volatile Dictionary<DeviceIdentity, DeviceDescriptor> _deviceCache = new();

    public HidDeviceWatcher(IUsbDeviceResolver usbDeviceResolver)
    {
        _usbDeviceResolver = usbDeviceResolver ?? throw new ArgumentNullException(nameof(usbDeviceResolver));
    }

    public IReadOnlyCollection<DeviceDescriptor> ConnectedDevices =>
        [.. _deviceCache.Values];

    public event EventHandler<DeviceArrivedEventArgs>? DeviceArrived;
    public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;

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

    internal static DeviceIdentity BuildDeviceIdentity(int vendorId, int productId, string? serial, uint usage)
    {
        var trimmedSerial = serial?.Trim();

        var (kind, value) =
            string.IsNullOrWhiteSpace(trimmedSerial)
                ? (DeviceIdentitySource.Usage, FormatUsage(usage))
                : (DeviceIdentitySource.Serial, trimmedSerial);

        return new DeviceIdentity(kind, vendorId, productId, value);
    }

    internal static string FormatUsage(uint usage) => $"{usage >> 16:X4}_{usage & 0xFFFF:X4}";

    private void OnChanged(object? sender, DeviceListChangedEventArgs e) => _deviceChangedSignal.Set();

    private void RaiseDevicesChanged(
        IEnumerable<DeviceDescriptor> added,
        IEnumerable<DeviceDescriptor> removed)
    {
        foreach (var device in removed) DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(device.Identity));

        foreach (var device in added) DeviceArrived?.Invoke(this, new DeviceArrivedEventArgs(device));
    }

    private Dictionary<DeviceIdentity, DeviceDescriptor> BuildDeviceCache(
        Dictionary<DeviceIdentity, DeviceDescriptor> cache,
        IEnumerable<HidDevice> devices)
    {
        var facts = new Dictionary<string, DeviceFact>();
        var groups = new Dictionary<DeviceIdentity, List<HidDevice>>();

        foreach (var device in devices)
        {
            if (!facts.TryGetValue(device.DevicePath, out var deviceFact))
            {
                deviceFact = BuildDeviceFact(device);
                if (deviceFact is null) continue;

                facts[device.DevicePath] = deviceFact;
            }

            var deviceIdentity = BuildDeviceIdentity(device, deviceFact);

            if (!groups.TryGetValue(deviceIdentity, out var group))
            {
                group = [];
                groups.Add(deviceIdentity, group);
            }

            group.Add(device);
        }

        var localDevices = new Dictionary<DeviceIdentity, DeviceDescriptor>();
        foreach (var (deviceIdentity, group) in groups)
        {
            if (cache.TryGetValue(deviceIdentity, out var existingDevice))
            {
                localDevices[deviceIdentity] = existingDevice;
                continue;
            }

            var primaryIndex = group.FindIndex(device => HasSupportedUsages(facts[device.DevicePath]));
            if (primaryIndex < 0) continue;

            var primary = group[primaryIndex];
            localDevices[deviceIdentity] = new DeviceDescriptor(
                deviceIdentity,
                DeviceKind.Hid,
                primary.DevicePath,
                GetSafe(primary, x => x.GetProductName()),
                [.. group.Select(x => new HidInterfaceDescriptor(x.DevicePath))],
                GetSerialInterfaces(primary.DevicePath));
        }

        return localDevices;
    }

    private IReadOnlyList<SerialInterfaceDescriptor> GetSerialInterfaces(string hidPath) =>
        _usbDeviceResolver.Resolve(hidPath)?.SerialInterfaces ?? [];

    private static bool HasSupportedUsages(DeviceFact deviceFact) =>
        deviceFact.Usages.All(x => SupportedDevices.Contains(x));

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

        return new DeviceFact([
            .. descriptor.DeviceItems[0].Usages.GetAllValues()
        ]);
    }

    private DeviceIdentity BuildDeviceIdentity(HidDevice device, DeviceFact deviceFact)
    {
        var serial = _usbDeviceResolver.Resolve(device.DevicePath)?.Identity.Serial;
        var usage = deviceFact.Usages.FirstOrDefault();

        return BuildDeviceIdentity(device.VendorID, device.ProductID, serial, usage);
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

    private static IEnumerable<DeviceDescriptor> GetDevices(
        Dictionary<DeviceIdentity, DeviceDescriptor> left,
        Dictionary<DeviceIdentity, DeviceDescriptor> right)
    {
        foreach (var (key, device) in right)
            if (!left.ContainsKey(key)) yield return device;
    }

    private record DeviceFact(ICollection<uint> Usages);
}