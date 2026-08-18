namespace GtaAutoGameplay.Core.Logging;

public static class StructuredLogFieldWhitelist
{
    private static readonly IReadOnlyDictionary<StructuredLogCategory, HashSet<string>> AllowedFields =
        new Dictionary<StructuredLogCategory, HashSet<string>>
        {
            [StructuredLogCategory.Application] = CreateSet(
                StructuredLogFieldNames.Operation,
                StructuredLogFieldNames.Outcome,
                StructuredLogFieldNames.Component,
                StructuredLogFieldNames.Count,
                StructuredLogFieldNames.DurationMilliseconds),
            [StructuredLogCategory.Safety] = CreateSet(
                StructuredLogFieldNames.Operation,
                StructuredLogFieldNames.Outcome,
                StructuredLogFieldNames.IsArmed,
                StructuredLogFieldNames.StopReasonCode,
                StructuredLogFieldNames.HeldInputCount,
                StructuredLogFieldNames.TargetMatch,
                StructuredLogFieldNames.Foreground,
                StructuredLogFieldNames.CaptureHealthy,
                StructuredLogFieldNames.StateFresh),
            [StructuredLogCategory.Input] = CreateSet(
                StructuredLogFieldNames.Operation,
                StructuredLogFieldNames.Outcome,
                StructuredLogFieldNames.SemanticAction,
                StructuredLogFieldNames.InputTokenId,
                StructuredLogFieldNames.Count,
                StructuredLogFieldNames.DurationMilliseconds),
            [StructuredLogCategory.State] = CreateSet(
                StructuredLogFieldNames.Operation,
                StructuredLogFieldNames.Outcome,
                StructuredLogFieldNames.GameMode,
                StructuredLogFieldNames.Confidence,
                StructuredLogFieldNames.EvidenceCount,
                StructuredLogFieldNames.AdapterId,
                StructuredLogFieldNames.AdapterVersion,
                StructuredLogFieldNames.StateFresh),
            [StructuredLogCategory.Configuration] = CreateSet(
                StructuredLogFieldNames.Operation,
                StructuredLogFieldNames.Outcome,
                StructuredLogFieldNames.CloudProviderMode,
                StructuredLogFieldNames.CredentialState,
                StructuredLogFieldNames.ScreenshotStorageEnabled,
                StructuredLogFieldNames.RecordingStorageEnabled,
                StructuredLogFieldNames.FrameReplayEnabled,
                StructuredLogFieldNames.LogCapacity,
                StructuredLogFieldNames.LogRetentionSeconds),
        };

    public static bool IsAllowed(StructuredLogCategory category, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        return Enum.IsDefined(category)
            && AllowedFields.TryGetValue(category, out HashSet<string>? fields)
            && fields.Contains(fieldName);
    }

    private static HashSet<string> CreateSet(params string[] fields) =>
        new(fields, StringComparer.Ordinal);
}
