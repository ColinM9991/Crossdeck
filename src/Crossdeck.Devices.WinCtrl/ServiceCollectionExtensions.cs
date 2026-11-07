using Crossdeck.Core.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Devices.WinCtrl;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWinCtrlDevices(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceDriverFactory, WinCtrlDeviceDriverFactory>();
        return services;
    }
}