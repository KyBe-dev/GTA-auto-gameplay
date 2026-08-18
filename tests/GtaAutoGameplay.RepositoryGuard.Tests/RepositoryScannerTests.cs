using System.Text;

namespace GtaAutoGameplay.RepositoryGuard.Tests;

[TestClass]
public sealed class RepositoryScannerTests
{
    private readonly RepositoryScanner _scanner = new();

    [TestMethod]
    public void SafeSourceAndDocumentation_Pass()
    {
        RepositoryFile[] files =
        [
            RepositoryFile.FromText("src/Safe.cs", "namespace Safe; public sealed class Value;") ,
            RepositoryFile.FromText(
                "src/Product.Core/Credentials/ICredentialStore.cs",
                "namespace Product.Core.Credentials; public interface ICredentialStore;") ,
            RepositoryFile.FromText("docs/SAFE.md", "This is ordinary project documentation."),
        ];

        Assert.IsEmpty(_scanner.Scan(files));
    }

    [TestMethod]
    public void SyntheticCredentialPatterns_AreDetected()
    {
        string assigned = "api_" + "key=" + "synthetic" + new string('A', 24);
        string accessToken = "gh" + "p_" + new string('B', 36);
        string jsonWebToken = "ey" + "J" + new string('C', 12)
            + "." + new string('D', 12)
            + "." + new string('E', 12);

        IReadOnlyList<ScanFinding> findings = _scanner.Scan(
        [
            RepositoryFile.FromText(
                "tests/runtime-sample.txt",
                string.Join(Environment.NewLine, assigned, accessToken, jsonWebToken)),
        ]);

        CollectionAssert.AreEquivalent(
            new[]
            {
                RepositoryGuardRuleIds.AssignedSecret,
                RepositoryGuardRuleIds.AccessToken,
                RepositoryGuardRuleIds.JsonWebToken,
            },
            findings.Select(finding => finding.RuleId).ToArray());
    }

    [TestMethod]
    public void PrivateKeyMarker_IsDetectedWithoutLeakingContent()
    {
        string marker = "-----BEGIN " + "PRIVATE KEY-----";
        string syntheticMaterial = "synthetic-material-" + new string('Q', 40);
        RepositoryFile file = RepositoryFile.FromText(
            "tests/runtime-key.txt",
            marker + Environment.NewLine + syntheticMaterial);

        IReadOnlyList<ScanFinding> findings = _scanner.Scan([file]);
        StringWriter output = new();
        int exitCode = RepositoryGuardReporter.WriteReport(findings, output);
        string report = output.ToString();

        Assert.AreEqual(1, exitCode);
        Assert.IsTrue(findings.Any(finding => finding.RuleId == RepositoryGuardRuleIds.PrivateKey));
        StringAssert.Contains(report, RepositoryGuardRuleIds.PrivateKey);
        StringAssert.Contains(report, file.Path);
        Assert.IsFalse(report.Contains(marker, StringComparison.Ordinal));
        Assert.IsFalse(report.Contains(syntheticMaterial, StringComparison.Ordinal));
    }

    [TestMethod]
    public void ForbiddenDirectoriesAndFileTypes_AreDetected()
    {
        RepositoryFile[] files =
        [
            RepositoryFile.FromText("src/bin/Release/product.dll", "generated"),
            RepositoryFile.FromText("gta-saves/SGTA50000", "save-data"),
            RepositoryFile.FromText("captures/frame.png", "not-a-real-image"),
            RepositoryFile.FromText("models/model.onnx", "not-a-real-model"),
            RepositoryFile.FromText("logs/run.log", "diagnostic"),
            RepositoryFile.FromText(".env.local", "placeholder-only"),
            RepositoryFile.FromText("certificates/signing.pfx", "not-a-real-certificate"),
        ];

        IReadOnlyList<ScanFinding> findings = _scanner.Scan(files);

        Assert.IsTrue(findings.Any(finding => finding.RuleId == RepositoryGuardRuleIds.GeneratedOrReleaseOutput));
        Assert.IsTrue(findings.Any(finding => finding.RuleId == RepositoryGuardRuleIds.GameResource));
        Assert.IsTrue(findings.Any(finding => finding.RuleId == RepositoryGuardRuleIds.UnreviewedMediaOrModel));
        Assert.IsTrue(findings.Any(finding => finding.RuleId == RepositoryGuardRuleIds.SecretFile));
        Assert.IsTrue(findings.Any(finding => finding.RuleId == RepositoryGuardRuleIds.PrivateCertificate));
    }

    [TestMethod]
    public void PublicFixturePath_IsNotUnconditionallyBlocked()
    {
        RepositoryFile fixture = RepositoryFile.FromText(
            "tests/fixtures/public/synthetic-state.txt",
            "mode=Unknown; confidence=0");

        Assert.IsEmpty(_scanner.Scan([fixture]));
    }

    [TestMethod]
    public void InvalidExampleEnvironmentConfiguration_DoesNotCreateFalsePositive()
    {
        string example = "API_" + "KEY=replace-with-your-own-key";
        RepositoryFile file = RepositoryFile.FromText(".env.example", example);

        Assert.IsEmpty(_scanner.Scan([file]));
    }

    [TestMethod]
    public void Allowlist_ExemptsOnlyExactRuleAndExactPath()
    {
        string token = "gh" + "p_" + new string('R', 36);
        RepositoryAllowlist allowlist = RepositoryAllowlist.Create(
        [
            new AllowlistEntry(
                RepositoryGuardRuleIds.AccessToken,
                "docs/reviewed.txt",
                "Synthetic false positive reviewed for this exact test path."),
        ]);

        IReadOnlyList<ScanFinding> findings = _scanner.Scan(
        [
            RepositoryFile.FromText("docs/reviewed.txt", token),
            RepositoryFile.FromText("docs/not-reviewed.txt", token),
            RepositoryFile.FromText("docs/reviewed.txt", "password=" + new string('S', 24)),
        ],
        allowlist);

        Assert.IsFalse(findings.Any(finding =>
            finding.Path == "docs/reviewed.txt"
            && finding.RuleId == RepositoryGuardRuleIds.AccessToken));
        Assert.IsTrue(findings.Any(finding =>
            finding.Path == "docs/not-reviewed.txt"
            && finding.RuleId == RepositoryGuardRuleIds.AccessToken));
        Assert.IsTrue(findings.Any(finding =>
            finding.Path == "docs/reviewed.txt"
            && finding.RuleId == RepositoryGuardRuleIds.AssignedSecret));
    }

    [TestMethod]
    public void Allowlist_RejectsWildcardOrDirectoryExemptions()
    {
        string wildcardJson = $$"""
        {
          "version": 1,
          "entries": [
            {
              "ruleId": "{{RepositoryGuardRuleIds.AccessToken}}",
              "path": "tests/fixtures/public/*",
              "reason": "This invalid entry attempts to exempt a complete directory."
            }
          ]
        }
        """;

        Assert.ThrowsExactly<InvalidDataException>(
            () => RepositoryAllowlist.Parse(wildcardJson));
    }

    [TestMethod]
    public void BinaryAndLargeFileRules_HaveExplicitBoundaries()
    {
        byte[] binary = [0x41, 0x00, 0x42];
        RepositoryFile binaryFile = new("assets/data.dat", binary, binary.LongLength);
        RepositoryFile boundaryFile = RepositoryFile.FromText(
            "docs/boundary.txt",
            new string('a', (int)RepositoryScanner.MaximumTrackedFileBytes));
        RepositoryFile oversized = new(
            "docs/oversized.txt",
            ReadOnlyMemory<byte>.Empty,
            RepositoryScanner.MaximumTrackedFileBytes + 1);

        IReadOnlyList<ScanFinding> findings = _scanner.Scan(
            [binaryFile, boundaryFile, oversized]);

        Assert.IsTrue(findings.Any(finding =>
            finding.Path == binaryFile.Path
            && finding.RuleId == RepositoryGuardRuleIds.BinaryFile));
        Assert.IsFalse(findings.Any(finding => finding.Path == boundaryFile.Path));
        Assert.IsTrue(findings.Any(finding =>
            finding.Path == oversized.Path
            && finding.RuleId == RepositoryGuardRuleIds.LargeFile));
    }

    [TestMethod]
    public void Scan_DoesNotModifyInputBytes()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("ordinary immutable scan input");
        byte[] expected = [.. bytes];
        RepositoryFile file = new("src/input.txt", bytes, bytes.LongLength);

        _ = _scanner.Scan([file]);

        CollectionAssert.AreEqual(expected, bytes);
    }

    [TestMethod]
    public void FailedScan_ReturnsNonZeroExitCode()
    {
        string token = "sk" + "-" + new string('T', 28);
        IReadOnlyList<ScanFinding> findings = _scanner.Scan(
            [RepositoryFile.FromText("src/synthetic.txt", token)]);

        int exitCode = RepositoryGuardReporter.WriteReport(findings, new StringWriter());

        Assert.AreNotEqual(0, exitCode);
    }
}
