using System.Collections.ObjectModel;

namespace GtaAutoGameplay.Core.StateEstimation;

public sealed class StateCandidateSupport
{
    private readonly ReadOnlyCollection<Guid> _evidenceIds;

    public StateCandidateSupport(
        string candidateValue,
        double support,
        int distinctEvidenceCount,
        int distinctObservationCount,
        IEnumerable<Guid> evidenceIds)
    {
        if (string.IsNullOrWhiteSpace(candidateValue))
        {
            throw new ArgumentException("Candidate value cannot be empty.", nameof(candidateValue));
        }

        if (!double.IsFinite(support) || support < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(support));
        }

        if (distinctEvidenceCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distinctEvidenceCount));
        }

        if (distinctObservationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distinctObservationCount));
        }

        CandidateValue = candidateValue;
        Support = support;
        DistinctEvidenceCount = distinctEvidenceCount;
        DistinctObservationCount = distinctObservationCount;
        _evidenceIds = Array.AsReadOnly((evidenceIds ?? throw new ArgumentNullException(nameof(evidenceIds))).ToArray());
    }

    public string CandidateValue { get; }

    public double Support { get; }

    public int DistinctEvidenceCount { get; }

    public int DistinctObservationCount { get; }

    public IReadOnlyList<Guid> EvidenceIds => _evidenceIds;
}
