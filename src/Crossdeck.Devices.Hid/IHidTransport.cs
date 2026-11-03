using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Hid;

public interface IHidTransport : IDeviceTransport
{
    int MaxInputReportLength { get; }

    Task SetFeatureAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken);

    Task<int> GetFeatureAsync(Memory<byte> report, CancellationToken cancellationToken);
}