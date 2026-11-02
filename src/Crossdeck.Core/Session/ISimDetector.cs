namespace Crossdeck.Core.Session;

public interface ISimDetector
{
    bool CanProbe();
    
    Task<SimDetection?> ProbeAsync(CancellationToken cancellationToken);
}