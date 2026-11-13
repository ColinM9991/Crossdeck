using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Moza.Drivers;
using Crossdeck.Devices.Serial;
using Crossdeck.Devices.Serial.Pairing;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza;

public sealed class MozaDeviceDriverFactory : IDeviceDriverFactory
{
    private readonly IHidTransportFactory _hidTransportFactory;
    private readonly ISerialTransportFactory _serialTransportFactory;
    private readonly ISerialPortLocator _serialPortLocator;
    private readonly TimeProvider _timeProvider;
    private readonly ILoggerFactory _loggerFactory;

    public MozaDeviceDriverFactory(
        IHidTransportFactory hidTransportFactory,
        ISerialTransportFactory serialTransportFactory,
        ISerialPortLocator serialPortLocator,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        _hidTransportFactory = hidTransportFactory ?? throw new ArgumentNullException(nameof(hidTransportFactory));
        _serialTransportFactory =
            serialTransportFactory ?? throw new ArgumentNullException(nameof(serialTransportFactory));
        _serialPortLocator = serialPortLocator ?? throw new ArgumentNullException(nameof(serialPortLocator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public int Priority => 101;

    public bool CanCreate(DeviceDescriptor deviceDescriptor) => MozaIds.IsFcdDisplay(deviceDescriptor.Identity.VendorId,
        deviceDescriptor.Identity.ProductId);

    public async Task<IDeviceDriver?> CreateAsync(DeviceDescriptor deviceDescriptor,
        CancellationToken cancellationToken)
    {
        try
        {
            var hidConnection = await _hidTransportFactory.CreateAsync(deviceDescriptor.Path);

            var hidDeviceDriver = new HidDeviceDriver(
                deviceDescriptor.Identity,
                hidConnection.Transport,
                new HidReportDecoder(hidConnection.ReportDescriptor));

            var serialTransport = CreateSerialTransport(deviceDescriptor);
            if (serialTransport is null)
            {
                await hidConnection.Transport.DisposeAsync();
                return null;
            }

            var fcdSession = new FcdSession(serialTransport, _timeProvider, _loggerFactory.CreateLogger<FcdSession>());
            return new MozaFcdDeviceDriver(hidDeviceDriver, fcdSession);
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

    private ISerialTransport? CreateSerialTransport(DeviceDescriptor deviceDescriptor)
    {
        const int mozaBaudRate = 2_000_000;

        var serialPortName = _serialPortLocator.FindSiblingPort(deviceDescriptor);
        return string.IsNullOrWhiteSpace(serialPortName)
            ? null
            : _serialTransportFactory.Create(new SerialSettings(serialPortName, mozaBaudRate, false, false));
    }
}