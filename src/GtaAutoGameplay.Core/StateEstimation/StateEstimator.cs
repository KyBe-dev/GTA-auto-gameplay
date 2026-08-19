using GtaAutoGameplay.Core.Domain;

namespace GtaAutoGameplay.Core.StateEstimation;

/// <summary>
/// Performs deterministic, in-memory fusion of enum-valued evidence. Confidence is summed per
/// candidate, but a candidate must also have distinct evidence IDs and observation times. Close
/// qualified candidates produce a safe-default conflict result. A previous value is retained only
/// while it still has qualified current support and a replacement lacks the configured advantage.
/// These conservative rules are not a claim of accuracy against real GTA imagery.
/// </summary>
public sealed class StateEstimator : IStateEstimator
{
    private static readonly StateField[] SupportedFields = Enum.GetValues<StateField>();
    private readonly StateEstimatorOptions _options;

    public StateEstimator(StateEstimatorOptions? options = null)
    {
        _options = options ?? new StateEstimatorOptions();
    }

    public StateEstimationResult Estimate(
        IReadOnlyCollection<Evidence> evidence,
        DateTimeOffset evaluatedAt,
        string adapterId,
        string adapterVersion,
        GameState? previousState = null)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        adapterId = RequireText(adapterId, nameof(adapterId));
        adapterVersion = RequireText(adapterVersion, nameof(adapterVersion));

        Evidence[] inputSnapshot = evidence.ToArray();
        HashSet<Guid> duplicateIds = inputSnapshot
            .GroupBy(item => item.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        List<PreparedEvidence> prepared = [];
        EvidenceAuditEntry?[] auditEntries = new EvidenceAuditEntry[inputSnapshot.Length];

        for (int index = 0; index < inputSnapshot.Length; index++)
        {
            Evidence item = inputSnapshot[index];
            if (duplicateIds.Contains(item.Id))
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Ignored,
                    EvidenceRejectionReason.DuplicateId);
                continue;
            }

            if (item.SourceType == EvidenceSourceType.Unknown)
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Invalid,
                    EvidenceRejectionReason.UnknownSource);
                continue;
            }

            if (!string.Equals(item.AdapterId, adapterId, StringComparison.Ordinal))
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Ignored,
                    EvidenceRejectionReason.AdapterIdMismatch);
                continue;
            }

            if (!string.Equals(item.AdapterVersion, adapterVersion, StringComparison.Ordinal))
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Ignored,
                    EvidenceRejectionReason.AdapterVersionMismatch);
                continue;
            }

            if (item.ObservedAt > evaluatedAt)
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Invalid,
                    EvidenceRejectionReason.FutureObservation);
                continue;
            }

            EvidenceStatus statusAtEvaluation = item.GetStatusAt(evaluatedAt);
            if (statusAtEvaluation == EvidenceStatus.Conflicting)
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Conflicting,
                    EvidenceRejectionReason.EvidenceMarkedConflicting);
                continue;
            }

            if (statusAtEvaluation == EvidenceStatus.Expired)
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Expired,
                    EvidenceRejectionReason.ExplicitlyExpired);
                continue;
            }

            if (evaluatedAt - item.ObservedAt > _options.ObservationWindow)
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Ignored,
                    EvidenceRejectionReason.OutsideObservationWindow);
                continue;
            }

            if (!StateFieldNames.TryGetField(item.TargetField, out StateField field))
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Ignored,
                    EvidenceRejectionReason.UnsupportedTargetField);
                continue;
            }

            if (!TryGetCanonicalCandidate(field, item.CandidateValue, out string candidateValue))
            {
                auditEntries[index] = Audit(
                    item,
                    EvidenceAuditStatus.Invalid,
                    EvidenceRejectionReason.InvalidCandidateValue);
                continue;
            }

            prepared.Add(new PreparedEvidence(index, item, field, candidateValue));
        }

        foreach (IGrouping<(StateField Field, string Candidate), PreparedEvidence> group in prepared
                     .GroupBy(item => (item.Field, item.CandidateValue)))
        {
            bool hasIndependentEvidence = group.Any(
                item => item.Evidence.SourceType != EvidenceSourceType.CloudCandidate);

            foreach (PreparedEvidence item in group)
            {
                if (item.Evidence.SourceType == EvidenceSourceType.CloudCandidate
                    && !hasIndependentEvidence)
                {
                    auditEntries[item.InputIndex] = Audit(
                        item.Evidence,
                        EvidenceAuditStatus.Ignored,
                        EvidenceRejectionReason.CloudCandidateWithoutIndependentConfirmation);
                }
                else
                {
                    auditEntries[item.InputIndex] = Audit(
                        item.Evidence,
                        EvidenceAuditStatus.Participated,
                        EvidenceRejectionReason.None);
                }
            }
        }

        PreparedEvidence[] participating = prepared
            .Where(item => auditEntries[item.InputIndex]!.Status == EvidenceAuditStatus.Participated)
            .OrderBy(item => item.Evidence.ObservedAt)
            .ThenBy(item => item.Evidence.Id)
            .ToArray();

        Dictionary<StateField, StateFieldDecision> decisions = SupportedFields.ToDictionary(
            field => field,
            field => DecideField(field, participating, evaluatedAt, previousState));

        ApplyCrossFieldConsistency(decisions);

        GameMode gameMode = ParseSelected<GameMode>(decisions[StateField.GameMode]);
        ControlMode controlMode = ParseSelected<ControlMode>(decisions[StateField.ControlMode]);
        MenuSubstate menuSubstate = ParseSelected<MenuSubstate>(decisions[StateField.MenuSubstate]);
        ObjectiveType objectiveType = ParseSelected<ObjectiveType>(decisions[StateField.ObjectiveType]);
        double summaryConfidence = Math.Clamp(
            decisions.Values.Average(decision => decision.Confidence),
            0d,
            1d);

        GameState state = new(
            evaluatedAt,
            gameMode: gameMode,
            controlMode: controlMode,
            menuSubstate: menuSubstate,
            objectiveType: objectiveType,
            confidence: summaryConfidence,
            evidence: participating.Select(item => item.Evidence));

        return new StateEstimationResult(
            state,
            decisions,
            auditEntries.Select(entry => entry!));
    }

    private StateFieldDecision DecideField(
        StateField field,
        IReadOnlyCollection<PreparedEvidence> participating,
        DateTimeOffset evaluatedAt,
        GameState? previousState)
    {
        StateCandidateSupport[] supports = participating
            .Where(item => item.Field == field)
            .GroupBy(item => item.CandidateValue, StringComparer.Ordinal)
            .Select(group => new StateCandidateSupport(
                group.Key,
                group.Sum(item => item.Evidence.FieldConfidence),
                group.Select(item => item.Evidence.Id).Distinct().Count(),
                group.Select(item => item.Evidence.ObservedAt).Distinct().Count(),
                group.Select(item => item.Evidence.Id).Order()))
            .OrderByDescending(candidate => candidate.Support)
            .ThenBy(candidate => candidate.CandidateValue, StringComparer.Ordinal)
            .ToArray();

        StateCandidateSupport[] qualified = supports
            .Where(IsQualified)
            .ToArray();

        if (qualified.Length == 0)
        {
            return CreateInsufficientDecision(field, supports);
        }

        StateCandidateSupport best = qualified[0];
        if (qualified.Length > 1
            && best.Support >= _options.ConflictMinimumSupport
            && qualified[1].Support >= _options.ConflictMinimumSupport
            && best.Support - qualified[1].Support <= _options.ConflictSupportDifference)
        {
            return new StateFieldDecision(
                field,
                StateFieldDecisionStatus.Conflicting,
                StateFieldDecisionReason.CompetingCandidates,
                GetSafeDefault(field),
                0d,
                0d,
                supports);
        }

        string previousValue = GetPreviousValue(field, previousState);
        if (IsPreviousStateTrustworthy(previousState, evaluatedAt)
            && previousValue != GetSafeDefault(field)
            && !string.Equals(previousValue, best.CandidateValue, StringComparison.Ordinal))
        {
            StateCandidateSupport? previousSupport = supports.FirstOrDefault(
                candidate => string.Equals(
                    candidate.CandidateValue,
                    previousValue,
                    StringComparison.Ordinal));
            double previousSupportValue = previousSupport?.Support ?? 0d;

            if (best.Support - previousSupportValue < _options.SwitchingAdvantage)
            {
                if (previousSupport is not null && IsQualified(previousSupport))
                {
                    return new StateFieldDecision(
                        field,
                        StateFieldDecisionStatus.HysteresisHeld,
                        StateFieldDecisionReason.PreviousValueRetainedByHysteresis,
                        previousValue,
                        previousSupport.Support,
                        GetCandidateConfidence(previousSupport),
                        supports);
                }

                return new StateFieldDecision(
                    field,
                    StateFieldDecisionStatus.InsufficientEvidence,
                    StateFieldDecisionReason.SwitchingAdvantageNotMetWithoutPreviousSupport,
                    GetSafeDefault(field),
                    0d,
                    0d,
                    supports);
            }
        }

        return new StateFieldDecision(
            field,
            StateFieldDecisionStatus.Confirmed,
            StateFieldDecisionReason.ConfirmedByMultiFrameSupport,
            best.CandidateValue,
            best.Support,
            GetCandidateConfidence(best),
            supports);
    }

    private StateFieldDecision CreateInsufficientDecision(
        StateField field,
        IReadOnlyList<StateCandidateSupport> supports)
    {
        StateFieldDecisionReason reason;
        if (supports.Count == 0)
        {
            reason = StateFieldDecisionReason.NoValidEvidence;
        }
        else if (supports[0].DistinctEvidenceCount < _options.MinimumDistinctEvidenceCount)
        {
            reason = StateFieldDecisionReason.InsufficientDistinctEvidence;
        }
        else if (supports[0].DistinctObservationCount < _options.MinimumDistinctObservationCount)
        {
            reason = StateFieldDecisionReason.InsufficientObservationTimes;
        }
        else
        {
            reason = StateFieldDecisionReason.InsufficientSupport;
        }

        return new StateFieldDecision(
            field,
            StateFieldDecisionStatus.InsufficientEvidence,
            reason,
            GetSafeDefault(field),
            0d,
            0d,
            supports);
    }

    private void ApplyCrossFieldConsistency(IDictionary<StateField, StateFieldDecision> decisions)
    {
        if (!string.Equals(
                decisions[StateField.GameMode].SelectedValue,
                nameof(GameMode.Menu),
                StringComparison.Ordinal)
            && !string.Equals(
                decisions[StateField.MenuSubstate].SelectedValue,
                nameof(MenuSubstate.None),
                StringComparison.Ordinal))
        {
            StateFieldDecision current = decisions[StateField.MenuSubstate];
            decisions[StateField.MenuSubstate] = new StateFieldDecision(
                StateField.MenuSubstate,
                StateFieldDecisionStatus.ConsistencyOverridden,
                StateFieldDecisionReason.MenuSubstateClearedForNonMenuMode,
                nameof(MenuSubstate.None),
                0d,
                0d,
                current.CandidateSupports);
        }
    }

    private bool IsQualified(StateCandidateSupport support) =>
        support.DistinctEvidenceCount >= _options.MinimumDistinctEvidenceCount
        && support.DistinctObservationCount >= _options.MinimumDistinctObservationCount
        && support.Support >= _options.MinimumCandidateSupport;

    private bool IsPreviousStateTrustworthy(GameState? previousState, DateTimeOffset evaluatedAt)
    {
        if (previousState is null
            || previousState.ObservedAt > evaluatedAt
            || evaluatedAt - previousState.ObservedAt > _options.PreviousStateMaximumAge)
        {
            return false;
        }

        return previousState.Confidence >= _options.MinimumPreviousStateConfidence;
    }

    private static double GetCandidateConfidence(StateCandidateSupport support) =>
        Math.Clamp(support.Support / support.DistinctEvidenceCount, 0d, 1d);

    private static string GetSafeDefault(StateField field) => field switch
    {
        StateField.GameMode => nameof(GameMode.Unknown),
        StateField.ControlMode => nameof(ControlMode.Unknown),
        StateField.MenuSubstate => nameof(MenuSubstate.None),
        StateField.ObjectiveType => nameof(ObjectiveType.Unknown),
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    private static string GetPreviousValue(StateField field, GameState? previousState)
    {
        if (previousState is null)
        {
            return GetSafeDefault(field);
        }

        return field switch
        {
            StateField.GameMode => previousState.GameMode.ToString(),
            StateField.ControlMode => previousState.ControlMode.ToString(),
            StateField.MenuSubstate => previousState.MenuSubstate.ToString(),
            StateField.ObjectiveType => previousState.ObjectiveType.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
    }

    private static bool TryGetCanonicalCandidate(
        StateField field,
        string candidateValue,
        out string canonicalValue)
    {
        return field switch
        {
            StateField.GameMode => TryParseExact<GameMode>(candidateValue, out canonicalValue),
            StateField.ControlMode => TryParseExact<ControlMode>(candidateValue, out canonicalValue),
            StateField.MenuSubstate => TryParseExact<MenuSubstate>(candidateValue, out canonicalValue),
            StateField.ObjectiveType => TryParseExact<ObjectiveType>(candidateValue, out canonicalValue),
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
    }

    private static bool TryParseExact<TEnum>(string value, out string canonicalValue)
        where TEnum : struct, Enum
    {
        if (!Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal))
        {
            canonicalValue = string.Empty;
            return false;
        }

        canonicalValue = Enum.Parse<TEnum>(value).ToString();
        return true;
    }

    private static TEnum ParseSelected<TEnum>(StateFieldDecision decision)
        where TEnum : struct, Enum => Enum.Parse<TEnum>(decision.SelectedValue);

    private static EvidenceAuditEntry Audit(
        Evidence evidence,
        EvidenceAuditStatus status,
        EvidenceRejectionReason reason) =>
        new(
            evidence.Id,
            evidence.SourceType,
            evidence.TargetField,
            evidence.CandidateValue,
            status,
            reason);

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
        }

        return value;
    }

    private sealed record PreparedEvidence(
        int InputIndex,
        Evidence Evidence,
        StateField Field,
        string CandidateValue);
}
