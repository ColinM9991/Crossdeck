using Crossdeck.Core.Devices;
using Crossdeck.Core.Session;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationServices(this IServiceCollection services)
    {
        services.AddSingleton<SimSessionSupervisor>();
        services.AddSingleton<IActiveSim>(sp => sp.GetRequiredService<SimSessionSupervisor>());
        services.AddSingleton<ISimSessionSupervisor>(sp => sp.GetRequiredService<SimSessionSupervisor>());

        services.AddSingleton<DeviceRegistry>();
        services.AddSingleton<IDeviceRegistry>(sp => sp.GetRequiredService<IDeviceRegistry>());
        
        return services;
    }
}