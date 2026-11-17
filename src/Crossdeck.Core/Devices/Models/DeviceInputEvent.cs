namespace Crossdeck.Core.Devices.Models;

public enum InputEventKind
{
    Pressed,
    Released,
    Increment,
    Decrement,
    HatChanged,
    AxisChanged,
    PositionChanged
}

public sealed record DeviceInputEvent(
    DeviceIdentity Identity,
    DeviceControl Control,
    InputEventKind Kind,
    double? Value,
    long Timestamp);
