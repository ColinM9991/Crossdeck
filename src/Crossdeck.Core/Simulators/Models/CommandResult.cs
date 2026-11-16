namespace Crossdeck.Core.Simulators.Models;

public closed record CommandResult
{
    public sealed record Success : CommandResult;

    public sealed record Failure : CommandResult;

    public sealed record Unsupported : CommandResult;

    public sealed record NoConnection : CommandResult;

    public sealed record NotReady : CommandResult;
}