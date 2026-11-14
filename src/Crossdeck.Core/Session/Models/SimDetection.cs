using Crossdeck.Core.Models;

namespace Crossdeck.Core.Session.Models;

public sealed record SimDetection
{
    public SimDetection(
        Simulator simulator)
    {
        Simulator = simulator;
    }

    private SimDetection()
    {
    }

    public static SimDetection None => new();
    
    public Simulator Simulator { get; }
}