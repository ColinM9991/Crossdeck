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
        
        return services;
    }
}