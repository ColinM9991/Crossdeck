namespace Crossdeck.Core.Devices;

public interface IDeviceWatcher
{
    event EventHandler<DeviceArrivedEventArgs> DeviceArrived;

    event EventHandler<DeviceRemovedEventArgs> DeviceRemoved;

    IReadOnlyCollection<DeviceDescriptor> ConnectedDevices { get; }

    Task RunAsync(CancellationToken cancellationToken);
}

public readonly record struct DeviceIdentity(DeviceTransportKind Transport, string StableId); // "hid", serial or location id

public enum DeviceTransportKind
{
    Hid,
    Serial
}

public sealed record DeviceDescriptor(
    DeviceIdentity Identity,
    string Path,
    int? VendorId,
    int? ProductId,
    string? SerialNumber,
    string? ProductName);

public readonly record struct InputId(string Value);

public enum InputEventKind
{
    Pressed,
    Released,
    Increment,
    Decrement,
    PositionChanged,
    AxisChanged
}

public sealed record DeviceInputEvent(
    DeviceIdentity Device,
    InputId Input,
    InputEventKind Kind,
    double? Value,
    long Timestamp);

public sealed record DeviceArrivedEventArgs(DeviceDescriptor Descriptor);
public sealed record DeviceRemovedEventArgs(DeviceIdentity Identity);