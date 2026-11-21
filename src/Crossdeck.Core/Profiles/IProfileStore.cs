using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileStore
{
    Task<IReadOnlyList<ProfileSummary>> ListAsync(CancellationToken cancellationToken);

    Task<ProfileLoadResult> LoadAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(Profile profile, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}