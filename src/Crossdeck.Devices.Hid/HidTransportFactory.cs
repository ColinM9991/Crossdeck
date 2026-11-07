using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Devices.Hid.Models;
using HidSharp;

namespace Crossdeck.Devices.Hid;

internal sealed class HidTransportFactory : IHidTransportFactory
{
    public Task<HidConnection> CreateAsync(string path)
    {
        var devices = DeviceList.Local.GetHidDevices();
        var hidDevice = devices.FirstOrDefault(d => string.Equals(d.DevicePath, path))
                        ?? throw new DeviceNotFoundException(path);

        if (!hidDevice.TryOpen(out var hidStream))
        {
            throw new DeviceNotUsableException(path);
        }

        hidStream.ReadTimeout = Timeout.Infinite;
        hidStream.WriteTimeout = Timeout.Infinite;

        try
        {
            var reportDescriptor = hidDevice.GetReportDescriptor();
            var transport = new HidTransport(hidStream, reportDescriptor);

            return Task.FromResult(new HidConnection(transport, reportDescriptor));
        }
        catch
        {
            hidStream.Dispose();
            throw;
        }
    }
}