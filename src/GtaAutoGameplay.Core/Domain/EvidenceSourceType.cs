namespace GtaAutoGameplay.Core.Domain;

public enum EvidenceSourceType
{
    Unknown = 0,
    LocalVision,
    Ocr,
    MissionTracker,
    ActionResult,
    PersistedPrior,
    CloudCandidate,
    UserConfirmation,
}
