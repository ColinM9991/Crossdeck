using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileValidator
{
    IReadOnlyList<ProfileProblem> Validate(Profile profile);
}
