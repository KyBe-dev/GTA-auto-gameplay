namespace GtaAutoGameplay.Core.Targeting;

public sealed record CandidateId
{
    public CandidateId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Candidate ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }
}
