namespace GtaAutoGameplay.Core.Logging;

public sealed record StructuredLogField
{
    public StructuredLogField(string name, StructuredLogValue value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Structured log field names cannot be empty.", nameof(name));
        }

        Name = name;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name { get; }

    public StructuredLogValue Value { get; }
}
