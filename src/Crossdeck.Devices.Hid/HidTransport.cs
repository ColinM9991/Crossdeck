using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;

namespace Crossdeck.Devices.Hid;

internal sealed class HidTransport : IHidTransport
{
    private readonly HidStream _hidStream;

    private readonly HidDeviceInputReceiver _receiver;
    private readonly byte[] _scratch;

    public HidTransport(
        HidStream hidStream,
        ReportDescriptor reportDescriptor)
    {
        _hidStream = hidStream ?? throw new ArgumentNullException(nameof(hidStream));

        _receiver = reportDescriptor.CreateHidDeviceInputReceiver();
        MaxInputReportLength = reportDescriptor.MaxInputReportLength;
        _scratch = new byte[MaxInputReportLength];

        _receiver.Start(_hidStream);
    }

    public int MaxInputReportLength { get; }

    public async ValueTask DisposeAsync()
    {
        await _hidStream.DisposeAsync();
    }

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_receiver.IsRunning) return Task.FromResult(0);
            if (_receiver.TryRead(_scratch, 0, out var _))
            {
                _scratch.AsSpan(0, MaxInputReportLength).CopyTo(buffer.Span);
                return Task.FromResult(MaxInputReportLength);
            }

            WaitHandle.WaitAny([
                _receiver.WaitHandle,
                cancellationToken.WaitHandle
            ]);
        }

        return Task.FromResult(0);
    }

    public Task SetFeatureAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken)
    {
        _hidStream.SetFeature(report.ToArray());

        return Task.CompletedTask;
    }

    public Task<int> GetFeatureAsync(Memory<byte> report, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}