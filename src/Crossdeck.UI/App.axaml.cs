using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Crossdeck.UI.ViewModels;
using Crossdeck.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Crossdeck.UI;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    
    public App(IServiceProvider serviceProvider)
    {
        _services = serviceProvider;
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow.DataContext = _services.GetRequiredService<MainViewModel>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}