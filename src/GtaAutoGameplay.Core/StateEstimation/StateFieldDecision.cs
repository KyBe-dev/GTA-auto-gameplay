using System.Collections.ObjectModel;
using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.StateEstimation;

public sealed class StateFieldDecision
{
    private readonly ReadOnlyCollection<StateCandidateSupport> _candidateSupports;

    public StateFieldDecision(
        StateField field,
        StateFieldDecisionStatus status,
        StateFieldDecisionReason reason,
        string selectedValue,
        double selectedSupport,
        double confidence,
        IEnumerable<StateCandidateSupport> candidateSupports)
    {
        if (!Enum.IsDefined(field))
        {
            throw new ArgumentOutOfRangeException(nameof(field));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            throw new ArgumentException("Selected value cannot be empty.", nameof(selectedValue));
        }

        if (!double.IsFinite(selectedSupport) || selectedSupport < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedSupport));
        }

        Field = field;
        Status = status;
        Reason = reason;
        SelectedValue = selectedValue;
        SelectedSupport = selectedSupport;
        Confidence = Domain.Confidence.EnsureValid(confidence, nameof(confidence));
        _candidateSupports = Array.AsReadOnly(
            (candidateSupports ?? throw new ArgumentNullException(nameof(candidateSupports))).ToArray());
    }

    public StateField Field { get; }

    public StateFieldDecisionStatus Status { get; }

    public StateFieldDecisionReason Reason { get; }

    public string SelectedValue { get; }

    public double SelectedSupport { get; }

    public double Confidence { get; }

    public IReadOnlyList<StateCandidateSupport> CandidateSupports => _candidateSupports;
}
