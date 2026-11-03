using Crossdeck.Core.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Devices.Hid;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHidServices(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceWatcher, HidDeviceWatcher>();
        services.AddSingleton<IDeviceDriverFactory, HidDeviceDriverFactory>();
        services.AddSingleton<IHidTransportFactory, HidTransportFactory>();
        
        return services;
    }
}