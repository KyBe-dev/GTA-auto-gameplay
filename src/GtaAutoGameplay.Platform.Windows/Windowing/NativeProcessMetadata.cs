namespace GtaAutoGameplay.Platform.Windows.Windowing;

internal sealed class NativeProcessMetadata
{
    public NativeProcessMetadata(
        DateTimeOffset startedAtUtc,
        string executableName,
        string executablePath)
    {
        if (startedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must use UTC.", nameof(startedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(executableName))
        {
            throw new ArgumentException("Executable name is required.", nameof(executableName));
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Executable path is required.", nameof(executablePath));
        }

        StartedAtUtc = startedAtUtc;
        ExecutableName = executableName;
        ExecutablePath = executablePath;
    }

    public DateTimeOffset StartedAtUtc { get; }

    public string ExecutableName { get; }

    // This full path is confined to the Windows platform process query and is never logged or exported.
    public string ExecutablePath { get; }
}
