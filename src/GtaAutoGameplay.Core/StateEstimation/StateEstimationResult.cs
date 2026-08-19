using System.Collections.ObjectModel;
using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.StateEstimation;

public sealed class StateEstimationResult
{
    private readonly ReadOnlyDictionary<StateField, StateFieldDecision> _fieldDecisions;
    private readonly ReadOnlyCollection<EvidenceAuditEntry> _evidenceAudit;

    public StateEstimationResult(
        GameState state,
        IReadOnlyDictionary<StateField, StateFieldDecision> fieldDecisions,
        IEnumerable<EvidenceAuditEntry> evidenceAudit)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        ArgumentNullException.ThrowIfNull(fieldDecisions);
        ArgumentNullException.ThrowIfNull(evidenceAudit);

        _fieldDecisions = new ReadOnlyDictionary<StateField, StateFieldDecision>(
            new Dictionary<StateField, StateFieldDecision>(fieldDecisions));
        _evidenceAudit = Array.AsReadOnly(evidenceAudit.ToArray());
    }

    public GameState State { get; }

    public IReadOnlyDictionary<StateField, StateFieldDecision> FieldDecisions => _fieldDecisions;

    public IReadOnlyList<EvidenceAuditEntry> EvidenceAudit => _evidenceAudit;
}
