using System.Reflection;
using GtaAutoGameplay.Core.Configuration;
using GtaAutoGameplay.Core.Logging;

namespace GtaAutoGameplay.Core.Tests;

[TestClass]
public sealed class StructuredLoggingTests
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 8, 18, 10, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void AllowedFields_CreateStructuredEventWithUtcTimestamp()
    {
        StructuredLogEvent logEvent = new(
            "application.started",
            BaseTime.ToOffset(TimeSpan.FromHours(8)),
            StructuredLogLevel.Information,
            StructuredLogCategory.Application,
            [
                new(
                    StructuredLogFieldNames.Operation,
                    StructuredLogValue.FromString("startup")),
                new(
                    StructuredLogFieldNames.Count,
                    StructuredLogValue.FromInt64(1)),
            ]);

        Assert.AreEqual(TimeSpan.Zero, logEvent.TimestampUtc.Offset);
        Assert.AreEqual(BaseTime, logEvent.TimestampUtc);
        Assert.AreEqual("startup", logEvent.Fields[StructuredLogFieldNames.Operation].StringValue);
        Assert.AreEqual(1L, logEvent.Fields[StructuredLogFieldNames.Count].Int64Value);
    }

    [TestMethod]
    public void UnknownField_IsRejectedByDefault()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new StructuredLogEvent(
            "application.unknown-field",
            BaseTime,
            StructuredLogLevel.Warning,
            StructuredLogCategory.Application,
            [new("unreviewedField", StructuredLogValue.FromString("value"))]));
    }

    [TestMethod]
    [DataRow("apiKey")]
    [DataRow("accessToken")]
    [DataRow("password")]
    [DataRow("rawProviderResponse")]
    [DataRow("ocrText")]
    [DataRow("screenshot")]
    [DataRow("desktopWindowContent")]
    public void SensitiveOrRawContentFieldNames_AreRejected(string fieldName)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new StructuredLogEvent(
            "application.sensitive-field",
            BaseTime,
            StructuredLogLevel.Error,
            StructuredLogCategory.Application,
            [new(fieldName, StructuredLogValue.FromString("must-not-be-recorded"))]));
    }

    [TestMethod]
    public void StructuredLogValue_PublicApiDoesNotAcceptArbitraryObjects()
    {
        bool hasObjectParameter = typeof(StructuredLogValue)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OfType<MethodBase>()
            .SelectMany(member => member.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(object));

        Assert.IsFalse(hasObjectParameter);
        Assert.IsEmpty(typeof(StructuredLogValue).GetConstructors());
    }

    [TestMethod]
    public void StringOverMaximumLength_IsRejected()
    {
        string oversized = new('x', StructuredLogLimits.MaxStringValueLength + 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => StructuredLogValue.FromString(oversized));
    }

    [TestMethod]
    public void CapacityReached_EvictsOldestInsertedEvent()
    {
        ManualTimeProvider time = new(BaseTime);
        InMemoryStructuredLog log = new(
            new StructuredLogOptions(2, TimeSpan.FromHours(1)),
            time);

        log.Write(CreateEvent("event.first", BaseTime));
        log.Write(CreateEvent("event.second", BaseTime.AddSeconds(1)));
        log.Write(CreateEvent("event.third", BaseTime.AddSeconds(2)));

        CollectionAssert.AreEqual(
            new[] { "event.second", "event.third" },
            log.GetSnapshot().Select(logEvent => logEvent.EventId).ToArray());
    }

    [TestMethod]
    public void Snapshot_IsReadOnlyAndCannotMutateInternalLog()
    {
        InMemoryStructuredLog log = new(
            new StructuredLogOptions(2, TimeSpan.FromHours(1)),
            new ManualTimeProvider(BaseTime));
        log.Write(CreateEvent("event.first", BaseTime));

        IReadOnlyList<StructuredLogEvent> snapshot = log.GetSnapshot();
        IList<StructuredLogEvent> mutableView = (IList<StructuredLogEvent>)snapshot;

        Assert.ThrowsExactly<NotSupportedException>(
            () => mutableView.Add(CreateEvent("event.injected", BaseTime)));
        Assert.HasCount(1, log.GetSnapshot());
        Assert.AreEqual("event.first", log.GetSnapshot()[0].EventId);
    }

    [TestMethod]
    public void RetentionReached_RemovesExpiredEvents()
    {
        ManualTimeProvider time = new(BaseTime);
        InMemoryStructuredLog log = new(
            new StructuredLogOptions(3, TimeSpan.FromMinutes(10)),
            time);
        log.Write(CreateEvent("event.expiring", BaseTime));

        time.SetUtcNow(BaseTime.AddMinutes(11));

        Assert.IsEmpty(log.GetSnapshot());
    }

    private static StructuredLogEvent CreateEvent(string eventId, DateTimeOffset timestamp) =>
        new(
            eventId,
            timestamp,
            StructuredLogLevel.Information,
            StructuredLogCategory.Application,
            [
                new(
                    StructuredLogFieldNames.Outcome,
                    StructuredLogValue.FromString("ok")),
            ]);

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow.ToUniversalTime();

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTimeOffset value) =>
            _utcNow = value.ToUniversalTime();
    }
}
