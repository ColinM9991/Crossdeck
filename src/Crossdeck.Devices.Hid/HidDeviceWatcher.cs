using Crossdeck.Core;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using HidSharp;
using HidSharp.Reports;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Hid;

public sealed class HidDeviceWatcher : IDeviceWatcher
{
    private static readonly HashSet<uint> SupportedDevices = [0x0001_0004, 0x0001_0005, 0x0001_0008, 0x0002_0001];

    private readonly Signal _deviceChangedSignal = new();
    private readonly IUsbDeviceResolver _usbDeviceResolver;
    private readonly ILogger<HidDeviceWatcher> _logger;

    private volatile Dictionary<DeviceIdentity, DeviceDescriptor> _deviceCache = new();

    public HidDeviceWatcher(
        IUsbDeviceResolver usbDeviceResolver,
        ILogger<HidDeviceWatcher> logger)
    {
        _usbDeviceResolver = usbDeviceResolver ?? throw new ArgumentNullException(nameof(usbDeviceResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyCollection<DeviceDescriptor> ConnectedDevices =>
        [.. _deviceCache.Values];

    public event EventHandler<DeviceArrivedEventArgs>? DeviceArrived;
    public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HID device watch starting");
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
            _logger.LogInformation("HID device watch stopped");
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

    internal static IReadOnlyList<string> OrderPaths(
        IEnumerable<string> paths,
        Func<string, uint> firstUsage)
    {
        List<string> list =
        [
            .. paths.OrderBy(firstUsage)
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
        ];
        return list;
    }

    internal static IReadOnlyList<HidInterfaceDescriptor> KeyInterfaces(
        IReadOnlyList<string> orderedPaths,
        IReadOnlyList<HidInterfaceDescriptor>? resolverInterfaces)
    {
        var keys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (resolverInterfaces is not null)
        {
            foreach (var resolverInterface in resolverInterfaces) keys.TryAdd(resolverInterface.Path, resolverInterface.Key);
        }

        var interfaces = orderedPaths
            .Select((path, ordinal) => new HidInterfaceDescriptor(
                keys.GetValueOrDefault(path, ordinal),
                path))
            .ToList();

        return interfaces.OrderBy(x => x.Key).ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void OnChanged(object? sender, DeviceListChangedEventArgs e) => _deviceChangedSignal.Set();

    private void RaiseDevicesChanged(
        IEnumerable<DeviceDescriptor> added,
        IEnumerable<DeviceDescriptor> removed)
    {
        foreach (var device in removed)
        {
            _logger.LogInformation("HID device removed {Identity}", device.Identity);
            DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(device.Identity));
        }

        foreach (var device in added)
        {
            _logger.LogInformation("HID device added {Identity}", device.Identity);
            DeviceArrived?.Invoke(this, new DeviceArrivedEventArgs(device));
        }
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

            var orderedPaths = OrderPaths(group.Select(x => x.DevicePath), x => facts[x].Usages.FirstOrDefault());
            var primaryPath = orderedPaths.FirstOrDefault(x => HasSupportedUsages(facts[x]));
            if (primaryPath is null) continue;

            var primary = group.Single(x => x.DevicePath == primaryPath);
            var usbDevice = _usbDeviceResolver.Resolve(primaryPath);
            localDevices[deviceIdentity] = new DeviceDescriptor(
                deviceIdentity,
                DeviceKind.Hid,
                primaryPath,
                GetSafe(primary, x => x.GetProductName()),
                KeyInterfaces(orderedPaths, usbDevice?.HidInterfaces),
                usbDevice?.SerialInterfaces ?? []);
        }

        return localDevices;
    }

    private static bool HasSupportedUsages(DeviceFact deviceFact) =>
        deviceFact.Usages.All(x => SupportedDevices.Contains(x));

    private DeviceFact? BuildDeviceFact(HidDevice device)
    {
        ReportDescriptor? descriptor;
        try
        {
            descriptor = device.GetReportDescriptor();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read report descriptor for {DevicePath}; skipping device",
                device.DevicePath);
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