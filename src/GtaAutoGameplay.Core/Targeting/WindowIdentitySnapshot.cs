namespace GtaAutoGameplay.Core.Targeting;

public sealed class WindowIdentitySnapshot
{
    public WindowIdentitySnapshot(
        string windowInstanceId,
        int processId,
        string processInstanceId,
        DateTimeOffset processStartedAtUtc,
        string windowClassName,
        string executableName,
        string executableIdentity,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset validUntilUtc)
    {
        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }

        RequireUtc(processStartedAtUtc, nameof(processStartedAtUtc));
        RequireUtc(capturedAtUtc, nameof(capturedAtUtc));
        RequireUtc(validUntilUtc, nameof(validUntilUtc));

        if (processStartedAtUtc > capturedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processStartedAtUtc),
                "Process start time cannot be later than the snapshot time.");
        }

        if (validUntilUtc <= capturedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(validUntilUtc),
                "Snapshot validity must end after the snapshot time.");
        }

        WindowInstanceId = RequireText(windowInstanceId, nameof(windowInstanceId));
        ProcessId = processId;
        ProcessInstanceId = RequireText(processInstanceId, nameof(processInstanceId));
        ProcessStartedAtUtc = processStartedAtUtc;
        WindowClassName = RequireText(windowClassName, nameof(windowClassName));
        ExecutableName = RequireExecutableName(executableName);
        ExecutableIdentity = RequireText(executableIdentity, nameof(executableIdentity));
        CapturedAtUtc = capturedAtUtc;
        ValidUntilUtc = validUntilUtc;
    }

    public string WindowInstanceId { get; }

    public int ProcessId { get; }

    public string ProcessInstanceId { get; }

    public DateTimeOffset ProcessStartedAtUtc { get; }

    public string WindowClassName { get; }

    public string ExecutableName { get; }

    public string ExecutableIdentity { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public DateTimeOffset ValidUntilUtc { get; }

    public bool IsExpiredAt(DateTimeOffset utcNow)
    {
        RequireUtc(utcNow, nameof(utcNow));
        return utcNow >= ValidUntilUtc;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
        }

        return value;
    }

    private static string RequireExecutableName(string executableName)
    {
        string validatedName = RequireText(executableName, nameof(executableName));
        if (validatedName.Contains('/') ||
            validatedName.Contains('\\') ||
            validatedName.Contains(':'))
        {
            throw new ArgumentException(
                "Executable name cannot contain a path.",
                nameof(executableName));
        }

        return validatedName;
    }

    private static void RequireUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must use UTC.", parameterName);
        }
    }
}
