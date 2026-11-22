using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public closed record ProfileLoadResult
{
    public sealed record Loaded(ProfileDocument Document) : ProfileLoadResult;

    public sealed record TooNew(int FileVersion, int AppVersion) : ProfileLoadResult;

    public sealed record Invalid(IReadOnlyList<ProfileProblem> Problems) : ProfileLoadResult;

    public sealed record NotFound() : ProfileLoadResult;
}