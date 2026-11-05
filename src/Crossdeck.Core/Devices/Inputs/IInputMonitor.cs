using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices.Inputs;

public interface IInputMonitor
{
    IAsyncEnumerable<DeviceInputEvent> Recent { get; }
}