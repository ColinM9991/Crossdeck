using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices.Inputs;

public interface IInputSink
{
    void Post(DeviceInputEvent inputEvent);
}