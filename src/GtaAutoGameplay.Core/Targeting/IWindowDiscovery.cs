namespace GtaAutoGameplay.Core.Targeting;

public interface IWindowDiscovery
{
    ValueTask<WindowDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken);

    ValueTask<WindowSelectionResult> SelectCandidateAsync(
        CandidateId candidateId,
        DateTimeOffset selectedAtUtc,
        CancellationToken cancellationToken);
}
