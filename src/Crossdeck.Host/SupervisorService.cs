using Crossdeck.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Host;

/// <summary>
/// Runs an <see cref="IRunnable"/> as a background service, restarting it with a backoff delay when it faults.
/// </summary>
/// <param name="runner">The component to run.</param>
/// <param name="logger">A logger for the supervised loop.</param>
/// <param name="backoff">The delay between restarts, or <c>null</c> to use the default of five seconds.</param>
public sealed class SupervisorService<T>(
    T runner,
    ILogger<SupervisorService<T>> logger,
    TimeSpan? backoff = null) : BackgroundService where T : IRunnable
{
    private static readonly TimeSpan DefaultBackoff = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var restartDelay = backoff ?? DefaultBackoff;

        logger.LogInformation("Supervised loop {Runner} starting", typeof(T).Name);

        while (true)
        {
            try
            {
                await runner.RunAsync(stoppingToken);

                logger.LogInformation("Supervised loop {Runner} completed", typeof(T).Name);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Supervised loop {Runner} stopped", typeof(T).Name);
                return;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Supervised loop {Runner} cancelled; restarting in {Backoff}", typeof(T).Name,
                    restartDelay);

                if (!await DelayBeforeRestartAsync(restartDelay, stoppingToken))
                    return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Supervised loop {Runner} faulted; restarting in {Backoff}", typeof(T).Name,
                    restartDelay);

                if (!await DelayBeforeRestartAsync(restartDelay, stoppingToken))
                    return;
            }
        }
    }

    private static async Task<bool> DelayBeforeRestartAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
