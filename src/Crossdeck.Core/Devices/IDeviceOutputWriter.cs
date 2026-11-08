using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

public interface IDeviceOutputWriter
{
    void Set(OutputId id, int value);

    Task RunAsync(CancellationToken cancellationToken);
}