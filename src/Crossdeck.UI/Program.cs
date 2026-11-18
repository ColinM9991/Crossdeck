using Avalonia;
using Crossdeck.Host;
using Crossdeck.UI.ViewModels;
using Crossdeck.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Crossdeck.UI;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        var settings = new HostApplicationBuilderSettings
        {
            Args = args,
        };

#if DEBUG
        settings.EnvironmentName = Environments.Development;
#endif

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(settings);

        builder.Services.Configure<HostOptions>(options =>
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

        builder.Services.AddHostServices()
            .AddSingleton<MainWindow>()
            .AddSingleton<MainViewModel>();

        using var host = builder.Build();

        BuildAvaloniaApp(host)
            .StartWithClassicDesktopLifetime(args);

        await host.StopAsync();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IHost host)
        => AppBuilder.Configure(() => new App(host))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}