using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileSerializer
{
    Task<ProfileLoadResult> LoadAsync(Stream stream, CancellationToken cancellationToken);

    Task SaveAsync(Profile profile, Stream stream, CancellationToken cancellationToken);
}