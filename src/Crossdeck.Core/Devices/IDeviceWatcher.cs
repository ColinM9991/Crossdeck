namespace Crossdeck.Core.Devices;

public interface IDeviceWatcher
{
    event EventHandler<DeviceArrivedEventArgs> DeviceArrived;

    event EventHandler<DeviceRemovedEventArgs> DeviceRemoved;

    IReadOnlyCollection<DeviceDescriptor> ConnectedDevices { get; }

    Task RunAsync(CancellationToken cancellationToken);
}

public readonly record struct DeviceIdentity(DeviceTransportKind DeviceKind, string StableId); // "hid", serial or location id

public enum DeviceTransportKind
{
    Hid,
    Serial
}

public sealed record DeviceDescriptor(
    DeviceIdentity Identity,
    string Path,
    uint[] Usages,
    int? VendorId,
    int? ProductId,
    string? SerialNumber,
    string? ProductName);

public enum InputKind { Button, Axis, Hat, Encoder, Switch }

public readonly record struct InputId(InputKind Kind, string Name)
{
    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}:{Name}";
}

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