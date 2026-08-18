using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GtaAutoGameplay.RepositoryGuard;

public sealed class RepositoryAllowlist
{
    private static readonly Regex RuleIdPattern = new(
        "^[A-Z]+[0-9]{3}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private readonly HashSet<(string RuleId, string Path)> _entries;

    private RepositoryAllowlist(IEnumerable<AllowlistEntry> entries)
    {
        _entries = [];

        foreach (AllowlistEntry entry in entries)
        {
            ValidateEntry(entry);
            string path = RepositoryFile.NormalizePath(entry.Path);

            if (!_entries.Add((entry.RuleId, path)))
            {
                throw new InvalidDataException(
                    $"Allowlist entry '{entry.RuleId}' for '{path}' is duplicated.");
            }
        }
    }

    public static RepositoryAllowlist Empty { get; } = new([]);

    public static RepositoryAllowlist Create(IEnumerable<AllowlistEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new RepositoryAllowlist(entries);
    }

    public static RepositoryAllowlist Load(string path) =>
        Parse(File.ReadAllText(path));

    public static RepositoryAllowlist Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        AllowlistDocument document = JsonSerializer.Deserialize<AllowlistDocument>(json, options)
            ?? throw new InvalidDataException("Allowlist document cannot be null.");

        if (document.Version != 1)
        {
            throw new InvalidDataException("Allowlist version must be exactly 1.");
        }

        return new RepositoryAllowlist(document.Entries ?? []);
    }

    public bool Contains(string ruleId, string path) =>
        _entries.Contains((ruleId, RepositoryFile.NormalizePath(path)));

    private static void ValidateEntry(AllowlistEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!RuleIdPattern.IsMatch(entry.RuleId)
            || !RepositoryGuardRuleIds.All.Contains(entry.RuleId))
        {
            throw new InvalidDataException($"Allowlist rule ID '{entry.RuleId}' is unknown.");
        }

        if (string.IsNullOrWhiteSpace(entry.Path)
            || entry.Path.IndexOfAny(['*', '?', '[', ']', '{', '}']) >= 0
            || entry.Path.Contains('\\', StringComparison.Ordinal)
            || entry.Path.EndsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Allowlist paths must be exact repository file paths using forward slashes; wildcards and directory paths are forbidden.");
        }

        if (string.IsNullOrWhiteSpace(entry.Reason)
            || entry.Reason.Length < 10
            || entry.Reason.Length > 300)
        {
            throw new InvalidDataException(
                "Allowlist entries require a specific explanation from 10 through 300 characters.");
        }
    }

    private sealed class AllowlistDocument
    {
        [JsonPropertyName("version")]
        public int Version { get; init; }

        [JsonPropertyName("entries")]
        public AllowlistEntry[]? Entries { get; init; }
    }
}
