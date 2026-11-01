using Crossdeck.Core;
using Microsoft.Extensions.Hosting;

namespace Crossdeck.Host;

public sealed class LoopService(IRunnable runner) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => runner.RunAsync(stoppingToken);
}