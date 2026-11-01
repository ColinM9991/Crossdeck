namespace Crossdeck.Core.Session;

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