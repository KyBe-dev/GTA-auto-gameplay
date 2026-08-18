namespace GtaAutoGameplay.Core.Logging;

public interface IStructuredLogSink
{
    void Write(StructuredLogEvent logEvent);
}
