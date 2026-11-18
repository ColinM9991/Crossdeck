using Crossdeck.Core.Models;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Session;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Session;

public sealed class SimSessionSupervisor : ISimSessionSupervisor
{
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);

    private readonly IReadOnlyList<ISimDetector> _detectors;
    private readonly Dictionary<Simulator, ISimProducerFactory> _factories;
    private readonly ILogger<SimSessionSupervisor> _logger;
    private readonly TimeSpan _retryDelay;
    private volatile ISimProducer? _current;
    private CancellationTokenSource? _session;

    public SimSessionSupervisor(
        IEnumerable<ISimDetector> detectors,
        IEnumerable<ISimProducerFactory> factories,
        ILogger<SimSessionSupervisor> logger,
        TimeSpan? retryDelay = null)
    {
        _detectors = [.. detectors];
        _factories = factories.ToDictionary(f => f.Simulator);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryDelay = retryDelay ?? DefaultRetryDelay;
    }

    public ISimProducer? Current => _current;

    public event EventHandler<ActiveSimChangingEventArgs>? Changing;

    public event EventHandler<ActiveSimChangedEventArgs>? Changed;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var detection = await DetectAsync(cancellationToken);
            if (detection is null)
            {
                await Task.Delay(_retryDelay, cancellationToken);
                continue;
            }

            if (!_factories.TryGetValue(detection.Simulator, out var factory))
            {
                _logger.LogWarning("No producer factory for detected simulator {Simulator}", detection.Simulator);
                await Task.Delay(_retryDelay, cancellationToken);
                continue;
            }

            _logger.LogInformation("Simulator detected {Simulator}", detection.Simulator);

            _session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                await using var producer = factory.Create();
                SetCurrent(producer);
                _logger.LogInformation("Simulator session started {Simulator}", detection.Simulator);

                try
                {
                    await producer.RunAsync(_session.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Simulator producer for {Simulator} faulted", detection.Simulator);
                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Simulator session for {Simulator} faulted", detection.Simulator);
                await Task.Delay(_retryDelay, cancellationToken);
            }
            finally
            {
                _logger.LogInformation("Simulator session ended {Simulator}", detection.Simulator);
                SetCurrent(null);
                _session.Dispose();
                _session = null;
            }
        }
    }

    private async ValueTask<SimDetection?> DetectAsync(CancellationToken cancellationToken)
    {
        foreach (var detector in _detectors)
        {
            try
            {
                if (!detector.CanProbe()) continue;

                var detection = await detector.ProbeAsync(cancellationToken);
                if (detection is not null)
                {
                    return detection;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulator detector {Detector} failed", detector.GetType().Name);
            }
        }

        return null;
    }

    private void SetCurrent(ISimProducer? next)
    {
        var previous = Current;
        Changing?.Invoke(this, new ActiveSimChangingEventArgs(previous, next));
        _current = next;
        _logger.LogInformation("Active sim changed {Simulator}", next?.GetType().Name ?? "none");
        Changed?.Invoke(this, new ActiveSimChangedEventArgs(_current));
    }
}