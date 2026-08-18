namespace GtaAutoGameplay.RepositoryGuard;

public sealed record ScanFinding(
    string RuleId,
    string Path,
    string SecurityNote,
    string? HistoryObjectId = null);
