using CommunityToolkit.Mvvm.ComponentModel;

namespace Crossdeck.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
    }

    [ObservableProperty] public partial string? Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty] public partial string? Status { get; set; } = "Disconnected";
}