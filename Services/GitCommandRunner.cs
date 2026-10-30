using System.Diagnostics;
using System.Text;

namespace GitOverview.Services;

/// <summary>Runs the git CLI read-only, without prompts and without tripping safe.directory checks.</summary>
public sealed class GitCommandRunner : IGitCommandRunner
{
    private static readonly string[] GlobalArguments =
        ["--no-optional-locks", "-c", "safe.directory=*", "-c", "core.quotepath=off"];

    public async Task<GitResult> RunAsync(
        string repositoryPath,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in GlobalArguments) startInfo.ArgumentList.Add(argument);
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(repositoryPath);
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GCM_INTERACTIVE"] = "never";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git.");
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var error = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException(
                $"git {string.Join(' ', arguments)} timed out after {timeout.TotalSeconds:0}s.");
        }

        return new GitResult(process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false));
    }
}
