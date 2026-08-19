namespace GtaAutoGameplay.Core.StateEstimation;

public enum StateFieldDecisionReason
{
    ConfirmedByMultiFrameSupport = 0,
    NoValidEvidence,
    InsufficientDistinctEvidence,
    InsufficientObservationTimes,
    InsufficientSupport,
    CompetingCandidates,
    PreviousValueRetainedByHysteresis,
    SwitchingAdvantageNotMetWithoutPreviousSupport,
    MenuSubstateClearedForNonMenuMode,
}
