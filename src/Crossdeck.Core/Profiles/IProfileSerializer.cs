using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileSerializer
{
    Task<ProfileLoadResult> ReadAsync(Stream stream, CancellationToken cancellationToken);

    Task WriteAsync(Profile profile, Stream stream, CancellationToken cancellationToken);
}