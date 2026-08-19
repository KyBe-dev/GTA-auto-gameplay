using GtaAutoGameplay.Core.Targeting;

namespace GtaAutoGameplay.Core.Tests.Fakes;

internal sealed class FakeWindowDiscovery : IWindowDiscovery
{
    private readonly Dictionary<CandidateId, WindowCandidate> _candidates;
    private readonly HashSet<CandidateId> _unavailableCandidates = [];
    private readonly List<WindowSelection> _selections = [];

    public FakeWindowDiscovery(IEnumerable<WindowCandidate> candidates)
    {
        WindowCandidate[] snapshot =
            (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();
        _ = WindowDiscoveryResult.Succeeded(snapshot);
        _candidates = snapshot.ToDictionary(candidate => candidate.CandidateId);
    }

    public WindowDiscoveryFailure? DiscoveryFailure { get; set; }

    public IReadOnlyList<WindowSelection> Selections => _selections.ToArray();

    public ValueTask<WindowDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult(
                WindowDiscoveryResult.Failed(WindowDiscoveryFailure.Cancelled));
        }

        if (DiscoveryFailure is not null)
        {
            return ValueTask.FromResult(
                WindowDiscoveryResult.Failed(DiscoveryFailure.Value));
        }

        return ValueTask.FromResult(
            WindowDiscoveryResult.Succeeded(_candidates.Values));
    }

    public ValueTask<WindowSelectionResult> SelectCandidateAsync(
        CandidateId candidateId,
        DateTimeOffset selectedAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidateId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult(
                WindowSelectionResult.Failed(WindowSelectionFailure.Cancelled));
        }

        if (!_candidates.TryGetValue(candidateId, out WindowCandidate? candidate))
        {
            return ValueTask.FromResult(
                WindowSelectionResult.Failed(WindowSelectionFailure.CandidateNotFound));
        }

        if (_unavailableCandidates.Contains(candidateId))
        {
            return ValueTask.FromResult(
                WindowSelectionResult.Failed(WindowSelectionFailure.CandidateUnavailable));
        }

        if (candidate.IsExpiredAt(selectedAtUtc))
        {
            return ValueTask.FromResult(
                WindowSelectionResult.Failed(WindowSelectionFailure.CandidateExpired));
        }

        WindowSelection selection = new(
            new SelectionId(Guid.NewGuid()),
            candidate.CandidateId,
            candidate.Identity,
            selectedAtUtc);
        _selections.Add(selection);

        return ValueTask.FromResult(WindowSelectionResult.Succeeded(selection));
    }

    public void MakeUnavailable(CandidateId candidateId)
    {
        ArgumentNullException.ThrowIfNull(candidateId);
        _unavailableCandidates.Add(candidateId);
    }
}
