using System.Diagnostics;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

public sealed class HidDeviceDriver : IDeviceDriver
{
    private readonly Dictionary<byte, byte[]> _lastReports = [];
    private readonly IHidTransport _transport;
    private readonly IHidReportDecoder _decoder;

    public HidDeviceDriver(
        DeviceIdentity identity,
        IHidTransport transport,
        IHidReportDecoder decoder)
    {
        Identity = identity;
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
    }

    public event EventHandler<DeviceInputEvent>? InputReceived;
    public DeviceIdentity Identity { get; }

    public IReadOnlyList<DeviceControl> Controls => _decoder.Controls;

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return Task
            .Factory
            .StartNew(
                _ => ReadAsyncInternal(),
                cancellationToken,
                TaskCreationOptions.LongRunning)
            .Unwrap();

        async Task ReadAsyncInternal()
        {
            var buffer = new byte[_transport.MaxInputReportLength];
            var changes = new List<HidInputChange>();

            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await _transport.ReadAsync(buffer, cancellationToken);
                if (read == 0) break;

                if (IsRepeat(buffer.AsSpan(0, read))) continue;

                changes.Clear();
                _decoder.Decode(buffer, changes);

                foreach (var change in changes)
                {
                    var inputEventKind = GetInputEventKind(change.DeviceControl, change.Value);
                    var inputEvent = new DeviceInputEvent(
                        Identity,
                        change.DeviceControl.Id,
                        inputEventKind,
                        change.Value,
                        Stopwatch.GetTimestamp());

                    InputReceived?.Invoke(this, inputEvent);
                }
            }
        }
    }

    private bool IsRepeat(ReadOnlySpan<byte> report)
    {
        if (_lastReports.TryGetValue(report[0], out var last) && report.SequenceEqual(last))
            return true;

        _lastReports[report[0]] = report.ToArray();
        return false;
    }

    private static InputEventKind GetInputEventKind(DeviceControl deviceControl, double value) => deviceControl switch
    {
        ButtonControl => (int)value != 0 ? InputEventKind.Pressed : InputEventKind.Released,
        AxisControl => InputEventKind.AxisChanged,
        HatControl => InputEventKind.HatChanged,
        _ => throw new ArgumentOutOfRangeException(nameof(deviceControl), deviceControl, null)
    };

    public async ValueTask DisposeAsync()
    {
        await _transport.DisposeAsync();
    }
}