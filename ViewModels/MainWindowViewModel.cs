using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitOverview.Models;
using GitOverview.Services;

namespace GitOverview.ViewModels;

public sealed partial class MainWindowViewModel(IRepositoryLocator locator, IGitStatusReader reader) : ObservableObject
{
    private readonly List<RepoStatus> _all = [];

    public ObservableCollection<RepoStatus> Repositories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    public partial string? RootPath { get; set; }

    [ObservableProperty] public partial bool FetchFirst { get; set; }
    [ObservableProperty] public partial bool OnlyNeedsAttention { get; set; }
    [ObservableProperty] public partial string SearchText { get; set; } = "";
    [ObservableProperty] public partial string StatusMessage { get; set; } = "Choose a folder to scan.";

    [ObservableProperty] public partial int TotalCount { get; set; }
    [ObservableProperty] public partial int AtRiskCount { get; set; }
    [ObservableProperty] public partial int UncommittedCount { get; set; }
    [ObservableProperty] public partial int CleanCount { get; set; }

    partial void OnOnlyNeedsAttentionChanged(bool value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private bool CanScan() => !string.IsNullOrWhiteSpace(RootPath);

    [RelayCommand(CanExecute = nameof(CanScan), IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        var root = RootPath!.Trim();
        _all.Clear();
        Repositories.Clear();
        UpdateCounts();

        try
        {
            StatusMessage = $"Searching {root} for repositories…";
            var paths = await Task.Run(() => locator.FindRepositories(root, cancellationToken), cancellationToken);

            var done = 0;
            var fetchFirst = FetchFirst;
            using var gate = new SemaphoreSlim(Environment.ProcessorCount);
            await Task.WhenAll(paths.Select(async path =>
            {
                // Starting git processes blocks, so all git work stays off the UI thread;
                // only the row update below runs on it.
                var status = await Task.Run(async () =>
                {
                    await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try { return await ReadAsync(path, fetchFirst, cancellationToken).ConfigureAwait(false); }
                    finally { gate.Release(); }
                }, cancellationToken);

                status = status with { RelativePath = Path.GetRelativePath(root, path) };
                _all.Add(status);
                if (Matches(status)) InsertSorted(status);
                UpdateCounts();
                StatusMessage = $"Reading repositories… {++done}/{paths.Count}";
            }));

            ApplyFilter();
            StatusMessage = $"Scanned {paths.Count} repositories in {root} at {DateTime.Now:HH:mm}. Double-click a row to open it.";
        }
        catch (OperationCanceledException)
        {
            ApplyFilter();
            StatusMessage = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            ApplyFilter();
            StatusMessage = $"Scan failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private static void OpenFolder(RepoStatus repository) =>
        Process.Start(new ProcessStartInfo(repository.Path) { UseShellExecute = true });

    private async Task<RepoStatus> ReadAsync(string path, bool fetchFirst, CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadAsync(path, fetchFirst, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is GitCommandException or TimeoutException)
        {
            return new RepoStatus(path) { Error = ex.Message };
        }
    }

    private void ApplyFilter()
    {
        Repositories.Clear();
        foreach (var repository in _all.Where(Matches).Order(DisplayOrder.Instance))
            Repositories.Add(repository);
    }

    private void InsertSorted(RepoStatus repository)
    {
        var index = 0;
        while (index < Repositories.Count && DisplayOrder.Instance.Compare(Repositories[index], repository) <= 0)
            index++;
        Repositories.Insert(index, repository);
    }

    private bool Matches(RepoStatus repository) =>
        (!OnlyNeedsAttention || repository.NeedsAttention) &&
        (string.IsNullOrWhiteSpace(SearchText) ||
         repository.RelativePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
         repository.Branch.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    private void UpdateCounts()
    {
        TotalCount = _all.Count;
        AtRiskCount = _all.Count(r => r.State is RepoState.NoRemote or RepoState.Unpushed or RepoState.Error);
        UncommittedCount = _all.Count(r => r.State is RepoState.Uncommitted or RepoState.UnpublishedBranches);
        CleanCount = _all.Count(r => r.State == RepoState.Clean);
    }

    /// <summary>Riskiest state first, then by path.</summary>
    private sealed class DisplayOrder : IComparer<RepoStatus>
    {
        public static readonly DisplayOrder Instance = new();

        public int Compare(RepoStatus? x, RepoStatus? y)
        {
            var byState = (y?.State ?? 0).CompareTo(x?.State ?? 0);
            return byState != 0
                ? byState
                : StringComparer.OrdinalIgnoreCase.Compare(x?.RelativePath, y?.RelativePath);
        }
    }
}
