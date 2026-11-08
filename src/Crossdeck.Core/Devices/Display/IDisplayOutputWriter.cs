using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Core.Devices.Display;

public interface IDisplayOutputWriter : IDeviceOutputWriter
{
    void Set(TextScreen screen);
}