using Crossdeck.Core.Devices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crossdeck.Devices.Moza;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMozaServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IDeviceDriverFactory, MozaDeviceDriverFactory>();

        return services;
    }
}