using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.StateEstimation;

public sealed class EvidenceAuditEntry
{
    public EvidenceAuditEntry(
        Guid evidenceId,
        EvidenceSourceType sourceType,
        string targetField,
        string candidateValue,
        EvidenceAuditStatus status,
        EvidenceRejectionReason reason)
    {
        if (!Enum.IsDefined(sourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceType));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        EvidenceId = evidenceId;
        SourceType = sourceType;
        TargetField = targetField ?? throw new ArgumentNullException(nameof(targetField));
        CandidateValue = candidateValue ?? throw new ArgumentNullException(nameof(candidateValue));
        Status = status;
        Reason = reason;
    }

    public Guid EvidenceId { get; }

    public EvidenceSourceType SourceType { get; }

    public string TargetField { get; }

    public string CandidateValue { get; }

    public EvidenceAuditStatus Status { get; }

    public EvidenceRejectionReason Reason { get; }
}
