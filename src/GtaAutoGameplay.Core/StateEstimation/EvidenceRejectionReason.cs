namespace GtaAutoGameplay.Core.StateEstimation;

public enum EvidenceRejectionReason
{
    None = 0,
    DuplicateId,
    FutureObservation,
    OutsideObservationWindow,
    ExplicitlyExpired,
    EvidenceMarkedConflicting,
    AdapterIdMismatch,
    AdapterVersionMismatch,
    UnsupportedTargetField,
    InvalidCandidateValue,
    UnknownSource,
    CloudCandidateWithoutIndependentConfirmation,
}
