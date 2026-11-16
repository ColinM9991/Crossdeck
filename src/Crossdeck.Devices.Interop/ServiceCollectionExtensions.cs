using Crossdeck.Core.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.Devices.Interop;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInteropServices(this IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IUsbDeviceResolver, WindowsUsbDeviceResolver>();
        else
            services.AddSingleton<IUsbDeviceResolver, NullUsbDeviceResolver>();

        return services;
    }
}
