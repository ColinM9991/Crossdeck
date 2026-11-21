using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileValidator
{
    Task<IReadOnlyList<ProfileProblem>> ValidateAsync(Profile profile, CancellationToken cancellationToken);
}