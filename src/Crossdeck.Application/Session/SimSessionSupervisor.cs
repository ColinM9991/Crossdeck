using Crossdeck.Core.Models;
using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;
using Crossdeck.Core.Session;

namespace Crossdeck.Application.Session;

public sealed class SimSessionSupervisor : ISimSessionSupervisor
{
    private readonly IReadOnlyList<ISimDetector> _detectors;
    private readonly Dictionary<Simulator, ISimProducerFactory> _factories;
    private volatile ISimProducer? _current;
    private CancellationTokenSource? _session;
    
    public SimSessionSupervisor(
        IEnumerable<ISimDetector> detectors,
        IEnumerable<ISimProducerFactory> factories)
    {
        _detectors = [.. detectors];
        _factories = factories.ToDictionary(f => f.Simulator);
    }
    
    public ISimProducer? Current => _current;
    
    public event EventHandler<ActiveSimChangedEventArgs>? Changed;
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(5);
        while (!cancellationToken.IsCancellationRequested)
        {
            var detection = await DetectAsync(cancellationToken);
            if (detection is null)
            {
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            _session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await using var producer = _factories[detection.Simulator].Create();
            SetCurrent(producer);

            try
            {
                await producer.RunAsync(_session.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
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
            if (!detector.CanProbe()) continue;
            var detection = await detector.ProbeAsync(cancellationToken);
            if (detection is not null)
            {
                return detection;
            }
        }

        return null;
    }
    
    private void SetCurrent(ISimProducer? current)
    {
        var previous = Current;
        _current = current;
        Changed?.Invoke(this, new ActiveSimChangedEventArgs(previous, current));
    }
}