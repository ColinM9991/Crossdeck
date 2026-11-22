using Crossdeck.Core.Devices;
using Crossdeck.Core.Session;
using Crossdeck.Application.Devices;
using Crossdeck.Application.Expressions;
using Crossdeck.Application.Profiles;
using Crossdeck.Application.Profiles.Validation;
using Crossdeck.Application.Routing;
using Crossdeck.Application.Routing.Adapters;
using Crossdeck.Application.Routing.Input;
using Crossdeck.Application.Routing.Output;
using Crossdeck.Application.Session;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Routing.Adapters;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crossdeck.Application;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSimulationServices()
        {
            services.TryAddSingleton(TimeProvider.System);

            services.AddProfileServices()
                .AddSimSupervisorServices()
                .AddDeviceServices()
                .AddInputServices()
                .AddOutputServices()
                .AddCommonServices()
                .AddAggregatorServices();

            return services;
        }

        private IServiceCollection AddProfileServices()
        {
            return services.AddSingleton<IProfileRuntime, ProfileRuntime>()
                .AddSingleton<IProfileSerializer, ProfileSerializer>()
                .AddSingleton<IProfileStore, ProfileStore>()
                .AddSingleton<IValidator<Profile>, ProfileRootValidator>()
                .AddSingleton<IProfileValidator, ProfileValidator>();
        }

        private IServiceCollection AddSimSupervisorServices()
        {
            return services.AddSingleton<SimSessionSupervisor>()
                .AddSingleton<IActiveSim>(sp => sp.GetRequiredService<SimSessionSupervisor>())
                .AddSingleton<ISimSessionSupervisor>(sp => sp.GetRequiredService<SimSessionSupervisor>());
        }

        private IServiceCollection AddDeviceServices()
        {
            return services.AddSingleton<DeviceRegistry>()
                .AddSingleton<IDeviceRegistry>(sp => sp.GetRequiredService<DeviceRegistry>());
        }

        private IServiceCollection AddInputServices()
        {
            return services.AddSingleton<InputDispatcher>()
                .AddSingleton<IInputSink>(sp => sp.GetRequiredService<InputDispatcher>())
                .AddSingleton<IInputDispatcher>(sp => sp.GetRequiredService<InputDispatcher>())
                .AddSingleton<IInputMonitor>(sp => sp.GetRequiredService<InputDispatcher>());
        }

        private IServiceCollection AddOutputServices()
        {
            return services.AddSingleton<SimValueRouter>()
                .AddSingleton<ISimValueRouter>(sp => sp.GetRequiredService<SimValueRouter>());
        }

        private IServiceCollection AddCommonServices()
        {
            return
                services.AddSingleton<ISimValueCache, SimValueCache>()
                    .AddSingleton<IVariableStore, VariableStore>()
                    .AddSingleton<IExpressionCompiler, NCalcExpressionCompiler>()
                    .AddSingleton<IAircraftScreenAdapterFactory, AircraftScreenAdapterFactory>();
        }

        private IServiceCollection AddAggregatorServices()
        {
            return services.AddSingleton<SubscriptionAggregator>()
                .AddSingleton<ISubscriptionAggregator>(sp => sp.GetRequiredService<SubscriptionAggregator>());
        }
    }
}