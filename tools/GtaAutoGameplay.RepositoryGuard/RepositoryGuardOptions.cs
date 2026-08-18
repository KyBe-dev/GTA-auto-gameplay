namespace GtaAutoGameplay.RepositoryGuard;

public sealed record RepositoryGuardOptions(
    string RepositoryRoot,
    string AllowlistPath,
    bool ScanHistory)
{
    public static RepositoryGuardOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string repositoryRoot = Directory.GetCurrentDirectory();
        string? allowlistPath = null;
        bool scanHistory = false;

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--repository":
                    repositoryRoot = ReadValue(args, ref index, "--repository");
                    break;
                case "--allowlist":
                    allowlistPath = ReadValue(args, ref index, "--allowlist");
                    break;
                case "--history":
                    scanHistory = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown repository guard argument '{args[index]}'.");
            }
        }

        repositoryRoot = Path.GetFullPath(repositoryRoot);
        allowlistPath ??= Path.Combine(repositoryRoot, "tools", "repository-guard.allowlist.json");
        allowlistPath = Path.GetFullPath(allowlistPath, repositoryRoot);

        EnsurePathIsWithinRepository(repositoryRoot, allowlistPath);
        return new RepositoryGuardOptions(repositoryRoot, allowlistPath, scanHistory);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"Option '{option}' requires a value.");
        }

        return args[index];
    }

    private static void EnsurePathIsWithinRepository(string repositoryRoot, string path)
    {
        string rootPrefix = repositoryRoot.EndsWith(Path.DirectorySeparatorChar)
            ? repositoryRoot
            : repositoryRoot + Path.DirectorySeparatorChar;

        if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The allowlist must be stored inside the repository.");
        }
    }
}
