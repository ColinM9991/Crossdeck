namespace Crossdeck.Core.Session;

public interface ISimDetector
{
    Task<SimDetection?> ProbeAsync(CancellationToken cancellationToken);
}