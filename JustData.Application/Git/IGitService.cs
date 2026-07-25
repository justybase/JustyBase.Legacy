namespace JustData.Application.Git;

public interface IGitService
{
    Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Walks <paramref name="path"/> and its parents looking for a <c>.git</c> directory or file.
    /// Returns the repository root, or null if none is found.
    /// </summary>
    string? DiscoverRepo(string path);

    Task<GitRepoStatus> GetStatusAsync(string repoPath, CancellationToken cancellationToken = default);

    Task<GitCommandResult> StageAsync(string repoPath, IReadOnlyList<string> paths, CancellationToken cancellationToken = default);

    /// <summary>Stages all tracked/untracked changes in the worktree (<c>git add -A</c>).</summary>
    Task<GitCommandResult> StageAllAsync(string repoPath, CancellationToken cancellationToken = default);

    Task<GitCommandResult> UnstageAsync(string repoPath, IReadOnlyList<string> paths, CancellationToken cancellationToken = default);

    Task<GitCommandResult> DiscardAsync(string repoPath, IReadOnlyList<string> paths, CancellationToken cancellationToken = default);

    Task<GitCommandResult> DeleteUntrackedAsync(string repoPath, IReadOnlyList<string> paths, CancellationToken cancellationToken = default);

    Task<GitCommandResult> CommitAsync(string repoPath, string message, CancellationToken cancellationToken = default);

    Task<GitCommandResult> PullAsync(string repoPath, CancellationToken cancellationToken = default);

    Task<GitCommandResult> PushAsync(string repoPath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitBranchInfo>> GetBranchesAsync(string repoPath, CancellationToken cancellationToken = default);

    Task<GitCommandResult> CreateBranchAsync(string repoPath, string branchName, bool checkout, CancellationToken cancellationToken = default);

    Task<GitCommandResult> CheckoutAsync(string repoPath, string branchName, CancellationToken cancellationToken = default);

    Task<GitCommandResult> MergeAsync(string repoPath, string branchName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitCommitInfo>> GetCommitsAsync(string repoPath, int maxCount = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitCommitFile>> GetCommitFilesAsync(string repoPath, string commitHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads left/right text for a side-by-side diff of a file as changed in <paramref name="commitHash"/>
    /// (parent tree vs that commit).
    /// </summary>
    Task<GitFileContents> GetCommitFileContentsAsync(
        string repoPath,
        string commitHash,
        GitCommitFile file,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitCommitInfo>> GetFileHistoryAsync(string repoPath, string filePath, int maxCount = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads left/right text for a side-by-side diff of <paramref name="file"/>.
    /// </summary>
    Task<GitFileContents> GetFileContentsAsync(string repoPath, GitFileStatus file, CancellationToken cancellationToken = default);

    Task<GitCommandResult> AddToGitIgnoreAsync(string repoPath, string relativePath, CancellationToken cancellationToken = default);
}
