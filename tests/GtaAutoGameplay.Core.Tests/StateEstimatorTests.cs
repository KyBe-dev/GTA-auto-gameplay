using GtaAutoGameplay.Core.Domain;
using GtaAutoGameplay.Core.AI;
using GtaAutoGameplay.Core.Safety;
using GtaAutoGameplay.Core.StateEstimation;
using GtaAutoGameplay.Core.Tests.Fakes;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class StateEstimatorTests
{
    private const string AdapterId = "gta-v-test";
    private const string AdapterVersion = "1.0.0";
    private static readonly DateTimeOffset EvaluatedAt =
        new(2026, 8, 19, 8, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void EmptyEvidence_ReturnsSafeDefaultState()
    {
        StateEstimationResult result = Estimate([]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(ControlMode.Unknown, result.State.ControlMode);
        Assert.AreEqual(MenuSubstate.None, result.State.MenuSubstate);
        Assert.AreEqual(ObjectiveType.Unknown, result.State.ObjectiveType);
        Assert.AreEqual(0d, result.State.Confidence);
        Assert.IsEmpty(result.State.Evidence);
        Assert.IsTrue(result.FieldDecisions.Values.All(
            decision => decision.Status == StateFieldDecisionStatus.InsufficientEvidence));
    }

    [TestMethod]
    public void SingleHighConfidenceEvidence_DoesNotDetermineGlobalState()
    {
        Evidence evidence = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 1,
            confidence: 1d);

        StateEstimationResult result = Estimate([evidence]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            StateFieldDecisionReason.InsufficientDistinctEvidence,
            result.FieldDecisions[StateField.GameMode].Reason);
    }

    [TestMethod]
    public void ConsistentMultiFrameEvidence_DeterminesState()
    {
        StateEstimationResult result = Estimate(CreatePair(
            StateField.GameMode,
            nameof(GameMode.Gameplay)));

        Assert.AreEqual(GameMode.Gameplay, result.State.GameMode);
        Assert.AreEqual(
            StateFieldDecisionStatus.Confirmed,
            result.FieldDecisions[StateField.GameMode].Status);
        Assert.HasCount(2, result.State.Evidence);
    }

    [TestMethod]
    public void AllSupportedFields_AreFusedWithoutCopyingUnsupportedStateFields()
    {
        StateEstimationResult result = Estimate([
            .. CreatePair(StateField.GameMode, nameof(GameMode.Menu)),
            .. CreatePair(StateField.ControlMode, nameof(ControlMode.UI), 4, 3),
            .. CreatePair(StateField.MenuSubstate, nameof(MenuSubstate.Settings), 4, 3),
            .. CreatePair(StateField.ObjectiveType, nameof(ObjectiveType.Interact), 4, 3),
        ]);

        Assert.AreEqual(GameMode.Menu, result.State.GameMode);
        Assert.AreEqual(ControlMode.UI, result.State.ControlMode);
        Assert.AreEqual(MenuSubstate.Settings, result.State.MenuSubstate);
        Assert.AreEqual(ObjectiveType.Interact, result.State.ObjectiveType);
        Assert.IsNull(result.State.MissionId);
        Assert.IsNull(result.State.MissionStageId);
        Assert.IsNull(result.State.VisibleObjectiveText);
        Assert.IsNull(result.State.ObjectiveTarget);
        Assert.IsNull(result.State.InputProfileId);
    }

    [TestMethod]
    public void DuplicateEvidenceId_DoesNotIncreaseSupport()
    {
        Guid duplicateId = Guid.NewGuid();
        Evidence first = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 2,
            id: duplicateId);
        Evidence repeated = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 1,
            id: duplicateId);

        StateEstimationResult result = Estimate([first, repeated]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.IsTrue(result.EvidenceAudit.All(
            entry => entry.Reason == EvidenceRejectionReason.DuplicateId));
        Assert.IsEmpty(result.State.Evidence);
    }

    [TestMethod]
    public void ExpiredEvidence_DoesNotParticipate()
    {
        Evidence expired = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 3,
            validUntil: EvaluatedAt.AddSeconds(-1));

        StateEstimationResult result = Estimate([
            expired,
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(EvidenceAuditStatus.Expired, AuditFor(result, expired).Status);
    }

    [TestMethod]
    public void FutureEvidence_IsInvalidAndDoesNotParticipate()
    {
        Evidence future = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: -1);

        StateEstimationResult result = Estimate([
            future,
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            EvidenceRejectionReason.FutureObservation,
            AuditFor(result, future).Reason);
    }

    [TestMethod]
    public void EvidenceOutsideObservationWindow_DoesNotParticipate()
    {
        Evidence old = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 6,
            validUntil: EvaluatedAt.AddSeconds(1));

        StateEstimationResult result = Estimate([
            old,
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            EvidenceRejectionReason.OutsideObservationWindow,
            AuditFor(result, old).Reason);
    }

    [TestMethod]
    public void ConflictingEvidence_IsAuditedButNotUsedForConfirmation()
    {
        Evidence conflicting = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 2,
            status: EvidenceStatus.Conflicting);

        StateEstimationResult result = Estimate([
            conflicting,
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(EvidenceAuditStatus.Conflicting, AuditFor(result, conflicting).Status);
        Assert.IsFalse(result.State.Evidence.Contains(conflicting));
    }

    [TestMethod]
    public void AdapterIdMismatch_IsRejected()
    {
        Evidence mismatch = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 2,
            adapterId: "other-adapter");

        StateEstimationResult result = Estimate([
            mismatch,
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            EvidenceRejectionReason.AdapterIdMismatch,
            AuditFor(result, mismatch).Reason);
    }

    [TestMethod]
    public void AdapterVersionMismatch_IsRejected()
    {
        Evidence mismatch = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            secondsBeforeEvaluation: 2,
            adapterVersion: "2.0.0");

        StateEstimationResult result = Estimate([
            mismatch,
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            EvidenceRejectionReason.AdapterVersionMismatch,
            AuditFor(result, mismatch).Reason);
    }

    [TestMethod]
    [DataRow(StateField.GameMode, "gameplay")]
    [DataRow(StateField.GameMode, "1")]
    [DataRow(StateField.ControlMode, "Manual")]
    [DataRow(StateField.ObjectiveType, "Navigate")]
    [DataRow(StateField.MenuSubstate, "Settings ")]
    public void InvalidOrLegacyCandidate_IsRejected(StateField field, string candidate)
    {
        Evidence invalid = CreateEvidence(field, candidate, 2);

        StateEstimationResult result = Estimate([
            invalid,
            CreateEvidence(field, candidate, 1),
        ]);

        Assert.AreEqual(
            EvidenceRejectionReason.InvalidCandidateValue,
            AuditFor(result, invalid).Reason);
        Assert.IsTrue(result.EvidenceAudit.All(
            entry => entry.Status == EvidenceAuditStatus.Invalid));
    }

    [TestMethod]
    public void CloseQualifiedCandidates_ReturnSafeDefaultAndConflictDecision()
    {
        StateEstimationResult result = Estimate([
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 4, 0.8d),
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 3, 0.8d),
            CreateEvidence(StateField.GameMode, nameof(GameMode.Paused), 2, 0.75d),
            CreateEvidence(StateField.GameMode, nameof(GameMode.Paused), 1, 0.75d),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            StateFieldDecisionStatus.Conflicting,
            result.FieldDecisions[StateField.GameMode].Status);
        Assert.AreEqual(
            StateFieldDecisionReason.CompetingCandidates,
            result.FieldDecisions[StateField.GameMode].Reason);
    }

    [TestMethod]
    public void CandidateWithClearSupportAdvantage_Wins()
    {
        StateEstimationResult result = Estimate([
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 4, 0.9d),
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 3, 0.9d),
            CreateEvidence(StateField.GameMode, nameof(GameMode.Paused), 2, 0.6d),
            CreateEvidence(StateField.GameMode, nameof(GameMode.Paused), 1, 0.6d),
        ]);

        Assert.AreEqual(GameMode.Gameplay, result.State.GameMode);
        Assert.AreEqual(
            nameof(GameMode.Gameplay),
            result.FieldDecisions[StateField.GameMode].SelectedValue);
    }

    [TestMethod]
    public void Hysteresis_RetainsSupportedPreviousValueWhenSwitchAdvantageIsSmall()
    {
        GameState previous = CreatePreviousState(GameMode.Gameplay);
        StateEstimationResult result = Estimate(
            [
                CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 4, 0.7d),
                CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 3, 0.7d),
                CreateEvidence(StateField.GameMode, nameof(GameMode.Paused), 2, 0.78d),
                CreateEvidence(StateField.GameMode, nameof(GameMode.Paused), 1, 0.78d),
            ],
            previous);

        Assert.AreEqual(GameMode.Gameplay, result.State.GameMode);
        Assert.AreEqual(
            StateFieldDecisionStatus.HysteresisHeld,
            result.FieldDecisions[StateField.GameMode].Status);
    }

    [TestMethod]
    public void PreviousStateWithoutCurrentSupport_FallsBackToSafeDefault()
    {
        GameState previous = CreatePreviousState(GameMode.Gameplay);

        StateEstimationResult result = Estimate([], previous);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            StateFieldDecisionReason.NoValidEvidence,
            result.FieldDecisions[StateField.GameMode].Reason);
    }

    [TestMethod]
    public void ExpiredPreviousState_IsNotRetainedByHysteresis()
    {
        GameState stalePrevious = new(
            EvaluatedAt.AddSeconds(-6),
            gameMode: GameMode.Gameplay,
            confidence: 0.9d);

        StateEstimationResult result = Estimate(
            CreatePair(StateField.GameMode, nameof(GameMode.Paused)),
            stalePrevious);

        Assert.AreEqual(GameMode.Paused, result.State.GameMode);
        Assert.AreEqual(
            StateFieldDecisionStatus.Confirmed,
            result.FieldDecisions[StateField.GameMode].Status);
    }

    [TestMethod]
    public void CloudCandidateAlone_CannotDetermineField()
    {
        Evidence[] cloudOnly =
        [
            CreateEvidence(
                StateField.GameMode,
                nameof(GameMode.Gameplay),
                2,
                sourceType: EvidenceSourceType.CloudCandidate),
            CreateEvidence(
                StateField.GameMode,
                nameof(GameMode.Gameplay),
                1,
                sourceType: EvidenceSourceType.CloudCandidate),
        ];

        StateEstimationResult result = Estimate(cloudOnly);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.IsTrue(result.EvidenceAudit.All(entry =>
            entry.Reason == EvidenceRejectionReason.CloudCandidateWithoutIndependentConfirmation));
    }

    [TestMethod]
    public void CloudCandidateWithIndependentEvidence_CanContribute()
    {
        Evidence cloud = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            2,
            sourceType: EvidenceSourceType.CloudCandidate);
        Evidence local = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            1,
            sourceType: EvidenceSourceType.LocalVision);

        StateEstimationResult result = Estimate([cloud, local]);

        Assert.AreEqual(GameMode.Gameplay, result.State.GameMode);
        Assert.AreEqual(EvidenceAuditStatus.Participated, AuditFor(result, cloud).Status);
        Assert.AreEqual(EvidenceAuditStatus.Participated, AuditFor(result, local).Status);
    }

    [TestMethod]
    public void NonMenuGameMode_ClearsMenuSubstate()
    {
        StateEstimationResult result = Estimate([
            .. CreatePair(StateField.GameMode, nameof(GameMode.Gameplay)),
            .. CreatePair(StateField.MenuSubstate, nameof(MenuSubstate.Settings), 4, 3),
        ]);

        Assert.AreEqual(GameMode.Gameplay, result.State.GameMode);
        Assert.AreEqual(MenuSubstate.None, result.State.MenuSubstate);
        Assert.AreEqual(
            StateFieldDecisionStatus.ConsistencyOverridden,
            result.FieldDecisions[StateField.MenuSubstate].Status);
    }

    [TestMethod]
    public void UnknownGameMode_ClearsOtherwiseConfirmedMenuSubstate()
    {
        StateEstimationResult result = Estimate(
            CreatePair(StateField.MenuSubstate, nameof(MenuSubstate.Settings)));

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(MenuSubstate.None, result.State.MenuSubstate);
        Assert.AreEqual(
            StateFieldDecisionReason.MenuSubstateClearedForNonMenuMode,
            result.FieldDecisions[StateField.MenuSubstate].Reason);
    }

    [TestMethod]
    public void InputCollectionAndPreviousState_AreNotModified()
    {
        List<Evidence> input = [.. CreatePair(StateField.GameMode, nameof(GameMode.Gameplay))];
        Evidence[] originalOrder = [.. input];
        GameState previous = CreatePreviousState(GameMode.Gameplay);
        DateTimeOffset previousObservedAt = previous.ObservedAt;
        double previousConfidence = previous.Confidence;

        StateEstimationResult result = Estimate(input, previous);

        CollectionAssert.AreEqual(originalOrder, input);
        Assert.AreEqual(previousObservedAt, previous.ObservedAt);
        Assert.AreEqual(previousConfidence, previous.Confidence);
        Assert.AreNotSame(previous, result.State);
    }

    [TestMethod]
    public void ResultCollections_AreReadOnlyDefensiveSnapshots()
    {
        List<Evidence> input = [.. CreatePair(StateField.GameMode, nameof(GameMode.Gameplay))];
        StateEstimationResult result = Estimate(input);
        input.Clear();

        Assert.HasCount(2, result.State.Evidence);
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<EvidenceAuditEntry>)result.EvidenceAudit).Add(result.EvidenceAudit[0]));
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IDictionary<StateField, StateFieldDecision>)result.FieldDecisions).Clear());
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<Guid>)result.FieldDecisions[StateField.GameMode]
                .CandidateSupports[0]
                .EvidenceIds).Clear());
    }

    [TestMethod]
    public void SummaryConfidence_IsBoundedAndNotSingleMaximumCopy()
    {
        StateEstimationResult result = Estimate(CreatePair(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            confidence: 1d));

        Assert.IsGreaterThanOrEqualTo(0d, result.State.Confidence);
        Assert.IsLessThanOrEqualTo(1d, result.State.Confidence);
        Assert.AreEqual(0.25d, result.State.Confidence);
        Assert.AreNotEqual(1d, result.State.Confidence);
    }

    [TestMethod]
    public async Task Estimation_DoesNotCallInputOrClearEmergencyStopLatch()
    {
        FakeAIProvider provider = new();
        FakeInputController input = new();
        FakeControlSafetyStateSource safetyStates = new(CreateSafeControlState());
        ControlSafetyCoordinator coordinator = new(input, safetyStates);
        Assert.IsTrue(coordinator.ArmFromUserAction());
        await coordinator.EmergencyStopAsync();

        StateEstimationResult result = Estimate(CreatePair(
            StateField.GameMode,
            nameof(GameMode.Gameplay)));

        Assert.AreEqual(GameMode.Gameplay, result.State.GameMode);
        Assert.IsTrue(coordinator.IsStopLatched);
        Assert.IsFalse(coordinator.IsArmed);
        Assert.AreEqual(0, provider.AnalyzeCallCount);
        Assert.IsEmpty(input.ExecutedActions);
        Assert.IsEmpty(input.PressedActions);
        Assert.IsFalse(typeof(StateEstimator).GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(IAIProvider)));
    }

    [TestMethod]
    public async Task ConcurrentCalls_WithSameInputsAreDeterministicAndStateless()
    {
        Evidence[] input =
        [
            .. CreatePair(StateField.GameMode, nameof(GameMode.Menu)),
            .. CreatePair(StateField.MenuSubstate, nameof(MenuSubstate.Settings), 4, 3),
        ];
        StateEstimator estimator = new();

        StateEstimationResult[] results = await Task.WhenAll(
            Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
                estimator.Estimate(input, EvaluatedAt, AdapterId, AdapterVersion))));

        Assert.IsTrue(results.All(result => result.State.GameMode == GameMode.Menu));
        Assert.IsTrue(results.All(result => result.State.MenuSubstate == MenuSubstate.Settings));
        Assert.IsTrue(results.All(result => result.State.Confidence == results[0].State.Confidence));
        Assert.IsTrue(results.All(result =>
            result.EvidenceAudit.Select(entry => (entry.EvidenceId, entry.Status, entry.Reason))
                .SequenceEqual(results[0].EvidenceAudit.Select(
                    entry => (entry.EvidenceId, entry.Status, entry.Reason)))));
    }

    [TestMethod]
    public void SameObservationTime_DoesNotSatisfyMultiFrameRequirement()
    {
        StateEstimationResult result = Estimate([
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
            CreateEvidence(StateField.GameMode, nameof(GameMode.Gameplay), 1),
        ]);

        Assert.AreEqual(GameMode.Unknown, result.State.GameMode);
        Assert.AreEqual(
            StateFieldDecisionReason.InsufficientObservationTimes,
            result.FieldDecisions[StateField.GameMode].Reason);
    }

    [TestMethod]
    public void UnsupportedTargetField_IsExplicitlyAudited()
    {
        Evidence unsupported = new(
            Guid.NewGuid(),
            EvidenceSourceType.LocalVision,
            EvaluatedAt.AddSeconds(-1),
            EvaluatedAt.AddSeconds(1),
            "MissionStageId",
            "stage-1",
            0.9d,
            AdapterId,
            AdapterVersion);

        StateEstimationResult result = Estimate([unsupported]);

        Assert.AreEqual(
            EvidenceRejectionReason.UnsupportedTargetField,
            AuditFor(result, unsupported).Reason);
    }

    [TestMethod]
    public void Options_RejectInvalidWindowsCountsAndThresholds()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new StateEstimatorOptions(observationWindow: TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new StateEstimatorOptions(previousStateMaximumAge: TimeSpan.FromHours(2)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new StateEstimatorOptions(minimumDistinctEvidenceCount: 1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new StateEstimatorOptions(
                minimumDistinctEvidenceCount: 2,
                minimumDistinctObservationCount: 3));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new StateEstimatorOptions(minimumCandidateSupport: double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new StateEstimatorOptions(conflictSupportDifference: -0.01d));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new StateEstimatorOptions(minimumPreviousStateConfidence: 1.01d));
    }

    [TestMethod]
    public void SupportedTargetFieldNames_AreStableAndExact()
    {
        string[] expected =
        [
            StateFieldNames.GameMode,
            StateFieldNames.ControlMode,
            StateFieldNames.MenuSubstate,
            StateFieldNames.ObjectiveType,
        ];

        CollectionAssert.AreEqual(
            expected,
            Enum.GetValues<StateField>().Select(StateFieldNames.GetName).ToArray());
        Assert.IsTrue(StateFieldNames.TryGetField("gameMode", out StateField field));
        Assert.AreEqual(StateField.GameMode, field);
        Assert.IsFalse(StateFieldNames.TryGetField("GameMode", out _));
    }

    [TestMethod]
    public void UnknownEvidenceSource_IsRejected()
    {
        Evidence unknownSource = CreateEvidence(
            StateField.GameMode,
            nameof(GameMode.Gameplay),
            1,
            sourceType: EvidenceSourceType.Unknown);

        StateEstimationResult result = Estimate([unknownSource]);

        Assert.AreEqual(
            EvidenceRejectionReason.UnknownSource,
            AuditFor(result, unknownSource).Reason);
    }

    private static StateEstimationResult Estimate(
        IReadOnlyCollection<Evidence> evidence,
        GameState? previous = null,
        StateEstimatorOptions? options = null) =>
        new StateEstimator(options).Estimate(
            evidence,
            EvaluatedAt,
            AdapterId,
            AdapterVersion,
            previous);

    private static Evidence[] CreatePair(
        StateField field,
        string candidate,
        int firstSecondsBeforeEvaluation = 2,
        int secondSecondsBeforeEvaluation = 1,
        double confidence = 0.8d) =>
        [
            CreateEvidence(field, candidate, firstSecondsBeforeEvaluation, confidence),
            CreateEvidence(field, candidate, secondSecondsBeforeEvaluation, confidence),
        ];

    private static Evidence CreateEvidence(
        StateField field,
        string candidate,
        int secondsBeforeEvaluation,
        double confidence = 0.8d,
        Guid? id = null,
        DateTimeOffset? validUntil = null,
        string adapterId = AdapterId,
        string adapterVersion = AdapterVersion,
        EvidenceSourceType sourceType = EvidenceSourceType.LocalVision,
        EvidenceStatus status = EvidenceStatus.Fresh)
    {
        DateTimeOffset observedAt = EvaluatedAt.AddSeconds(-secondsBeforeEvaluation);
        return new Evidence(
            id ?? Guid.NewGuid(),
            sourceType,
            observedAt,
            validUntil ?? EvaluatedAt.AddSeconds(10),
            StateFieldNames.GetName(field),
            candidate,
            confidence,
            adapterId,
            adapterVersion,
            status);
    }

    private static EvidenceAuditEntry AuditFor(
        StateEstimationResult result,
        Evidence evidence) =>
        result.EvidenceAudit.Single(entry => entry.EvidenceId == evidence.Id);

    private static GameState CreatePreviousState(GameMode gameMode) =>
        new(
            EvaluatedAt.AddSeconds(-1),
            gameMode: gameMode,
            confidence: 0.8d);

    private static ControlSafetyState CreateSafeControlState() =>
        new(
            captureTargetId: "capture-target",
            inputTargetId: "input-target",
            captureWindowIdentity: "window-1",
            captureProcessIdentity: "process-1",
            inputWindowIdentity: "window-1",
            inputProcessIdentity: "process-1",
            isInputTargetForeground: true,
            isCaptureHealthy: true,
            isStateFresh: true);
}
