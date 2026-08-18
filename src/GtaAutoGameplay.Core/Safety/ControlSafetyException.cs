namespace GtaAutoGameplay.Core.Safety;

public sealed class ControlSafetyException : InvalidOperationException
{
    public ControlSafetyException(ControlStopReason reason)
        : this(reason, null)
    {
    }

    public ControlSafetyException(ControlStopReason reason, Exception? innerException)
        : base($"Automatic input was denied for safety reason '{reason}'.", innerException)
    {
        Reason = reason;
    }

    public ControlStopReason Reason { get; }
}
