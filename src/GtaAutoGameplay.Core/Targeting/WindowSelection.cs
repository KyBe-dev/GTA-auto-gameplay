namespace GtaAutoGameplay.Core.Targeting;

public sealed class WindowSelection
{
    public WindowSelection(
        SelectionId selectionId,
        CandidateId candidateId,
        WindowIdentitySnapshot identity,
        DateTimeOffset selectedAtUtc)
    {
        SelectionId = selectionId ?? throw new ArgumentNullException(nameof(selectionId));
        CandidateId = candidateId ?? throw new ArgumentNullException(nameof(candidateId));
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));

        if (selectedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must use UTC.", nameof(selectedAtUtc));
        }

        if (selectedAtUtc < identity.CapturedAtUtc || identity.IsExpiredAt(selectedAtUtc))
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectedAtUtc),
                "Selection time must fall within the identity snapshot validity period.");
        }

        SelectedAtUtc = selectedAtUtc;
    }

    public SelectionId SelectionId { get; }

    public CandidateId CandidateId { get; }

    public WindowIdentitySnapshot Identity { get; }

    public DateTimeOffset SelectedAtUtc { get; }

    public DateTimeOffset ValidUntilUtc => Identity.ValidUntilUtc;

    public bool IsExpiredAt(DateTimeOffset utcNow) => Identity.IsExpiredAt(utcNow);
}
