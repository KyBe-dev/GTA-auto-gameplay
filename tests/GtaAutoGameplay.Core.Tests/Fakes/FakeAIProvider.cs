using GtaAutoGameplay.Core.AI;

namespace GtaAutoGameplay.Core.Tests.Fakes;

internal sealed class FakeAIProvider : IAIProvider
{
    private int _analyzeCallCount;

    public string CurrentProviderId { get; set; } = "fake-provider";

    public Func<string>? ProviderIdHandler { get; set; }

    public AIProviderAvailability CurrentAvailability { get; set; } =
        AIProviderAvailability.Ready;

    public Func<AIProviderAvailability>? AvailabilityHandler { get; set; }

    public Func<AIAnalysisRequest, CancellationToken, ValueTask<AIAnalysisResult>>?
        AnalyzeHandler { get; set; }

    public int AnalyzeCallCount => Volatile.Read(ref _analyzeCallCount);

    public string ProviderId => ProviderIdHandler?.Invoke() ?? CurrentProviderId;

    public AIProviderAvailability Availability =>
        AvailabilityHandler?.Invoke() ?? CurrentAvailability;

    public ValueTask<AIAnalysisResult> AnalyzeAsync(
        AIAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _analyzeCallCount);
        return AnalyzeHandler?.Invoke(request, cancellationToken)
            ?? ValueTask.FromResult(CreateValidResult());
    }

    public static AIAnalysisResult CreateValidResult() =>
        new(
            AIProviderAvailability.Ready,
            [new AIStateCandidate("gameMode", "Unknown", 0.5d)],
            requiresHumanConfirmation: true);
}
