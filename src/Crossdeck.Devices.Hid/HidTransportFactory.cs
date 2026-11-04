using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;
using HidSharp;

namespace Crossdeck.Devices.Hid;

internal sealed class HidTransportFactory : IHidTransportFactory
{
    public Task<HidConnection> CreateAsync(DeviceDescriptor deviceDescriptor, CancellationToken cancellationToken)
    {
        var devices = DeviceList.Local.GetHidDevices();
        var hidDevice = devices.FirstOrDefault(d => string.Equals(d.DevicePath, deviceDescriptor.Path))
                        ?? throw new DeviceNotFoundException(deviceDescriptor.Path);

        if (!hidDevice.TryOpen(out var hidStream))
        {
            throw new DeviceNotUsableException(deviceDescriptor.Path);
        }

        hidStream.ReadTimeout = Timeout.Infinite;
        hidStream.WriteTimeout = Timeout.Infinite;

        try
        {
            var reportDescriptor = hidDevice.GetReportDescriptor();
            var transport = new HidTransport(deviceDescriptor, hidStream, reportDescriptor);

            return Task.FromResult(new HidConnection(transport, new HidReportDecoder(reportDescriptor)));
        }
        catch
        {
            hidStream.Dispose();
            throw;
        }
    }
}