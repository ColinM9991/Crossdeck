using Crossdeck.Devices.Serial.Pairing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WindowsSerialPortLocator = Crossdeck.Devices.Serial.Pairing.WindowsSerialPortLocator;

namespace Crossdeck.Devices.Serial;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMozaServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ISerialTransportFactory, SerialTransportFactory>();

        if (OperatingSystem.IsWindows())
            services.AddSingleton<ISerialPortLocator, WindowsSerialPortLocator>();
        else
            services.AddSingleton<ISerialPortLocator, NullSerialPortLocator>();

        return services;
    }
}