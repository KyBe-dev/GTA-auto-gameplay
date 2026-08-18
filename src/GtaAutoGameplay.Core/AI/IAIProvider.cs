namespace GtaAutoGameplay.Core.AI;

public interface IAIProvider
{
    string ProviderId { get; }

    AIProviderAvailability Availability { get; }

    ValueTask<AIAnalysisResult> AnalyzeAsync(
        AIAnalysisRequest request,
        CancellationToken cancellationToken);
}
