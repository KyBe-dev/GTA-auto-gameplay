namespace GtaAutoGameplay.Core.Targeting;

public sealed class WindowCandidate
{
    public WindowCandidate(
        CandidateId candidateId,
        string title,
        WindowIdentitySnapshot identity)
    {
        CandidateId = candidateId ?? throw new ArgumentNullException(nameof(candidateId));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
    }

    public CandidateId CandidateId { get; }

    public string Title { get; }

    public string ProcessName => Identity.ExecutableName;

    public int ProcessId => Identity.ProcessId;

    public DateTimeOffset DiscoveredAtUtc => Identity.CapturedAtUtc;

    public DateTimeOffset ValidUntilUtc => Identity.ValidUntilUtc;

    public WindowIdentitySnapshot Identity { get; }

    public bool IsExpiredAt(DateTimeOffset utcNow) => Identity.IsExpiredAt(utcNow);
}
