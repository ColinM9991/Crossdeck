using Crossdeck.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Host;

public sealed class SupervisorService<T>(T runner, ILogger<SupervisorService<T>> logger) : BackgroundService where T : IRunnable
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            await runner.RunAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Supervised loop {Runner} faulted", typeof(T).Name);
        }
    }
}
