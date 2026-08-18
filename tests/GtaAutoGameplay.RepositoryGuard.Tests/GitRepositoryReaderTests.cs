using System.Diagnostics;

namespace GtaAutoGameplay.RepositoryGuard.Tests;

[TestClass]
public sealed class GitRepositoryReaderTests
{
    [TestMethod]
    public void ReachableHistory_IncludesDeletedSyntheticSecretWithoutChangingRepositoryFiles()
    {
        string testRoot = Path.Combine(
            Path.GetTempPath(),
            "GtaAutoGameplay.RepositoryGuard.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);

        try
        {
            RunGit(testRoot, "init");
            string trackedPath = Path.Combine(testRoot, "historical.txt");
            string syntheticToken = "gh" + "p_" + new string('H', 36);
            File.WriteAllText(trackedPath, syntheticToken);
            RunGit(testRoot, "add", "historical.txt");
            RunGit(
                testRoot,
                "-c", "user.name=Repository Guard Tests",
                "-c", "user.email=repository-guard-tests@invalid.example",
                "commit", "-m", "add synthetic history sample");

            File.Delete(trackedPath);
            RunGit(testRoot, "add", "--all");
            RunGit(
                testRoot,
                "-c", "user.name=Repository Guard Tests",
                "-c", "user.email=repository-guard-tests@invalid.example",
                "commit", "-m", "remove synthetic history sample");

            GitRepositoryReader reader = new(testRoot);
            IReadOnlyList<RepositoryFile> history = reader.ReadReachableHistoryFiles();
            IReadOnlyList<ScanFinding> findings = new RepositoryScanner().Scan(history);

            Assert.IsFalse(File.Exists(trackedPath));
            Assert.IsTrue(findings.Any(finding =>
                finding.Path == "historical.txt"
                && finding.RuleId == RepositoryGuardRuleIds.AccessToken
                && finding.HistoryObjectId is not null));
            Assert.IsFalse(string.Join(Environment.NewLine, findings).Contains(
                syntheticToken,
                StringComparison.Ordinal));
        }
        finally
        {
            string fullTestRoot = Path.GetFullPath(testRoot);
            string expectedParent = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                "GtaAutoGameplay.RepositoryGuard.Tests")) + Path.DirectorySeparatorChar;

            if (fullTestRoot.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(fullTestRoot))
            {
                foreach (string file in Directory.EnumerateFiles(
                    fullTestRoot,
                    "*",
                    SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(fullTestRoot, recursive: true);
            }
        }
    }

    private static void RunGit(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start Git for an isolated test repository.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.AreEqual(
            0,
            process.ExitCode,
            $"Git test setup failed. Output: {standardOutput} Error: {standardError}");
    }
}
