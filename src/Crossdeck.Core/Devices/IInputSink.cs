using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IInputSink
{
    void Post(DeviceInputEvent inputEvent);
}