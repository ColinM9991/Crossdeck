using Crossdeck.Core;
using Microsoft.Extensions.Hosting;

namespace Crossdeck.Host;

public sealed class SupervisorService<T>(T runner) : BackgroundService where T : IRunnable
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await runner.RunAsync(stoppingToken);
    }
}