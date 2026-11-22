using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileRuntime
{
    Profile? Active { get; }

    event EventHandler<Profile?>? ActiveChanged;

    IReadOnlyList<ProfileProblem> Activate(Profile profile);

    void Deactivate();
}