using System.Text;
using System.Text.RegularExpressions;

namespace GtaAutoGameplay.RepositoryGuard;

public sealed class RepositoryScanner
{
    public const long MaximumTrackedFileBytes = 1_048_576;

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private static readonly Regex AssignedSecretPattern = new(
        "(?im)\\b(?:api[_-]?key|access[_-]?token|auth[_-]?token|client[_-]?secret|password|connection[_-]?string)\\b\\s*[:=]\\s*[\\\"']?(?<value>[A-Za-z0-9_./+=:@-]{16,})",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private static readonly Regex AccessTokenPattern = new(
        "(?:" +
        "gh" + "[pousr]_[A-Za-z0-9]{30,}" +
        "|AK" + "IA[0-9A-Z]{16}" +
        "|sk" + "-[A-Za-z0-9_-]{20,}" +
        ")",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private static readonly Regex PrivateKeyPattern = new(
        "-----BEGIN " + "(?:RSA |EC |OPENSSH |PGP )?" + "PRIVATE KEY-----",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private static readonly Regex JsonWebTokenPattern = new(
        "\\bey" + "J[A-Za-z0-9_-]{8,}\\.[A-Za-z0-9_-]{8,}\\.[A-Za-z0-9_-]{8,}\\b",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private static readonly HashSet<string> PrivateCertificateExtensions = new(
        [".key", ".pem", ".pfx", ".p12", ".snk"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> MediaAndModelExtensions = new(
        [
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp",
            ".mp4", ".avi", ".mov", ".mkv", ".webm",
            ".onnx", ".pt", ".pth", ".ckpt", ".safetensors", ".tflite",
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> GeneratedAndReleaseExtensions = new(
        [
            ".exe", ".dll", ".pdb", ".log", ".dmp", ".dump", ".mdmp",
            ".zip", ".7z", ".rar", ".msi", ".msix", ".appx", ".nupkg", ".snupkg",
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> GeneratedDirectorySegments = new(
        [
            ".vs", "bin", "obj", "testresults", "artifacts", "publish",
            "release-output", "logs", "crash-dumps", "crashdumps",
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> RestrictedGameDirectorySegments = new(
        [
            "gta-saves", "gta-savegames", "gta-account-data", "gta-user-config",
            "gta-profiles", "rockstar games", "profiles",
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> RestrictedMediaDirectorySegments = new(
        [
            "screenshots", "captures", "capture-frames", "recordings", "replays",
            "video-captures", "model-cache",
        ],
        StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ScanFinding> Scan(
        IEnumerable<RepositoryFile> files,
        RepositoryAllowlist? allowlist = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        allowlist ??= RepositoryAllowlist.Empty;

        List<ScanFinding> findings = [];

        foreach (RepositoryFile file in files)
        {
            ArgumentNullException.ThrowIfNull(file);
            ScanPath(file, findings);
            ScanSizeAndContent(file, findings);
        }

        return findings
            .Where(finding => !allowlist.Contains(finding.RuleId, finding.Path))
            .Distinct()
            .OrderBy(finding => finding.Path, StringComparer.Ordinal)
            .ThenBy(finding => finding.RuleId, StringComparer.Ordinal)
            .ThenBy(finding => finding.HistoryObjectId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ScanPath(RepositoryFile file, ICollection<ScanFinding> findings)
    {
        string[] segments = file.Path.Split('/');
        string fileName = segments[^1];
        string extension = System.IO.Path.GetExtension(fileName);

        bool isDocumentedEnvironmentExample =
            fileName.Equals(".env.example", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".example", StringComparison.OrdinalIgnoreCase)
                && fileName.StartsWith(".env.", StringComparison.OrdinalIgnoreCase);

        if (!isDocumentedEnvironmentExample
            && (fileName.Equals(".env", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith(".env.", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("secrets.json", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".secrets.json", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("credentials.json", StringComparison.OrdinalIgnoreCase)
                || segments.Any(segment => segment.Equals(".credentials", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("local-credentials", StringComparison.OrdinalIgnoreCase))
                || segments[0].Equals("credentials", StringComparison.OrdinalIgnoreCase)))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.SecretFile,
                "Local environment, credential, or secret configuration files must not be tracked.");
        }

        if (PrivateCertificateExtensions.Contains(extension))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.PrivateCertificate,
                "Certificate private keys and signing-key containers must remain outside the repository.");
        }

        if (IsRestrictedGameResource(fileName, extension, segments))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.GameResource,
                "Game binaries, archives, saves, profiles, and account data are prohibited repository content.");
        }

        if (MediaAndModelExtensions.Contains(extension)
            || segments.Any(RestrictedMediaDirectorySegments.Contains))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.UnreviewedMediaOrModel,
                "Screenshots, recordings, captured frames, and model weights require exact review before tracking.");
        }

        bool installerOutput = segments.Length >= 2
            && segments[0].Equals("installer", StringComparison.OrdinalIgnoreCase)
            && (segments[1].Equals("output", StringComparison.OrdinalIgnoreCase)
                || segments[1].Equals("dist", StringComparison.OrdinalIgnoreCase)
                || segments[1].Equals("packages", StringComparison.OrdinalIgnoreCase));

        if (GeneratedAndReleaseExtensions.Contains(extension)
            || segments.Any(GeneratedDirectorySegments.Contains)
            || installerOutput)
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.GeneratedOrReleaseOutput,
                "Build, diagnostic, crash, package, and release outputs must not be tracked as source.");
        }
    }

    private static void ScanSizeAndContent(
        RepositoryFile file,
        ICollection<ScanFinding> findings)
    {
        if (file.Length > MaximumTrackedFileBytes)
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.LargeFile,
                $"Tracked files larger than {MaximumTrackedFileBytes} bytes require exact review.");
            return;
        }

        ReadOnlySpan<byte> bytes = file.Content.Span;
        if (bytes.Contains((byte)0))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.BinaryFile,
                "Binary tracked files require exact review because text scanning cannot validate their contents.");
            return;
        }

        string content;
        try
        {
            content = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.BinaryFile,
                "Non-UTF-8 tracked files require exact review because text scanning cannot validate their contents.");
            return;
        }

        Match assignment = AssignedSecretPattern.Match(content);
        while (assignment.Success)
        {
            if (!IsObviousPlaceholder(assignment.Groups["value"].Value))
            {
                AddFinding(
                    findings,
                    file,
                    RepositoryGuardRuleIds.AssignedSecret,
                    "A credential-like assignment was detected; the matched value is intentionally omitted.");
                break;
            }

            assignment = assignment.NextMatch();
        }

        if (AccessTokenPattern.IsMatch(content))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.AccessToken,
                "An API key or access-token pattern was detected; the matched value is intentionally omitted.");
        }

        if (PrivateKeyPattern.IsMatch(content))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.PrivateKey,
                "A private-key boundary marker was detected; key material is intentionally omitted.");
        }

        if (JsonWebTokenPattern.IsMatch(content))
        {
            AddFinding(
                findings,
                file,
                RepositoryGuardRuleIds.JsonWebToken,
                "A JSON Web Token pattern was detected; the matched value is intentionally omitted.");
        }
    }

    private static bool IsRestrictedGameResource(
        string fileName,
        string extension,
        IEnumerable<string> segments)
    {
        if (extension.Equals(".rpf", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("GTA5.exe", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("PlayGTAV.exe", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("GTAVLauncher.exe", StringComparison.OrdinalIgnoreCase)
            || segments.Any(RestrictedGameDirectorySegments.Contains))
        {
            return true;
        }

        return fileName.StartsWith("SGTA5", StringComparison.OrdinalIgnoreCase)
            && fileName[5..].All(char.IsAsciiDigit);
    }

    private static bool IsObviousPlaceholder(string value)
    {
        string normalized = value.Trim('"', '\'', '<', '>').ToLowerInvariant();
        string[] markers =
        [
            "example", "placeholder", "replace", "not-a-real", "not_real",
            "your-own", "your_own", "invalid", "dummy", "redacted",
        ];

        return markers.Any(normalized.Contains)
            || normalized.All(character => character is 'x' or '0' or '-' or '_');
    }

    private static void AddFinding(
        ICollection<ScanFinding> findings,
        RepositoryFile file,
        string ruleId,
        string securityNote) =>
        findings.Add(new ScanFinding(
            ruleId,
            file.Path,
            securityNote,
            file.HistoryObjectId));
}
