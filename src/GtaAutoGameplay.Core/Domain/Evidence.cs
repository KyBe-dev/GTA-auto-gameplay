namespace GtaAutoGameplay.Core.Domain;

public sealed class Evidence
{
    public Evidence(
        Guid id,
        EvidenceSourceType sourceType,
        DateTimeOffset observedAt,
        DateTimeOffset validUntil,
        string targetField,
        string candidateValue,
        double fieldConfidence,
        string adapterId,
        string adapterVersion,
        EvidenceStatus status = EvidenceStatus.Fresh)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Evidence ID cannot be empty.", nameof(id));
        }

        if (!Enum.IsDefined(sourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceType));
        }

        if (validUntil < observedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(validUntil),
                "Evidence cannot expire before it was observed.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Id = id;
        SourceType = sourceType;
        ObservedAt = observedAt;
        ValidUntil = validUntil;
        TargetField = RequireText(targetField, nameof(targetField));
        CandidateValue = RequireText(candidateValue, nameof(candidateValue));
        FieldConfidence = Confidence.EnsureValid(fieldConfidence, nameof(fieldConfidence));
        AdapterId = RequireText(adapterId, nameof(adapterId));
        AdapterVersion = RequireText(adapterVersion, nameof(adapterVersion));
        Status = status;
    }

    public Guid Id { get; }

    public EvidenceSourceType SourceType { get; }

    public DateTimeOffset ObservedAt { get; }

    public DateTimeOffset ValidUntil { get; }

    public string TargetField { get; }

    public string CandidateValue { get; }

    public double FieldConfidence { get; }

    public string AdapterId { get; }

    public string AdapterVersion { get; }

    public EvidenceStatus Status { get; }

    public EvidenceStatus GetStatusAt(DateTimeOffset timestamp)
    {
        if (Status == EvidenceStatus.Conflicting)
        {
            return EvidenceStatus.Conflicting;
        }

        return Status == EvidenceStatus.Expired || timestamp >= ValidUntil
            ? EvidenceStatus.Expired
            : EvidenceStatus.Fresh;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
        }

        return value;
    }
}
