namespace GtaAutoGameplay.Core.Safety;

public enum ControlStopReason
{
    None = 0,
    NotArmed,
    EmergencyStop,
    TargetIdentityMismatch,
    InputTargetNotForeground,
    CaptureUnhealthy,
    StateStale,
    Cancelled,
    InputControllerFailure,
    SafetyStateUnavailable,
}
