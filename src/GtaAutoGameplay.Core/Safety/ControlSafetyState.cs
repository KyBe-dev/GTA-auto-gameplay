using System.Collections.ObjectModel;
using GtaAutoGameplay.Core.Input;

namespace GtaAutoGameplay.Core.Safety;

public sealed class ControlSafetyState
{
    private readonly ReadOnlyCollection<InputToken> _heldInputs;

    public ControlSafetyState(
        string? captureTargetId = null,
        string? inputTargetId = null,
        string? captureWindowIdentity = null,
        string? captureProcessIdentity = null,
        string? inputWindowIdentity = null,
        string? inputProcessIdentity = null,
        bool isInputTargetForeground = false,
        bool isCaptureHealthy = false,
        bool isStateFresh = false,
        bool isArmed = false,
        IEnumerable<InputToken>? heldInputs = null,
        ControlStopReason stopReason = ControlStopReason.None)
    {
        if (!Enum.IsDefined(stopReason))
        {
            throw new ArgumentOutOfRangeException(nameof(stopReason));
        }

        CaptureTargetId = captureTargetId;
        InputTargetId = inputTargetId;
        CaptureWindowIdentity = captureWindowIdentity;
        CaptureProcessIdentity = captureProcessIdentity;
        InputWindowIdentity = inputWindowIdentity;
        InputProcessIdentity = inputProcessIdentity;
        IsInputTargetForeground = isInputTargetForeground;
        IsCaptureHealthy = isCaptureHealthy;
        IsStateFresh = isStateFresh;
        IsArmed = isArmed;
        _heldInputs = Array.AsReadOnly((heldInputs ?? []).ToArray());
        StopReason = stopReason;
    }

    public string? CaptureTargetId { get; }

    public string? InputTargetId { get; }

    public string? CaptureWindowIdentity { get; }

    public string? CaptureProcessIdentity { get; }

    public string? InputWindowIdentity { get; }

    public string? InputProcessIdentity { get; }

    public bool IsInputTargetForeground { get; }

    public bool IsCaptureHealthy { get; }

    public bool IsStateFresh { get; }

    public bool IsArmed { get; }

    public IReadOnlyList<InputToken> HeldInputs => _heldInputs;

    public ControlStopReason StopReason { get; }

    public bool TargetsMatch =>
        HasValue(CaptureTargetId)
        && HasValue(InputTargetId)
        && HasValue(CaptureWindowIdentity)
        && HasValue(CaptureProcessIdentity)
        && string.Equals(CaptureWindowIdentity, InputWindowIdentity, StringComparison.Ordinal)
        && string.Equals(CaptureProcessIdentity, InputProcessIdentity, StringComparison.Ordinal);

    public bool MeetsInputSafetyConditions =>
        TargetsMatch
        && IsInputTargetForeground
        && IsCaptureHealthy
        && IsStateFresh;

    public bool CanSendInput => IsArmed && MeetsInputSafetyConditions;

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);
}
