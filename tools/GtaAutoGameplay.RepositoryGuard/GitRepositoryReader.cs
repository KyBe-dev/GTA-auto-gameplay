using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace GtaAutoGameplay.RepositoryGuard;

public sealed class GitRepositoryReader
{
    private readonly string _repositoryRoot;

    public GitRepositoryReader(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        _repositoryRoot = Path.GetFullPath(repositoryRoot);

        if (!Directory.Exists(Path.Combine(_repositoryRoot, ".git")))
        {
            throw new DirectoryNotFoundException(
                $"'{_repositoryRoot}' is not a Git worktree with a local .git directory.");
        }
    }

    public IReadOnlyList<RepositoryFile> ReadCandidateWorkingTreeFiles()
    {
        string output = RunGitText(
            "ls-files",
            "--cached",
            "--others",
            "--exclude-standard",
            "-z");

        List<RepositoryFile> files = [];
        foreach (string path in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            string normalized = RepositoryFile.NormalizePath(path);
            string fullPath = GetContainedFullPath(normalized);

            if (!File.Exists(fullPath))
            {
                continue;
            }

            FileInfo info = new(fullPath);
            if (info.LinkTarget is not null
                || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidDataException(
                    $"Candidate path '{normalized}' is a symbolic link or reparse point and will not be followed.");
            }

            byte[] content = info.Length <= RepositoryScanner.MaximumTrackedFileBytes
                ? File.ReadAllBytes(fullPath)
                : [];
            files.Add(new RepositoryFile(normalized, content, info.Length));
        }

        return files;
    }

    public IReadOnlyList<RepositoryFile> ReadReachableHistoryFiles()
    {
        string[] commits = RunGitText("rev-list", "--all")
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        HashSet<(string ObjectId, string Path)> historicalPaths = [];
        foreach (string commit in commits)
        {
            string tree = RunGitText("ls-tree", "-r", "-z", "--full-tree", commit);
            foreach (string entry in tree.Split('\0', StringSplitOptions.RemoveEmptyEntries))
            {
                int tabIndex = entry.IndexOf('\t');
                if (tabIndex < 0)
                {
                    continue;
                }

                string[] metadata = entry[..tabIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (metadata.Length != 3 || !metadata[1].Equals("blob", StringComparison.Ordinal))
                {
                    continue;
                }

                historicalPaths.Add((metadata[2], RepositoryFile.NormalizePath(entry[(tabIndex + 1)..])));
            }
        }

        Dictionary<string, HistoricalBlob> blobs = new(StringComparer.Ordinal);
        List<RepositoryFile> files = [];

        foreach ((string objectId, string path) in historicalPaths
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.ObjectId, StringComparer.Ordinal))
        {
            if (!blobs.TryGetValue(objectId, out HistoricalBlob? blob))
            {
                long length = long.Parse(
                    RunGitText("cat-file", "-s", objectId).Trim(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture);
                byte[] content = length <= RepositoryScanner.MaximumTrackedFileBytes
                    ? RunGitBinary("cat-file", "blob", objectId)
                    : [];
                blob = new HistoricalBlob(length, content);
                blobs.Add(objectId, blob);
            }

            files.Add(new RepositoryFile(path, blob.Content, blob.Length, objectId));
        }

        return files;
    }

    private string GetContainedFullPath(string repositoryPath)
    {
        string fullPath = Path.GetFullPath(
            repositoryPath.Replace('/', Path.DirectorySeparatorChar),
            _repositoryRoot);
        string rootPrefix = _repositoryRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _repositoryRoot
            : _repositoryRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Tracked path '{repositoryPath}' resolves outside the repository root.");
        }

        return fullPath;
    }

    private string RunGitText(params string[] arguments)
    {
        using Process process = StartGit(arguments);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        EnsureSuccess(process, error, arguments);
        return output;
    }

    private byte[] RunGitBinary(params string[] arguments)
    {
        using Process process = StartGit(arguments);
        using MemoryStream output = new();
        Task copy = process.StandardOutput.BaseStream.CopyToAsync(output);
        string error = process.StandardError.ReadToEnd();
        copy.GetAwaiter().GetResult();
        process.WaitForExit();
        EnsureSuccess(process, error, arguments);
        return output.ToArray();
    }

    private Process StartGit(IEnumerable<string> arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = _repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("core.quotepath=false");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("color.ui=false");
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Unable to start the Git process.");
        }

        return process;
    }

    private static void EnsureSuccess(
        Process process,
        string error,
        IReadOnlyCollection<string> arguments)
    {
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Read-only Git command '{string.Join(' ', arguments)}' failed with exit code {process.ExitCode}: {error.Trim()}");
        }
    }

    private sealed record HistoricalBlob(long Length, byte[] Content);
}
