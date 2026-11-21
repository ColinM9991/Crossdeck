using System.Collections.Frozen;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Routing.Input;

internal sealed record InputIndex
{
    public static readonly InputIndex Empty = new InputIndex(
        FrozenDictionary<(DeviceIdentity Device, InputId Control), CompiledInputBinding>.Empty,
        FrozenSet<SimRef>.Empty);

    public InputIndex(
        FrozenDictionary<(DeviceIdentity Device, InputId Control), CompiledInputBinding> byDevice,
        FrozenSet<SimRef> requiredRefs)
    {
        ByDevice = byDevice ?? throw new ArgumentNullException(nameof(byDevice));
        RequiredRefs = requiredRefs ?? throw new ArgumentNullException(nameof(requiredRefs));
    }

    public FrozenDictionary<(DeviceIdentity Device, InputId Control), CompiledInputBinding> ByDevice { get; }

    public FrozenSet<SimRef> RequiredRefs { get; }
}