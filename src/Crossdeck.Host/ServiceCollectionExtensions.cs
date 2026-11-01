using Crossdeck.Core;
using Crossdeck.Core.Session;
using Crossdeck.Sim.XPlane;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Host;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostServices(this IServiceCollection services)
    {
        services
            .AddSimulationServices()
            .AddXPlane();
        
        services.AddHostedService<LoopService>(sp => new LoopService(sp.GetRequiredService<ISimSessionSupervisor>()));

        return services;
    }
}