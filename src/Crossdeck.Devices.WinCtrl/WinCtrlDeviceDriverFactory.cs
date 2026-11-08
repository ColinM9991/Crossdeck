using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Exceptions;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.WinCtrl.Assemblers;

namespace Crossdeck.Devices.WinCtrl;

public sealed class WinCtrlDeviceDriverFactory : IDeviceDriverFactory
{
    private readonly IHidTransportFactory _hidTransportFactory;
    private readonly IReadOnlyDictionary<WinCtrlKind, IWinCtrlDriverAssembler> _assemblers;

    public WinCtrlDeviceDriverFactory(IHidTransportFactory hidTransportFactory,
        IEnumerable<IWinCtrlDriverAssembler> assemblers)
    {
        _hidTransportFactory = hidTransportFactory ?? throw new ArgumentNullException(nameof(hidTransportFactory));
        _assemblers = (assemblers ?? throw new ArgumentNullException(nameof(assemblers))).ToDictionary(a => a.Kind);
    }

    public int Priority => 100;

    public bool CanCreate(DeviceDescriptor deviceDescriptor) =>
        deviceDescriptor.Identity.VendorId == 0x4098 &&
        WinCtrlDefinitions.ForPid(deviceDescriptor.Identity.ProductId) is { } definition &&
        _assemblers.ContainsKey(definition.Kind);

    public async Task<IDeviceDriver?> CreateAsync(DeviceDescriptor deviceDescriptor,
        CancellationToken cancellationToken)
    {
        var definition = WinCtrlDefinitions.ForPid(deviceDescriptor.Identity.ProductId);
        if (definition is null) return null;
        if (!_assemblers.TryGetValue(definition.Kind, out var assembler)) return null;

        try
        {
            var hidConnection = await _hidTransportFactory.CreateAsync(deviceDescriptor.Path);

            try
            {
                var hidDevice = new HidDeviceDriver(
                    deviceDescriptor.Identity,
                    hidConnection.Transport,
                    new HidReportDecoder(hidConnection.ReportDescriptor));

                return assembler.Assemble(definition, hidDevice, hidConnection.Transport);
            }
            catch
            {
                await hidConnection.Transport.DisposeAsync();
                throw;
            }
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
}