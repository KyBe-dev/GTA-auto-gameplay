using System.Text.Json.Serialization;

namespace GtaAutoGameplay.RepositoryGuard;

public sealed record AllowlistEntry(
    [property: JsonPropertyName("ruleId")] string RuleId,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("reason")] string Reason);
