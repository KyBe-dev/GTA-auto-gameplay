namespace GtaAutoGameplay.Core.Configuration;

public sealed class CaptureDataOptions
{
    public CaptureDataOptions(
        bool saveScreenshots,
        bool saveRecordings,
        bool enableFrameReplay)
    {
        SaveScreenshots = saveScreenshots;
        SaveRecordings = saveRecordings;
        EnableFrameReplay = enableFrameReplay;
    }

    public bool SaveScreenshots { get; }

    public bool SaveRecordings { get; }

    public bool EnableFrameReplay { get; }

    public static CaptureDataOptions Disabled { get; } = new(
        saveScreenshots: false,
        saveRecordings: false,
        enableFrameReplay: false);

    internal CaptureDataOptions Copy() =>
        new(SaveScreenshots, SaveRecordings, EnableFrameReplay);
}
