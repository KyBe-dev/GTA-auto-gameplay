using System.Text;

namespace GtaAutoGameplay.RepositoryGuard;

public sealed class RepositoryFile
{
    public RepositoryFile(
        string path,
        ReadOnlyMemory<byte> content,
        long length,
        string? historyObjectId = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Repository paths cannot be empty.", nameof(path));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        Path = NormalizePath(path);
        Content = content;
        Length = length;
        HistoryObjectId = historyObjectId;
    }

    public string Path { get; }

    public ReadOnlyMemory<byte> Content { get; }

    public long Length { get; }

    public string? HistoryObjectId { get; }

    public static RepositoryFile FromText(
        string path,
        string content,
        string? historyObjectId = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return new RepositoryFile(path, bytes, bytes.LongLength, historyObjectId);
    }

    internal static string NormalizePath(string path)
    {
        string normalized = path.Replace('\\', '/').TrimStart('/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Repository paths must be relative and cannot traverse directories.", nameof(path));
        }

        return string.Join('/', segments);
    }
}
