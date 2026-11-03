using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Hid;

public class HidJoystickDriver : IDeviceDriver
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
                Console.WriteLine($"{change.Id} {change.Value}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _transport.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}