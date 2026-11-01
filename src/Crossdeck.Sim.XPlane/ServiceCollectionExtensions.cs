using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Sim.XPlane;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddXPlane(this IServiceCollection services)
    {
        services.AddSingleton<ISimProducerFactory, XPlaneProducerFactory>();
        services.AddTransient<ISimProducer, XPlaneApiSimProducer>();
        services.AddSingleton<ISimDetector, XPlaneApiSimDetector>();

        services.AddTransient<XPlaneOptions>(_ => new XPlaneOptions
        {
            Host = "localhost",
            Port = 8086
        });

        services.AddHttpClient("xplane", opts =>
        {
            opts.BaseAddress = new Uri("http://localhost:8086/api/v3/");
        });
        
        return services;
    }
}