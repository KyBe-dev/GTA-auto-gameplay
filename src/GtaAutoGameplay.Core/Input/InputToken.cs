namespace GtaAutoGameplay.Core.Input;

public sealed record InputToken
{
    public InputToken(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Input token cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }
}
