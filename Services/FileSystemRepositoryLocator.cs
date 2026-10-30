namespace GitOverview.Services;

/// <summary>Walks the folder tree and stops descending once a repository is found.</summary>
public sealed class FileSystemRepositoryLocator : IRepositoryLocator
{
    private static readonly EnumerationOptions Options = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint
    };

    public IReadOnlyList<string> FindRepositories(string rootPath, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Folder not found: {rootPath}");

        var found = new List<string>();
        var pending = new Stack<string>();
        pending.Push(Path.GetFullPath(rootPath));

        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsRepository(directory))
            {
                found.Add(directory);
                continue;
            }

            foreach (var child in Directory.EnumerateDirectories(directory, "*", Options))
                pending.Push(child);
        }

        return found;
    }

    private static bool IsRepository(string directory)
    {
        var git = Path.Combine(directory, ".git");
        return Directory.Exists(git) || File.Exists(git);
    }
}
