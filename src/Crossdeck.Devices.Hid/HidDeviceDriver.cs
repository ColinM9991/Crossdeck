using System.Diagnostics;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

public sealed class HidDeviceDriver : IDeviceDriver
{
    private readonly Dictionary<InputId, double?> _lastSentValues = [];
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

    public DeviceIdentity Identity { get; }

    public IReadOnlyList<DeviceControl> Controls => _decoder.Controls;

    public event EventHandler<DeviceInputEvent>? InputReceived;

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

                changes.Clear();
                _decoder.Decode(buffer, changes);

                foreach (var change in changes)
                {
                    if (!ShouldRepeat(change))
                        continue;

                    Console.WriteLine($"Change: {change.DeviceControl.Id}, Value: {change.Value}");
                    var inputEventKind = GetInputEventKind(change.DeviceControl, change.Value);
                    var inputEvent = new DeviceInputEvent(
                        Identity,
                        change.DeviceControl,
                        inputEventKind,
                        change.Value,
                        Stopwatch.GetTimestamp());

                    InputReceived?.Invoke(this, inputEvent);
                }
            }
        }
    }

    public async ValueTask DisposeAsync() => await _transport.DisposeAsync();

    private bool ShouldRepeat(HidInputChange change)
    {
        var control = change.DeviceControl;
        if (!_lastSentValues.TryGetValue(control.Id, out var lastValue))
        {
            _lastSentValues[control.Id] = change.Value;
            return true;
        }

        var shouldRepeat = control switch
        {
            AxisControl c => (change.Value - lastValue >= (c.Max - c.Min) * 0.5),
            _ => (change.Value.CompareTo(lastValue) != 0),
        };

        if (!shouldRepeat)
            return false;

        _lastSentValues[control.Id] = change.Value;
        return true;
    }

    private static InputEventKind GetInputEventKind(DeviceControl deviceControl, double value) => deviceControl switch
    {
        ButtonControl => (int)value != 0 ? InputEventKind.Pressed : InputEventKind.Released,
        AxisControl => InputEventKind.AxisChanged,
        HatControl => InputEventKind.HatChanged,
        _ => throw new ArgumentOutOfRangeException(nameof(deviceControl), deviceControl, null)
    };
}