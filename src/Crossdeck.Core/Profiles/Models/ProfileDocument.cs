namespace Crossdeck.Core.Profiles.Models;

public sealed record ProfileDocument(
    Profile Profile,
    int FileVersion,
    bool Migrated);