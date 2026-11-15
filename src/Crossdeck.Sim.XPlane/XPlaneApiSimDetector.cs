using System.Diagnostics;
using Crossdeck.Core.Models;
using Crossdeck.Core.Session;
using Crossdeck.Core.Session.Models;
using Crossdeck.Sim.XPlane.Api;

namespace Crossdeck.Sim.XPlane;

public class XPlaneApiSimDetector(IHttpClientFactory httpClientFactory) : ISimDetector
{
    public bool CanProbe() => Process.GetProcessesByName("X-Plane").Length > 0 || Process.GetProcessesByName("X-Ssytem").Length > 0;

    public async Task<SimDetection?> ProbeAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        
        var client = httpClientFactory.CreateClient(XPlaneConstants.HttpClientName);
        try
        {
            var capabilities = await client
                .GetFromJsonAsync<Capabilities?>(XPlaneConstants.CapabilitiesPath, timeout.Token);

            return capabilities is null ? null : new SimDetection(Simulator.XPlane);
        }
        catch (HttpRequestException) { }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }

        return null;
    }
}