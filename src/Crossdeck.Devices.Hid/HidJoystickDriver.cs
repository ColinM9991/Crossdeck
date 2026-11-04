using System.Diagnostics;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

public sealed class HidJoystickDriver : IDeviceDriver
{
    private readonly IHidTransport _transport;
    private readonly IHidReportDecoder _decoder;

    public HidJoystickDriver(
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

    public async Task RunAsync(CancellationToken cancellationToken)
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
                var inputEventKind = GetInputEventKind(change.DeviceControl, change.Value);
                InputReceived?.Invoke(this,
                    new DeviceInputEvent(
                        Identity,
                        change.DeviceControl.Id,
                        inputEventKind,
                        change.Value,
                        Stopwatch.GetTimestamp()));
            }
        }
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