namespace GitOverview.Models;

public sealed record RepoStatus(string Path)
{
    public string Name => new DirectoryInfo(Path).Name;
    public string RelativePath { get; init; } = Path;
    public string Branch { get; init; } = "";
    public IReadOnlyList<string> RemoteUrls { get; init; } = [];
    public int UnpushedCommits { get; init; }
    public IReadOnlyList<string> LocalOnlyBranches { get; init; } = [];
    public int Changes { get; init; }
    public int Stashes { get; init; }
    public DateTimeOffset? LastCommit { get; init; }
    public string? Error { get; init; }

    public RepoState State =>
        Error is not null ? RepoState.Error
        : RemoteUrls.Count == 0 ? RepoState.NoRemote
        : UnpushedCommits > 0 ? RepoState.Unpushed
        : Changes > 0 || Stashes > 0 ? RepoState.Uncommitted
        : LocalOnlyBranches.Count > 0 ? RepoState.UnpublishedBranches
        : RepoState.Clean;

    public bool NeedsAttention => State != RepoState.Clean;
    public bool HasRemote => RemoteUrls.Count > 0;
    public string RemoteText => HasRemote ? string.Join(", ", RemoteUrls) : "none";
    public string RemoteDisplay => HasRemote ? string.Join(", ", RemoteUrls.Select(ShortenUrl)) : "none";
    public string LocalOnlyBranchesText => string.Join(", ", LocalOnlyBranches);
    public string LastCommitText => LastCommit?.ToLocalTime().ToString("yyyy-MM-dd") ?? "no commits";

    private static string ShortenUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Segments.Length > 1
            ? $"{uri.Host}/…/{uri.Segments[^1].Trim('/')}"
            : url;
}
