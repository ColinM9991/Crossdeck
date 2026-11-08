using System.Collections.Concurrent;
using Crossdeck.Core;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

public sealed class HidOutputWriter : IDeviceOutputWriter
{
    private readonly IHidTransport _transport;
    private readonly IHidReportEncoder _encoder;
    private readonly ConcurrentDictionary<OutputId, int> _state = new();
    private readonly Signal _dirtySignal = new();

    public HidOutputWriter(
        IHidTransport transport,
        IHidReportEncoder encoder)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
    }

    public void Set(OutputId id, int value)
    {
        _state[id] = value;
        _dirtySignal.Set();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _encoder.Reset();
        await _dirtySignal.SetAsync(cancellationToken);

        await foreach (var _ in _dirtySignal.WaitAsync(cancellationToken))
        {
            var snapshot = _state.ToDictionary();

            foreach (var report in _encoder.Encode(snapshot))
            {
                await (report.Type is HidReportType.Feature
                    ? _transport.SetFeatureAsync(report.Data, cancellationToken)
                    : _transport.WriteAsync(report.Data, cancellationToken));
            }
        }
    }
}