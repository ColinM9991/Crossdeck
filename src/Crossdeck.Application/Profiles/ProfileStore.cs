using System.Collections.Immutable;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;

namespace Crossdeck.Application.Profiles;

internal sealed class ProfileStore : IProfileStore
{
    private readonly string _profilesPath;
    private readonly IProfileSerializer _profileSerializer;
    private ImmutableArray<ProfileSummary> _profileSummaries;


    public ProfileStore(
        IProfileSerializer profileSerializer)
    {
        _profileSerializer = profileSerializer ?? throw new ArgumentNullException(nameof(profileSerializer));
        _profileSummaries = ImmutableArray<ProfileSummary>.Empty;

        var applicationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Crossdeck");

        _profilesPath = Path.Combine(applicationPath, "Profiles");
    }

    public async Task<IReadOnlyList<ProfileSummary>> ListAsync(CancellationToken cancellationToken)
    {
        if (_profileSummaries.IsEmpty)
            await LoadAllProfiles(cancellationToken);

        return _profileSummaries;
    }

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
        var tempFilePath = Path.Combine(_profilesPath, $"{profile.Id}.tmp.json");
        var fileMode = File.Exists(tempFilePath) ? FileMode.Truncate : FileMode.OpenOrCreate;

        await using var fileStream = File.Open(tempFilePath, fileMode);

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

    private async Task LoadAllProfiles(CancellationToken cancellationToken)
    {
        foreach (var file in Directory.EnumerateFiles(_profilesPath, "*.json"))
        {
            await using var fileStream = File.Open(file, FileMode.Open);
            var profile = await _profileSerializer.ReadAsync(fileStream, cancellationToken);
            if (profile is not ProfileLoadResult.Loaded res)
                continue;

            _profileSummaries = _profileSummaries.Add(new ProfileSummary(
                res.Document.Profile.Id,
                res.Document.Profile.Name,
                res.Document.Profile.Simulator,
                res.Document.Profile.Aircraft));
        }
    }
}