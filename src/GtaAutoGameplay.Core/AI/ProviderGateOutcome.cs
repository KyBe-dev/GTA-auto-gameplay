namespace GtaAutoGameplay.Core.AI;

public sealed class ProviderGateOutcome
{
    public ProviderGateOutcome(
        ProviderGateResultType resultType,
        ProviderFallbackDirective fallbackDirective,
        TimeSpan duration,
        AIProviderAvailability? providerStatus = null,
        AIAnalysisResult? analysisResult = null)
    {
        if (!Enum.IsDefined(resultType))
        {
            throw new ArgumentOutOfRangeException(nameof(resultType));
        }

        if (!Enum.IsDefined(fallbackDirective))
        {
            throw new ArgumentOutOfRangeException(nameof(fallbackDirective));
        }

        if (providerStatus is not null && !Enum.IsDefined(providerStatus.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(providerStatus));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        if (resultType == ProviderGateResultType.Succeeded)
        {
            if (fallbackDirective != ProviderFallbackDirective.None || analysisResult is null)
            {
                throw new ArgumentException(
                    "Successful Provider outcomes require an analysis result and no fallback directive.");
            }
        }
        else if (fallbackDirective == ProviderFallbackDirective.None || analysisResult is not null)
        {
            throw new ArgumentException(
                "Failed Provider outcomes require a fallback directive and cannot expose an analysis result.");
        }

        ResultType = resultType;
        FallbackDirective = fallbackDirective;
        Duration = duration;
        ProviderStatus = providerStatus;
        AnalysisResult = analysisResult;
    }

    public ProviderGateResultType ResultType { get; }

    public ProviderFallbackDirective FallbackDirective { get; }

    public TimeSpan Duration { get; }

    public AIProviderAvailability? ProviderStatus { get; }

    public AIAnalysisResult? AnalysisResult { get; }
}
