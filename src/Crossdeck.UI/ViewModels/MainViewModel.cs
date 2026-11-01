using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Crossdeck.Core.Session;
using Crossdeck.Core.Simulators;

namespace Crossdeck.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IActiveSim _activeSim;
    public MainViewModel(IActiveSim activeSim)
    {
        _activeSim = activeSim;
        activeSim.Changed += OnSimChanged;
    }
    
    [ObservableProperty] public partial string? Greeting { get; set; } = "Welcome to Avalonia!";

    public async void OnSimChanged(object? sender, ActiveSimChangedEventArgs args)
    {
        if (args.Current is null) return;

        var current = args.Current;
        
        await foreach (var simEvent in current.Events)
        {
            Greeting = simEvent switch
            {
                SimEvent.AircraftChanged path => path.AircraftId,
                _ => Greeting,
            };
        }
    }
}