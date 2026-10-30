using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GitOverview.Models;
using neisep.NerdFonts;

namespace GitOverview.Converters;

/// <summary>Nerd Font icons and colours for repository states.</summary>
public static class RepoConverters
{
    private static readonly IBrush Green = new ImmutableSolidColorBrush(Color.Parse("#3FB950"));
    private static readonly IBrush Blue = new ImmutableSolidColorBrush(Color.Parse("#58A6FF"));
    private static readonly IBrush Orange = new ImmutableSolidColorBrush(Color.Parse("#D29922"));
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Color.Parse("#F85149"));
    private static readonly IBrush Purple = new ImmutableSolidColorBrush(Color.Parse("#BC8CFF"));

    public static readonly FuncValueConverter<RepoState, string> Glyph = new(state => state switch
    {
        RepoState.Clean => Oct.CheckCircle,
        RepoState.UnpublishedBranches => Oct.GitBranch,
        RepoState.Uncommitted => Oct.Pencil,
        RepoState.Unpushed => Oct.RepoPush,
        RepoState.NoRemote => Oct.CloudOffline,
        _ => Oct.XCircle
    });

    public static readonly FuncValueConverter<RepoState, IBrush> Brush = new(state => state switch
    {
        RepoState.Clean => Green,
        RepoState.UnpublishedBranches => Blue,
        RepoState.Uncommitted => Orange,
        RepoState.Unpushed or RepoState.NoRemote => Red,
        _ => Purple
    });

    public static readonly FuncValueConverter<RepoState, string> Label = new(state => state switch
    {
        RepoState.Clean => "Clean",
        RepoState.UnpublishedBranches => "Unpublished branch",
        RepoState.Uncommitted => "Uncommitted",
        RepoState.Unpushed => "Unpushed commits",
        RepoState.NoRemote => "No remote",
        _ => "Error"
    });

    public static readonly FuncValueConverter<bool, string> RemoteGlyph =
        new(hasRemote => hasRemote ? Oct.Cloud : Oct.CloudOffline);

    public static readonly FuncValueConverter<int, double> CountOpacity =
        new(count => count == 0 ? 0.35 : 1.0);
}
