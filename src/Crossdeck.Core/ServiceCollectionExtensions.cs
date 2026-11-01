using Crossdeck.Core.Session;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationServices(this IServiceCollection services)
    {
        services.AddSingleton<IActiveSim, SimSessionSupervisor>();
        services.AddSingleton<ISimSessionSupervisor, SimSessionSupervisor>();
        
        return services;
    }
}