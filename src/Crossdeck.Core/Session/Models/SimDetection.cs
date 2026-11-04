using Crossdeck.Core.Models;

namespace Crossdeck.Core.Session.Models;

public sealed record SimDetection
{
    public static SimDetection None => new();
    
    private SimDetection()
    {
    }
    
    public SimDetection(
        Simulator simulator)
    {
        Simulator = simulator;
    }
    
    public Simulator Simulator { get; }
}