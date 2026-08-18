namespace GtaAutoGameplay.Core.Logging;

public interface IStructuredLogReader
{
    IReadOnlyList<StructuredLogEvent> GetSnapshot();
}
