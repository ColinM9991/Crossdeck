using Crossdeck.Core;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Session;
using Crossdeck.Devices.Hid;
using Crossdeck.Sim.XPlane;
#if WINDOWS
using Crossdeck.Sim.MSFS;
#endif
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Host;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostServices(this IServiceCollection services)
    {
        services
            .AddSimulationServices()
            .AddHidServices()
            .AddXPlane();
        
        #if WINDOWS
        services.AddMsfs();
        #endif

        services.AddHostedService<LoopService<SimSessionSupervisor>>();
        services.AddHostedService<LoopService<DeviceRegistry>>();

        return services;
    }
}