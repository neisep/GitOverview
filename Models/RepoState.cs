namespace GitOverview.Models;

/// <summary>Ordered by severity, so sorting by state puts the riskiest repositories first.</summary>
public enum RepoState
{
    Clean,
    UnpublishedBranches,
    Uncommitted,
    Unpushed,
    NoRemote,
    Error
}
