using Crossdeck.Core.Devices;
using Crossdeck.Devices.WinCtrl.Assemblers;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Devices.WinCtrl;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWinCtrlDevices(this IServiceCollection services)
    {
        services.AddSingleton<IWinCtrlDriverAssembler, CduDriverAssembler>();
        services.AddSingleton<IWinCtrlDriverAssembler, SegmentDriverAssembler>();
        services.AddSingleton<IWinCtrlDriverAssembler, IndicatorDriverAssembler>();
        services.AddSingleton<IDeviceDriverFactory, WinCtrlDeviceDriverFactory>();
        return services;
    }
}