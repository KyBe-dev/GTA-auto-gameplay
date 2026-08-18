using System.Collections.ObjectModel;
using GtaAutoGameplay.Core.Configuration;

namespace GtaAutoGameplay.Core.Logging;

public sealed class InMemoryStructuredLog : IStructuredLogSink, IStructuredLogReader
{
    private readonly object _sync = new();
    private readonly List<StructuredLogEvent> _events = [];
    private readonly StructuredLogOptions _options;
    private readonly TimeProvider _timeProvider;

    public InMemoryStructuredLog(
        StructuredLogOptions options,
        TimeProvider? timeProvider = null)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Copy();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void Write(StructuredLogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        lock (_sync)
        {
            RemoveExpiredLocked();

            while (_events.Count >= _options.Capacity)
            {
                _events.RemoveAt(0);
            }

            _events.Add(logEvent);
        }
    }

    public IReadOnlyList<StructuredLogEvent> GetSnapshot()
    {
        lock (_sync)
        {
            RemoveExpiredLocked();
            StructuredLogEvent[] snapshot = [.. _events];
            return new ReadOnlyCollection<StructuredLogEvent>(snapshot);
        }
    }

    private void RemoveExpiredLocked()
    {
        DateTimeOffset cutoff = _timeProvider.GetUtcNow() - _options.RetentionPeriod;
        _events.RemoveAll(logEvent => logEvent.TimestampUtc < cutoff);
    }
}
