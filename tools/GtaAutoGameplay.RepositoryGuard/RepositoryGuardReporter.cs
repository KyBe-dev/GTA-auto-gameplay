namespace GtaAutoGameplay.RepositoryGuard;

public static class RepositoryGuardReporter
{
    public static int WriteReport(
        IReadOnlyCollection<ScanFinding> findings,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(output);

        if (findings.Count == 0)
        {
            output.WriteLine("Repository guard passed: no blocked tracked or candidate files were detected.");
            output.WriteLine("This baseline scanner cannot prove that a repository is secret-free and does not replace a mature secret-scanning audit.");
            return 0;
        }

        output.WriteLine($"Repository guard failed with {findings.Count} finding(s). Suspected secret values are never printed.");

        foreach (ScanFinding finding in findings)
        {
            string history = finding.HistoryObjectId is null
                ? string.Empty
                : $" [history object {finding.HistoryObjectId[..Math.Min(12, finding.HistoryObjectId.Length)]}]";
            output.WriteLine($"[{finding.RuleId}] {finding.Path}{history}: {finding.SecurityNote}");
        }

        return 1;
    }
}
