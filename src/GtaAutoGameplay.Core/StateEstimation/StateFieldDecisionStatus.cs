namespace GtaAutoGameplay.Core.StateEstimation;

public enum StateFieldDecisionStatus
{
    Confirmed = 0,
    InsufficientEvidence,
    Conflicting,
    HysteresisHeld,
    ConsistencyOverridden,
}
