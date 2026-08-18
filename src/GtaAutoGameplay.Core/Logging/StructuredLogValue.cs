namespace GtaAutoGameplay.Core.Logging;

public sealed class StructuredLogValue
{
    private StructuredLogValue(
        StructuredLogValueKind kind,
        string? stringValue = null,
        bool? booleanValue = null,
        long? int64Value = null,
        double? doubleValue = null,
        Guid? guidValue = null,
        DateTimeOffset? utcDateTimeValue = null)
    {
        Kind = kind;
        StringValue = stringValue;
        BooleanValue = booleanValue;
        Int64Value = int64Value;
        DoubleValue = doubleValue;
        GuidValue = guidValue;
        UtcDateTimeValue = utcDateTimeValue;
    }

    public StructuredLogValueKind Kind { get; }

    public string? StringValue { get; }

    public bool? BooleanValue { get; }

    public long? Int64Value { get; }

    public double? DoubleValue { get; }

    public Guid? GuidValue { get; }

    public DateTimeOffset? UtcDateTimeValue { get; }

    public static StructuredLogValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > StructuredLogLimits.MaxStringValueLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value.Length,
                $"Structured log strings cannot exceed {StructuredLogLimits.MaxStringValueLength} characters.");
        }

        return new StructuredLogValue(StructuredLogValueKind.String, stringValue: value);
    }

    public static StructuredLogValue FromBoolean(bool value) =>
        new(StructuredLogValueKind.Boolean, booleanValue: value);

    public static StructuredLogValue FromInt64(long value) =>
        new(StructuredLogValueKind.Int64, int64Value: value);

    public static StructuredLogValue FromDouble(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Structured log floating-point values must be finite.");
        }

        return new StructuredLogValue(StructuredLogValueKind.Double, doubleValue: value);
    }

    public static StructuredLogValue FromGuid(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Structured log GUID values cannot be empty.", nameof(value));
        }

        return new StructuredLogValue(StructuredLogValueKind.Guid, guidValue: value);
    }

    public static StructuredLogValue FromUtcDateTime(DateTimeOffset value) =>
        new(StructuredLogValueKind.UtcDateTime, utcDateTimeValue: value.ToUniversalTime());
}
