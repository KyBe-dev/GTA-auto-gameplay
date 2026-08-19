namespace GtaAutoGameplay.Core.StateEstimation;

public sealed class StateEstimatorOptions
{
    public static readonly TimeSpan DefaultObservationWindow = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DefaultPreviousStateMaximumAge = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan MaximumAllowedWindow = TimeSpan.FromHours(1);

    public const int DefaultMinimumDistinctEvidenceCount = 2;
    public const int DefaultMinimumDistinctObservationCount = 2;
    public const int MaximumAllowedEvidenceCount = 100;
    public const double DefaultMinimumCandidateSupport = 1d;
    public const double DefaultConflictMinimumSupport = 1d;
    public const double DefaultConflictSupportDifference = 0.15d;
    public const double DefaultSwitchingAdvantage = 0.25d;
    public const double DefaultMinimumPreviousStateConfidence = 0.5d;
    public const double MaximumAllowedSupportThreshold = 100d;

    public StateEstimatorOptions(
        TimeSpan? observationWindow = null,
        TimeSpan? previousStateMaximumAge = null,
        int minimumDistinctEvidenceCount = DefaultMinimumDistinctEvidenceCount,
        int minimumDistinctObservationCount = DefaultMinimumDistinctObservationCount,
        double minimumCandidateSupport = DefaultMinimumCandidateSupport,
        double conflictMinimumSupport = DefaultConflictMinimumSupport,
        double conflictSupportDifference = DefaultConflictSupportDifference,
        double switchingAdvantage = DefaultSwitchingAdvantage,
        double minimumPreviousStateConfidence = DefaultMinimumPreviousStateConfidence)
    {
        ObservationWindow = ValidateWindow(
            observationWindow ?? DefaultObservationWindow,
            nameof(observationWindow));
        PreviousStateMaximumAge = ValidateWindow(
            previousStateMaximumAge ?? DefaultPreviousStateMaximumAge,
            nameof(previousStateMaximumAge));

        if (minimumDistinctEvidenceCount is < 2 or > MaximumAllowedEvidenceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumDistinctEvidenceCount));
        }

        if (minimumDistinctObservationCount < 2
            || minimumDistinctObservationCount > minimumDistinctEvidenceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumDistinctObservationCount));
        }

        MinimumDistinctEvidenceCount = minimumDistinctEvidenceCount;
        MinimumDistinctObservationCount = minimumDistinctObservationCount;
        MinimumCandidateSupport = ValidatePositiveSupport(
            minimumCandidateSupport,
            nameof(minimumCandidateSupport));
        ConflictMinimumSupport = ValidatePositiveSupport(
            conflictMinimumSupport,
            nameof(conflictMinimumSupport));
        ConflictSupportDifference = ValidateNonNegativeSupport(
            conflictSupportDifference,
            nameof(conflictSupportDifference));
        SwitchingAdvantage = ValidateNonNegativeSupport(
            switchingAdvantage,
            nameof(switchingAdvantage));

        if (!double.IsFinite(minimumPreviousStateConfidence)
            || minimumPreviousStateConfidence is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumPreviousStateConfidence));
        }

        MinimumPreviousStateConfidence = minimumPreviousStateConfidence;
    }

    public TimeSpan ObservationWindow { get; }

    public TimeSpan PreviousStateMaximumAge { get; }

    public int MinimumDistinctEvidenceCount { get; }

    public int MinimumDistinctObservationCount { get; }

    public double MinimumCandidateSupport { get; }

    public double ConflictMinimumSupport { get; }

    public double ConflictSupportDifference { get; }

    public double SwitchingAdvantage { get; }

    public double MinimumPreviousStateConfidence { get; }

    private static TimeSpan ValidateWindow(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero || value > MaximumAllowedWindow)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    private static double ValidatePositiveSupport(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value > MaximumAllowedSupportThreshold)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    private static double ValidateNonNegativeSupport(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > MaximumAllowedSupportThreshold)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}
