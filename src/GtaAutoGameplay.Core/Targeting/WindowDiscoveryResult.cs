using System.Collections.ObjectModel;

namespace GtaAutoGameplay.Core.Targeting;

public sealed class WindowDiscoveryResult
{
    private readonly ReadOnlyCollection<WindowCandidate> _candidates;

    private WindowDiscoveryResult(
        IEnumerable<WindowCandidate> candidates,
        WindowDiscoveryFailure? failure)
    {
        WindowCandidate[] snapshot =
            (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();

        if (snapshot.Any(candidate => candidate is null))
        {
            throw new ArgumentException("Candidate collection cannot contain null values.", nameof(candidates));
        }

        if (snapshot.Select(candidate => candidate.CandidateId).Distinct().Count() != snapshot.Length)
        {
            throw new ArgumentException("Candidate IDs must be unique.", nameof(candidates));
        }

        if (failure is not null &&
            (!Enum.IsDefined(failure.Value) || failure == WindowDiscoveryFailure.Unknown))
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        if (failure is not null && snapshot.Length != 0)
        {
            throw new ArgumentException("Failed discovery results cannot contain candidates.");
        }

        _candidates = Array.AsReadOnly(snapshot);
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;

    public IReadOnlyList<WindowCandidate> Candidates => _candidates;

    public WindowDiscoveryFailure? Failure { get; }

    public static WindowDiscoveryResult Succeeded(IEnumerable<WindowCandidate> candidates) =>
        new(candidates, failure: null);

    public static WindowDiscoveryResult Failed(WindowDiscoveryFailure failure) =>
        new([], failure);
}
