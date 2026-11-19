using System.Collections.Frozen;

namespace Crossdeck.Sim.XPlane;

internal sealed class WebApiSimVariableCache : ISimVariableCache
{
    private readonly IXPlaneApiService _apiService;

    private volatile FrozenDictionary<string, long> _simVariables = FrozenDictionary<string, long>.Empty;

    public WebApiSimVariableCache(
        IXPlaneApiService apiService)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
    }

    public long Count => _simVariables.Count;

    public bool TryGetId(string variableName, out long value) => _simVariables.TryGetValue(variableName, out value);

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var datarefs = await _apiService.GetDatarefsAsync(null, null, null, "id,name", cancellationToken);
        var commands = await _apiService.GetCommandsAsync(null, null, null, "id,name", cancellationToken);

        if (datarefs == null || commands == null)
            return;

        var result = datarefs.Data.ToDictionary(x => x.Name, x => x.Id);
        foreach (var command in commands.Data)
        {
            result[command.Name] = command.Id;
        }

        _simVariables = result.ToFrozenDictionary();
    }
}