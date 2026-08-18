namespace GtaAutoGameplay.RepositoryGuard;

public static class RepositoryGuardApplication
{
    public static Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            RepositoryGuardOptions options = RepositoryGuardOptions.Parse(args);
            RepositoryAllowlist allowlist = RepositoryAllowlist.Load(options.AllowlistPath);
            GitRepositoryReader reader = new(options.RepositoryRoot);
            List<RepositoryFile> files = [.. reader.ReadCandidateWorkingTreeFiles()];

            if (options.ScanHistory)
            {
                files.AddRange(reader.ReadReachableHistoryFiles());
            }

            RepositoryScanner scanner = new();
            IReadOnlyList<ScanFinding> findings = scanner.Scan(files, allowlist);
            return Task.FromResult(RepositoryGuardReporter.WriteReport(findings, output));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            error.WriteLine($"Repository guard could not complete: {exception.Message}");
            return Task.FromResult(2);
        }
    }
}
