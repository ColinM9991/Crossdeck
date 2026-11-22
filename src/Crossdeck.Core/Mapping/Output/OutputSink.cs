using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Mapping.Output;

public closed record OutputSink
{
    public sealed record Indicator(DeviceIdentity Device, OutputId Output) : OutputSink;

    public sealed record Screen(DeviceIdentity Device, DisplayRole? Role = DisplayRole.Captain) : OutputSink;

    public sealed record Variable(string Name) : OutputSink;

    public sealed record Computed(string Id) : OutputSink;
}