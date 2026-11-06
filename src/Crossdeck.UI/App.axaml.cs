using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Crossdeck.UI.ViewModels;
using Crossdeck.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Crossdeck.UI;

public partial class App : Avalonia.Application
{
    private readonly IHost _host;
    private bool _hostStarted;

    public App(IHost host)
    {
        _host = host;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
            mainWindow.Opened += OnMainWindowOpened;
            mainWindow.Closed += OnMainWindowClosed;

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnMainWindowOpened(object? sender, EventArgs e)
    {
        if (_hostStarted)
        {
            return;
        }

        _hostStarted = true;

        try
        {
            await _host.StartAsync();
        }
        catch (Exception)
        {
            // Ignored
        }
    }
    
    private async void OnMainWindowClosed(object? sender, EventArgs e)
    {
        if (!_hostStarted) return;
        _hostStarted = false;
        await _host.StopAsync();
    }
}