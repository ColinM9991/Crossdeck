using Crossdeck.Core.Models;

namespace Crossdeck.Core.Profiles.Models;

public sealed record ProfileSummary(
    Guid Id,
    string Name,
    Simulator Simulator,
    AircraftMatch Aircraft);