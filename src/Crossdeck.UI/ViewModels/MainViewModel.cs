using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators.Models;

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

    [ObservableProperty] public partial string? Status { get; set; } = "Disconnected";

    public async void OnSimChanged(object? sender, ActiveSimChanged args)
    {
        if (args.Current is null) return;

        var current = args.Current;
        
        await foreach (var simEvent in current.Events)
        {
            switch (simEvent)
            {
                case SimEvent.AircraftChanged aircraftChanged:
                    Greeting = aircraftChanged.AircraftId;
                    break;
                case SimEvent.StatusChanged status:
                    Status = status.Status switch
                    {
                        SimStatus.Connected => "Connected",
                        SimStatus.Disconnected => "Disconnected",
                        SimStatus.Gone => "Gone",
                        _ => "Invalid"
                    };
                    break;
            }
        }
    }
}