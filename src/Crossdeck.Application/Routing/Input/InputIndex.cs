using System.Collections.Frozen;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing.Input;

internal sealed record InputIndex
{
    public static readonly InputIndex Empty = new InputIndex(
        FrozenDictionary<(DeviceIdentity Device, InputId Control), InputBinding>.Empty,
        FrozenSet<SimRef>.Empty);

    public InputIndex(
        FrozenDictionary<(DeviceIdentity Device, InputId Control), InputBinding> byDevice,
        FrozenSet<SimRef> requiredRefs)
    {
        ByDevice = byDevice ?? throw new ArgumentNullException(nameof(byDevice));
        RequiredRefs = requiredRefs ?? throw new ArgumentNullException(nameof(requiredRefs));
    }

    public FrozenDictionary<(DeviceIdentity Device, InputId Control), InputBinding> ByDevice { get; }

    public FrozenSet<SimRef> RequiredRefs { get; }
}