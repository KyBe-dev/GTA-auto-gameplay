using System.Collections.ObjectModel;

namespace GtaAutoGameplay.Core.Logging;

public sealed class StructuredLogEvent
{
    public StructuredLogEvent(
        string eventId,
        DateTimeOffset timestampUtc,
        StructuredLogLevel level,
        StructuredLogCategory category,
        IEnumerable<StructuredLogField>? fields = null)
    {
        EventId = ValidateEventId(eventId);

        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown structured log level.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown structured log category.");
        }

        TimestampUtc = timestampUtc.ToUniversalTime();
        Level = level;
        Category = category;
        Fields = CreateFields(category, fields ?? []);
    }

    public string EventId { get; }

    public DateTimeOffset TimestampUtc { get; }

    public StructuredLogLevel Level { get; }

    public StructuredLogCategory Category { get; }

    public IReadOnlyDictionary<string, StructuredLogValue> Fields { get; }

    private static string ValidateEventId(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)
            || eventId.Length > StructuredLogLimits.MaxEventIdLength
            || eventId.Any(character =>
                !char.IsAsciiLetterOrDigit(character)
                && character is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "Event IDs must contain only ASCII letters, digits, periods, underscores, or hyphens and stay within the configured length limit.",
                nameof(eventId));
        }

        return eventId;
    }

    private static IReadOnlyDictionary<string, StructuredLogValue> CreateFields(
        StructuredLogCategory category,
        IEnumerable<StructuredLogField> fields)
    {
        Dictionary<string, StructuredLogValue> snapshot = new(StringComparer.Ordinal);

        foreach (StructuredLogField field in fields)
        {
            ArgumentNullException.ThrowIfNull(field);

            if (!StructuredLogFieldWhitelist.IsAllowed(category, field.Name))
            {
                throw new ArgumentException(
                    $"Field '{field.Name}' is not allowed for category '{category}'.",
                    nameof(fields));
            }

            if (!snapshot.TryAdd(field.Name, field.Value))
            {
                throw new ArgumentException(
                    $"Field '{field.Name}' was provided more than once.",
                    nameof(fields));
            }

            if (snapshot.Count > StructuredLogLimits.MaxFieldsPerEvent)
            {
                throw new ArgumentException(
                    $"Structured log events cannot contain more than {StructuredLogLimits.MaxFieldsPerEvent} fields.",
                    nameof(fields));
            }
        }

        return new ReadOnlyDictionary<string, StructuredLogValue>(snapshot);
    }
}
