using System.Diagnostics;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Hid;

public sealed class HidDeviceDriver : IDeviceDriver
{
    private readonly Dictionary<InputId, double> _lastSentValues = [];
    private readonly IHidTransport _transport;
    private readonly IHidReportDecoder _decoder;
    private readonly ILogger<HidDeviceDriver> _logger;

    public HidDeviceDriver(
        DeviceIdentity identity,
        IHidTransport transport,
        IHidReportDecoder decoder,
        ILogger<HidDeviceDriver> logger)
    {
        Identity = identity;
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var read = await _transport.ReadAsync(buffer, cancellationToken);
                    if (read == 0)
                    {
                        _logger.LogWarning("Transport read returned zero bytes; ending input loop for {Identity}",
                            Identity);
                        break;
                    }

                    changes.Clear();
                    _decoder.Decode(buffer, changes);

                    foreach (var change in changes)
                    {
                        if (!ShouldRaise(change))
                            continue;

                        var inputEventKind = GetInputEventKind(change.DeviceControl, change.Value);
                        var inputEvent = new DeviceInputEvent(
                            Identity,
                            change.DeviceControl,
                            inputEventKind,
                            change.Value,
                            Stopwatch.GetTimestamp());

                        _logger.LogDebug("Input event {Identity} {Control} {Kind} {Value}",
                            Identity, change.DeviceControl.Id, inputEventKind, change.Value);
                        InputReceived?.Invoke(this, inputEvent);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "HID input read loop for {Identity} faulted", Identity);
                throw;
            }
        }
    }

    public async ValueTask DisposeAsync() => await _transport.DisposeAsync();

    private bool ShouldRaise(HidInputChange change)
    {
        var control = change.DeviceControl;
        if (!_lastSentValues.TryGetValue(control.Id, out var lastValue))
        {
            _lastSentValues[control.Id] = change.Value;
            return true;
        }

        var shouldRepeat = control switch
        {
            AxisControl c => Math.Abs(change.Value - lastValue) >= (c.Max - c.Min) * 0.005,
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