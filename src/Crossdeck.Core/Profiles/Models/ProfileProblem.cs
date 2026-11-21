namespace Crossdeck.Core.Profiles.Models;

public sealed record ProfileProblem(
    string Path,
    string Message,
    ProblemSeverity Severity,
    string? BindingId = null,
    int? Position = null);