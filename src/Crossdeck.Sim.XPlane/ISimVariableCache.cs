namespace Crossdeck.Sim.XPlane;

internal interface ISimVariableCache
{
    long Count { get; }

    bool TryGetId(string variableName, out long value);

    Task RefreshAsync(CancellationToken cancellationToken);
}