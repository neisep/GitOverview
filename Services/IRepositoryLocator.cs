namespace GitOverview.Services;

public interface IRepositoryLocator
{
    IReadOnlyList<string> FindRepositories(string rootPath, CancellationToken cancellationToken);
}
