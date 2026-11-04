namespace Crossdeck.Core.Devices.Models;

public enum InputEventKind
{
    Pressed,
    Released,
    Increment,
    Decrement,
    HatChanged,
    AxisChanged
}

public sealed record DeviceInputEvent(
    DeviceIdentity Device,
    InputId Input,
    InputEventKind Kind,
    double? Value,
    long Timestamp);
