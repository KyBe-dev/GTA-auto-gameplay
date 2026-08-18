using System.Text.RegularExpressions;

namespace GtaAutoGameplay.RepositoryGuard.Tests;

[TestClass]
public sealed class WorkflowSecurityTests
{
    [TestMethod]
    public void RepositorySecurityWorkflow_UsesMinimumPermissionsAndPinnedActions()
    {
        string repositoryRoot = FindRepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(
            repositoryRoot,
            ".github",
            "workflows",
            "repository-security.yml"));

        StringAssert.Contains(workflow, "permissions:\n  contents: read");
        Assert.IsFalse(workflow.Contains("contents: write", StringComparison.Ordinal));
        StringAssert.Contains(workflow, "fetch-depth: 0");
        StringAssert.Contains(workflow, "persist-credentials: false");
        StringAssert.Contains(workflow, "--history");

        MatchCollection actionReferences = Regex.Matches(
            workflow,
            "(?m)^\\s*uses:\\s*[^@\\s]+@(?<reference>[^\\s#]+)",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        Assert.HasCount(2, actionReferences.Cast<Match>().ToArray());
        foreach (Match actionReference in actionReferences)
        {
            Assert.IsTrue(Regex.IsMatch(
                actionReference.Groups["reference"].Value,
                "^[0-9a-f]{40}$",
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)));
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root for workflow validation.");
    }
}
