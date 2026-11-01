namespace Crossdeck.Core;

public interface IRunnable
{
    Task RunAsync(CancellationToken cancellationToken);
}