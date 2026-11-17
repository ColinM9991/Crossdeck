using System.Collections.Frozen;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing;

internal sealed record OutputIndex
{
    public static readonly OutputIndex Empty = new(
        FrozenDictionary<SimRef, OutputBinding[]>.Empty,
        FrozenDictionary<DeviceIdentity, OutputBinding[]>.Empty,
        FrozenSet<SimRef>.Empty);

    public OutputIndex(
        FrozenDictionary<SimRef, OutputBinding[]> byRef,
        FrozenDictionary<DeviceIdentity, OutputBinding[]> byDevice,
        FrozenSet<SimRef> requiredRefs)
    {
        ByRef = byRef;
        ByDevice = byDevice;
        RequiredRefs = requiredRefs;
    }

    public FrozenDictionary<SimRef, OutputBinding[]> ByRef { get; }

    public FrozenDictionary<DeviceIdentity, OutputBinding[]> ByDevice { get; }

    public FrozenSet<SimRef> RequiredRefs { get; }
}