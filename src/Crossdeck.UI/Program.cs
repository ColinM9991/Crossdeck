using Avalonia;
using System;
using System.Threading.Tasks;
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
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

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