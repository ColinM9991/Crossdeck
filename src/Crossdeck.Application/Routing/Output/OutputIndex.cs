using System.Collections.Frozen;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Output;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing.Output;

internal sealed record OutputIndex
{
    public static readonly OutputIndex Empty = new(
        FrozenDictionary<SimRef, OutputBinding[]>.Empty,
        FrozenDictionary<DeviceIdentity, OutputBinding[]>.Empty,
        FrozenDictionary<DeviceIdentity, OutputSink.Screen[]>.Empty,
        FrozenSet<SimRef>.Empty);

    public OutputIndex(
        FrozenDictionary<SimRef, OutputBinding[]> byRef,
        FrozenDictionary<DeviceIdentity, OutputBinding[]> byDevice,
        FrozenDictionary<DeviceIdentity, OutputSink.Screen[]> screenByDevice,
        FrozenSet<SimRef> requiredRefs)
    {
        ByRef = byRef;
        ByDevice = byDevice;
        ScreenByDevice = screenByDevice;
        RequiredRefs = requiredRefs;
    }

    public FrozenDictionary<SimRef, OutputBinding[]> ByRef { get; }

    public FrozenDictionary<DeviceIdentity, OutputBinding[]> ByDevice { get; }

    public FrozenDictionary<DeviceIdentity, OutputSink.Screen[]> ScreenByDevice { get; }

    public FrozenSet<SimRef> RequiredRefs { get; }
}