using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

internal sealed class HidDeviceDriverFactory : IDeviceDriverFactory
{
    private readonly IHidTransportFactory _hidTransportFactory;

    public HidDeviceDriverFactory(
        IHidTransportFactory hidTransportFactory)
    {
        _hidTransportFactory = hidTransportFactory ?? throw new ArgumentNullException(nameof(hidTransportFactory));
    }

    public int Priority => 0;

    public bool CanCreate(DeviceDescriptor deviceDescriptor) =>
        deviceDescriptor.Kind is DeviceKind.Hid;

    public async Task<IDeviceDriver?> CreateAsync(
        DeviceDescriptor deviceDescriptor,
        CancellationToken cancellationToken)
    {
        try
        {
            if (deviceDescriptor.HidInterfaces.Count > 1)
                return await CreateCompositeAsync(deviceDescriptor);

            var hidConnection = await _hidTransportFactory.CreateAsync(deviceDescriptor.Path);

            return new HidDeviceDriver(
                deviceDescriptor.Identity,
                hidConnection.Transport,
                new HidReportDecoder(hidConnection.ReportDescriptor));
        }
        catch (DeviceNotUsableException)
        {
            return null;
        }
        catch (DeviceNotFoundException)
        {
            return null;
        }
    }

    private async Task<IDeviceDriver> CreateCompositeAsync(DeviceDescriptor deviceDescriptor)
    {
        var connections = new List<HidConnection>();

        try
        {
            foreach (var hidInterface in deviceDescriptor.HidInterfaces)
                connections.Add(await _hidTransportFactory.CreateAsync(hidInterface.Path));
        }
        catch
        {
            await DisposeConnectionsAsync(connections);
            throw;
        }

        var drivers = connections
            .Select((connection, index) => new HidDeviceDriver(
                WithInterfaceKey(deviceDescriptor.Identity, deviceDescriptor.HidInterfaces[index].Key),
                connection.Transport,
                new HidReportDecoder(connection.ReportDescriptor)))
            .ToList();

        return new CompositeHidDeviceDriver(deviceDescriptor.Identity, drivers);
    }

    private static async Task DisposeConnectionsAsync(List<HidConnection> connections)
    {
        foreach (var connection in connections)
            await connection.Transport.DisposeAsync();
    }

    private static DeviceIdentity WithInterfaceKey(DeviceIdentity identity, int key) =>
        identity with { Value = $"{identity.Value}@if={key}" };
}