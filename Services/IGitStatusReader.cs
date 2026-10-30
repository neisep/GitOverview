using GitOverview.Models;

namespace GitOverview.Services;

public interface IGitStatusReader
{
    Task<RepoStatus> ReadAsync(string repositoryPath, bool fetchFirst, CancellationToken cancellationToken);
}

public sealed class GitCommandException(string message) : Exception(message);
