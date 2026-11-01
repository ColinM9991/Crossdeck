using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Crossdeck.Core.Simulators;
using Crossdeck.Sim.XPlane;

namespace Crossdeck.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISimProducer _simProducer = new XPlaneApiSimProducer(new HttpClient
        { BaseAddress = new Uri("http://localhost:8086/api/v3/") });

    [ObservableProperty] public partial string? Greeting { get; set; } = "Welcome to Avalonia!";

    [RelayCommand]
    public async Task BeginConnection()
    {
        Task.Run(async () => await _simProducer.RunAsync(CancellationToken.None));
        
        await foreach (var simEvent in _simProducer.Events)
        {
            Greeting = simEvent switch
            {
                SimEvent.AircraftChanged path => path.AircraftId,
                _ => "Nothing",
            };
        }
    }
}