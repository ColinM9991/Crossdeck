using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Application.Profiles;

internal sealed class ProfileStore : IProfileStore
{
    private readonly string _profilesPath;
    private readonly IProfileSerializer _profileSerializer;


    public ProfileStore(
        IProfileSerializer profileSerializer)
    {
        _profileSerializer = profileSerializer ?? throw new ArgumentNullException(nameof(profileSerializer));

        var applicationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Crossdeck");

        _profilesPath = Path.Combine(applicationPath, "Profiles");
    }

    public async Task<IReadOnlyList<ProfileSummary>> ListAsync(CancellationToken cancellationToken) => await LoadAllProfiles(cancellationToken);

    public async Task<ProfileLoadResult> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var file = Path.Combine(_profilesPath, $"{id}.json");
        if (!File.Exists(file))
            return new ProfileLoadResult.NotFound();

        await using var fileStream = File.Open(file, FileMode.Open);
        var profileLoadResult = await _profileSerializer.ReadAsync(fileStream, cancellationToken);

        return profileLoadResult;
    }

    public async Task SaveAsync(Profile profile, CancellationToken cancellationToken)
    {
        var tempFilePath = Path.Combine(_profilesPath, $"{profile.Id}.json.tmp");

        await using (var fileStream = File.Create(tempFilePath))
            await _profileSerializer.WriteAsync(profile, fileStream, cancellationToken);

        var targetFilePath = Path.Combine(_profilesPath, $"{profile.Id}.json");
        File.Move(tempFilePath, targetFilePath, overwrite: true);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var file = Path.Combine(_profilesPath, $"{id}.json");
        if (!File.Exists(file))
            return Task.CompletedTask;

        File.Delete(file);

        return Task.CompletedTask;
    }

    private async Task<IReadOnlyList<ProfileSummary>> LoadAllProfiles(CancellationToken cancellationToken)
    {
        var profileSummaries = new List<ProfileSummary>();
        foreach (var file in Directory.EnumerateFiles(_profilesPath, "*.json"))
        {
            try
            {
                await using var fileStream = File.OpenRead(file);
                var profile = await _profileSerializer.ReadAsync(fileStream, cancellationToken);
                if (profile is not ProfileLoadResult.Loaded res)
                    continue;

                profileSummaries.Add(new ProfileSummary(
                    res.Document.Profile.Id,
                    res.Document.Profile.Name,
                    res.Document.Profile.Simulator,
                    res.Document.Profile.Aircraft));
            }
            catch (IOException)
            {
                continue;
            }
        }

        return profileSummaries;
    }
}