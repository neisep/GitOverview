using System.Globalization;
using GitOverview.Models;

namespace GitOverview.Services;

public sealed class GitStatusReader(IGitCommandRunner git) : IGitStatusReader
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromMinutes(2);

    public async Task<RepoStatus> ReadAsync(string repositoryPath, bool fetchFirst, CancellationToken cancellationToken)
    {
        Task<GitResult> Run(TimeSpan timeout, params string[] arguments) =>
            RunCheckedAsync(repositoryPath, arguments, timeout, cancellationToken);

        if (fetchFirst)
            await Run(FetchTimeout, "fetch", "--all", "--prune", "--quiet").ConfigureAwait(false);

        // All commands are read-only, so they can run concurrently.
        var status = Run(CommandTimeout, "status", "--porcelain=v2", "--branch");
        var branches = Run(CommandTimeout,
            "for-each-ref", "--format=%(refname:short)|%(upstream:short)", "refs/heads");
        var stashes = Run(CommandTimeout, "stash", "list");
        var remoteUrls = ReadRemoteUrlsAsync(repositoryPath, cancellationToken);
        var history = ReadHistoryAsync(repositoryPath, cancellationToken);
        await Task.WhenAll(status, branches, stashes, remoteUrls, history).ConfigureAwait(false);

        var statusLines = (await status).Lines;
        var (unpushed, lastCommit) = await history;

        return new RepoStatus(repositoryPath)
        {
            Branch = ParseBranch(statusLines),
            RemoteUrls = await remoteUrls,
            UnpushedCommits = unpushed,
            LocalOnlyBranches = (await branches).Lines
                .Where(line => line.EndsWith('|'))
                .Select(line => line.TrimEnd('|'))
                .ToList(),
            Changes = statusLines.Count(line => !line.StartsWith('#')),
            Stashes = (await stashes).Lines.Length,
            LastCommit = lastCommit
        };
    }

    private async Task<(int Unpushed, DateTimeOffset? LastCommit)> ReadHistoryAsync(
        string repositoryPath, CancellationToken cancellationToken)
    {
        var head = await git.RunAsync(repositoryPath, ["rev-parse", "-q", "--verify", "HEAD"],
            CommandTimeout, cancellationToken).ConfigureAwait(false);
        if (!head.Success) return (0, null);

        var count = RunCheckedAsync(repositoryPath,
            ["rev-list", "--count", "--branches", "--not", "--remotes"], CommandTimeout, cancellationToken);
        var date = RunCheckedAsync(repositoryPath, ["log", "-1", "--format=%cI"], CommandTimeout, cancellationToken);
        await Task.WhenAll(count, date).ConfigureAwait(false);

        return (int.Parse((await count).Output.Trim(), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse((await date).Output.Trim(), CultureInfo.InvariantCulture));
    }

    private async Task<IReadOnlyList<string>> ReadRemoteUrlsAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        // Exit code 1 means no remote is configured.
        var result = await git.RunAsync(repositoryPath, ["config", "--get-regexp", @"^remote\..*\.url$"],
            CommandTimeout, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode == 1) return [];
        EnsureSuccess(result, "config --get-regexp remote urls");

        return result.Lines.Select(line => line[(line.IndexOf(' ') + 1)..]).ToList();
    }

    private async Task<GitResult> RunCheckedAsync(
        string repositoryPath, string[] arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var result = await git.RunAsync(repositoryPath, arguments, timeout, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, string.Join(' ', arguments));
        return result;
    }

    private static void EnsureSuccess(GitResult result, string command)
    {
        if (!result.Success)
            throw new GitCommandException($"git {command} failed ({result.ExitCode}): {result.Error.Trim()}");
    }

    private static string ParseBranch(IEnumerable<string> statusLines)
    {
        const string prefix = "# branch.head ";
        var head = statusLines.FirstOrDefault(line => line.StartsWith(prefix, StringComparison.Ordinal));
        return head?[prefix.Length..] ?? "";
    }
}
