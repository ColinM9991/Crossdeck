using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Mapping;

public record OutputSink
{
    public sealed record Indicator(DeviceIdentity Device, OutputId Output) : OutputSink;

    public sealed record Screen(DeviceIdentity Device) : OutputSink;

    public sealed record Variable(string Name) : OutputSink;
}