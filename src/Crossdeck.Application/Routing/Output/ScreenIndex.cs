using System.Collections.Frozen;
using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Routing.Adapters;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing.Output;

internal sealed record ScreenIndex(
    IAircraftScreenAdapter? Adapter,
    FrozenDictionary<DeviceIdentity, DisplayRole> ScreenRoles,
    FrozenSet<SimRef> RequiredRefs,
    FrozenDictionary<SimRef, FrozenSet<DisplayRole>> ByRef,
    FrozenDictionary<DisplayRole, Dictionary<SimRef, SimValue>> ValueBuffers)
{
    internal static readonly ScreenIndex Empty = new(
        null,
        FrozenDictionary<DeviceIdentity, DisplayRole>.Empty,
        FrozenSet<SimRef>.Empty,
        FrozenDictionary<SimRef, FrozenSet<DisplayRole>>.Empty,
        FrozenDictionary<DisplayRole, Dictionary<SimRef, SimValue>>.Empty);
}