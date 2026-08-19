namespace GtaAutoGameplay.Core.Targeting;

public sealed record SelectionId
{
    public SelectionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Selection ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }
}
