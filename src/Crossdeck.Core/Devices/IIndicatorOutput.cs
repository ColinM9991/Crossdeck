using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IIndicatorOutput
{
    void Set(OutputId id, int value);
}