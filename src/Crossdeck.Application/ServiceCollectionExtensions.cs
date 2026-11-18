using Crossdeck.Core.Devices;
using Crossdeck.Core.Session;
using Crossdeck.Application.Devices;
using Crossdeck.Application.Routing;
using Crossdeck.Application.Session;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crossdeck.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimulationServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<SimSessionSupervisor>();
        services.AddSingleton<IActiveSim>(sp => sp.GetRequiredService<SimSessionSupervisor>());
        services.AddSingleton<ISimSessionSupervisor>(sp => sp.GetRequiredService<SimSessionSupervisor>());

        services.AddSingleton<DeviceRegistry>();
        services.AddSingleton<IDeviceRegistry>(sp => sp.GetRequiredService<DeviceRegistry>());

        services.AddSingleton<InputDispatcher>();
        services.AddSingleton<IInputSink>(sp => sp.GetRequiredService<InputDispatcher>());
        services.AddSingleton<IInputDispatcher>(sp => sp.GetRequiredService<InputDispatcher>());
        services.AddSingleton<IInputMonitor>(sp => sp.GetRequiredService<InputDispatcher>());

        services.AddSingleton<SimValueRouter>();
        services.AddSingleton<ISimValueRouter>(sp => sp.GetRequiredService<SimValueRouter>());
        services.AddSingleton<ISimValueCache, SimValueCache>();

        services.AddSingleton<SubscriptionAggregator>();
        services.AddSingleton<ISubscriptionAggregator>(sp => sp.GetRequiredService<SubscriptionAggregator>());

        return services;
    }
}