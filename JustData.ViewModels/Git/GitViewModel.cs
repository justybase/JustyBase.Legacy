using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustData.Application;
using JustData.Application.Files;
using JustData.Application.Git;
using JustData.ViewModels.Files;
using System.Collections.ObjectModel;
using System.Globalization;

namespace JustData.ViewModels.Git;

public sealed class GitViewModel : ObservableObject, IDisposable
{
    private readonly IGitService _gitService;
    private readonly FilesViewModel _filesViewModel;
    private readonly IFilePickerService _filePicker;
    private readonly IGitCommitMessageAiService _commitMessageAi;
    private readonly IUiDispatcher? _uiDispatcher;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;
    private bool _isBusy;
    private bool _isGitAvailable = true;
    private bool _isGeneratingCommitMessage;
    private string? _statusMessage;
    private string? _errorMessage;
    private string _commitMessage = string.Empty;
    private string? _selectedRepoPath;
    private string _branchName = string.Empty;
    private bool _isDetached;
    private string? _activeFilePath;
    private string? _timelineFileName;
    private string? _manualRepoPath;
    private string _localUserName = string.Empty;
    private string _localUserEmail = string.Empty;
    private string? _identitySummary;
    private string? _upstreamBranch;
    private int _refreshVersion;
    private int _previewVersion;
    private int _commitFilesVersion;
    private string? _selectedCommitHash;

    public GitViewModel(
        IGitService gitService,
        FilesViewModel filesViewModel,
        IFilePickerService filePicker,
        IGitCommitMessageAiService? commitMessageAi = null,
        IUiDispatcher? uiDispatcher = null)
    {
        _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        _filesViewModel = filesViewModel ?? throw new ArgumentNullException(nameof(filesViewModel));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _commitMessageAi = commitMessageAi ?? new UnavailableGitCommitMessageAiService();
        _uiDispatcher = uiDispatcher;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        OpenRepositoryCommand = new AsyncRelayCommand(OpenRepositoryAsync, () => !IsBusy);
        CommitCommand = new AsyncRelayCommand(CommitAsync, CanCommit);
        StageAllAndCommitCommand = new AsyncRelayCommand(StageAllAndCommitAsync, CanStageAllAndCommit);
        StageAllCommand = new AsyncRelayCommand(StageAllAsync, CanMutate);
        PullCommand = new AsyncRelayCommand(PullAsync, CanMutate);
        PushCommand = new AsyncRelayCommand(PushAsync, CanMutate);
        SyncCommand = new AsyncRelayCommand(SyncAsync, CanMutate);
        StageSelectedCommand = new AsyncRelayCommand<GitFileStatusItem?>(StageSelectedAsync, CanMutateFile);
        UnstageSelectedCommand = new AsyncRelayCommand<GitFileStatusItem?>(UnstageSelectedAsync, CanMutateFile);
        DiscardSelectedCommand = new AsyncRelayCommand<GitFileStatusItem?>(DiscardSelectedAsync, CanMutateFile);
        PreviewDiffCommand = new AsyncRelayCommand<GitFileStatusItem?>(PreviewDiffAsync, CanMutateFile);
        PreviewCommitFileCommand = new AsyncRelayCommand<GitCommitFileItem?>(PreviewCommitFileAsync, CanPreviewCommitFile);
        PreviewTimelineCommitCommand = new AsyncRelayCommand<GitCommitItem?>(PreviewTimelineCommitAsync, CanPreviewTimelineCommit);
        AddToGitIgnoreCommand = new AsyncRelayCommand<GitFileStatusItem?>(AddToGitIgnoreAsync, CanMutateFile);
        CreateBranchCommand = new AsyncRelayCommand<string?>(CreateBranchAsync, _ => CanMutate());
        MergeBranchCommand = new AsyncRelayCommand<string?>(MergeBranchAsync, _ => CanMutate());
        CheckoutBranchCommand = new AsyncRelayCommand<string?>(CheckoutBranchAsync, _ => CanMutate());
        SaveLocalIdentityCommand = new AsyncRelayCommand(SaveLocalIdentityAsync, CanMutate);
        GenerateCommitMessageCommand = new AsyncRelayCommand(GenerateCommitMessageAsync, CanGenerateCommitMessage);
        SelectRepoCommand = new AsyncRelayCommand<string?>(SelectRepoAsync, path => !IsBusy && !string.IsNullOrWhiteSpace(path));

        _filesViewModel.RootPaths.CollectionChanged += (_, _) => _ = DiscoverAndRefreshAsync();
    }

    public ObservableCollection<string> AvailableRepos { get; } = [];

    public ObservableCollection<GitCommitItem> Commits { get; } = [];

    public ObservableCollection<GitCommitItem> Timeline { get; } = [];

    public ObservableCollection<string> Branches { get; } = [];

    public ObservableCollection<GitCommitFileItem> CommitFiles { get; } = [];

    private IReadOnlyList<GitFileStatusItem> _stagedChanges = [];
    private IReadOnlyList<GitFileStatusItem> _unstagedChanges = [];

    public IReadOnlyList<GitFileStatusItem> StagedChanges
    {
        get => _stagedChanges;
        private set
        {
            if (SetProperty(ref _stagedChanges, value))
            {
                OnPropertyChanged(nameof(StagedCount));
                OnPropertyChanged(nameof(HasUncommittedChanges));
                OnPropertyChanged(nameof(BranchDisplay));
                StageAllAndCommitCommand.NotifyCanExecuteChanged();
                GenerateCommitMessageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<GitFileStatusItem> UnstagedChanges
    {
        get => _unstagedChanges;
        private set
        {
            if (SetProperty(ref _unstagedChanges, value))
            {
                OnPropertyChanged(nameof(UnstagedCount));
                OnPropertyChanged(nameof(HasUncommittedChanges));
                OnPropertyChanged(nameof(BranchDisplay));
                StageAllAndCommitCommand.NotifyCanExecuteChanged();
                GenerateCommitMessageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int StagedCount => StagedChanges.Count;
    public int UnstagedCount => UnstagedChanges.Count;
    public bool HasUncommittedChanges => StagedCount + UnstagedCount > 0;

    /// <summary>All working-tree changes (staged and/or unstaged).</summary>
    public IEnumerable<GitFileStatusItem> AllChanges =>
        StagedChanges.Concat(UnstagedChanges).DistinctBy(c => c.Path, StringComparer.OrdinalIgnoreCase);

    public string? SelectedRepoPath
    {
        get => _selectedRepoPath;
        private set
        {
            if (SetProperty(ref _selectedRepoPath, value))
            {
                NotifyCommands();
                OnPropertyChanged(nameof(HasRepository));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public bool HasRepository => !string.IsNullOrWhiteSpace(SelectedRepoPath);

    public bool ShowEmptyState => !IsBusy && (!IsGitAvailable || !HasRepository);

    public bool IsGitAvailable
    {
        get => _isGitAvailable;
        private set
        {
            if (SetProperty(ref _isGitAvailable, value))
                OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommands();
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public bool IsGeneratingCommitMessage
    {
        get => _isGeneratingCommitMessage;
        private set
        {
            if (SetProperty(ref _isGeneratingCommitMessage, value))
                GenerateCommitMessageCommand.NotifyCanExecuteChanged();
        }
    }

    public bool CanShowGenerateCommitMessage => _commitMessageAi.IsAvailable;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string CommitMessage
    {
        get => _commitMessage;
        set
        {
            if (SetProperty(ref _commitMessage, value ?? string.Empty))
            {
                CommitCommand.NotifyCanExecuteChanged();
                StageAllAndCommitCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string BranchName
    {
        get => _branchName;
        private set
        {
            if (SetProperty(ref _branchName, value ?? string.Empty))
                OnPropertyChanged(nameof(BranchDisplay));
        }
    }

    public bool IsDetached
    {
        get => _isDetached;
        private set
        {
            if (SetProperty(ref _isDetached, value))
                OnPropertyChanged(nameof(BranchDisplay));
        }
    }

    /// <summary>Branch label like VS Code status bar, e.g. main* when dirty.</summary>
    public string BranchDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(BranchName))
                return string.Empty;

            string name = IsDetached ? $"detached ({BranchName})" : BranchName;
            if (!string.IsNullOrWhiteSpace(_upstreamBranch) && !IsDetached)
                name = $"{name} ↔ {_upstreamBranch}";
            return HasUncommittedChanges ? $"{name}*" : name;
        }
    }

    public string? ActiveFilePath
    {
        get => _activeFilePath;
        private set => SetProperty(ref _activeFilePath, value);
    }

    public string? TimelineFileName
    {
        get => _timelineFileName;
        private set => SetProperty(ref _timelineFileName, value);
    }

    public bool HasTimeline => Timeline.Count > 0;

    public string LocalUserName
    {
        get => _localUserName;
        set => SetProperty(ref _localUserName, value ?? string.Empty);
    }

    public string LocalUserEmail
    {
        get => _localUserEmail;
        set => SetProperty(ref _localUserEmail, value ?? string.Empty);
    }

    public string? IdentitySummary
    {
        get => _identitySummary;
        private set => SetProperty(ref _identitySummary, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand OpenRepositoryCommand { get; }
    public IAsyncRelayCommand CommitCommand { get; }
    public IAsyncRelayCommand StageAllAndCommitCommand { get; }
    public IAsyncRelayCommand StageAllCommand { get; }
    public IAsyncRelayCommand PullCommand { get; }
    public IAsyncRelayCommand PushCommand { get; }
    public IAsyncRelayCommand SyncCommand { get; }
    public IAsyncRelayCommand<GitFileStatusItem?> StageSelectedCommand { get; }
    public IAsyncRelayCommand<GitFileStatusItem?> UnstageSelectedCommand { get; }
    public IAsyncRelayCommand<GitFileStatusItem?> DiscardSelectedCommand { get; }
    public IAsyncRelayCommand<GitFileStatusItem?> PreviewDiffCommand { get; }
    public IAsyncRelayCommand<GitCommitFileItem?> PreviewCommitFileCommand { get; }
    public IAsyncRelayCommand<GitCommitItem?> PreviewTimelineCommitCommand { get; }
    public IAsyncRelayCommand<GitFileStatusItem?> AddToGitIgnoreCommand { get; }
    public IAsyncRelayCommand<string?> CreateBranchCommand { get; }
    public IAsyncRelayCommand<string?> MergeBranchCommand { get; }
    public IAsyncRelayCommand<string?> CheckoutBranchCommand { get; }
    public IAsyncRelayCommand SaveLocalIdentityCommand { get; }
    public IAsyncRelayCommand GenerateCommitMessageCommand { get; }
    public IAsyncRelayCommand<string?> SelectRepoCommand { get; }

    public event Action<string>? OpenFileRequested;
    public event Action<GitFileContents>? DiffPreviewRequested;
    public event Action<string>? ErrorOccurred;

    public Task InitializeAsync() => DiscoverAndRefreshAsync();

    public void SetActiveFile(string? filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = SetActiveFileAsync(filePath);
    }

    private async Task SetActiveFileAsync(string? filePath)
    {
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            string? normalized = NormalizeActiveFilePath(filePath);
            if (string.Equals(ActiveFilePath, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            await OnUiAsync(() =>
            {
                ActiveFilePath = normalized;
                TimelineFileName = ActiveFilePath is null ? null : Path.GetFileName(ActiveFilePath);
            }).ConfigureAwait(false);
            await RefreshTimelineAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private static string? NormalizeActiveFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;
        try
        {
            return Path.GetFullPath(filePath.Trim());
        }
        catch
        {
            return filePath.Trim();
        }
    }

    public Task EnsureCommitTooltipAsync(GitCommitItem commit)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return LoadCommitTooltipAsync(commit);
    }

    private async Task LoadCommitTooltipAsync(GitCommitItem commit)
    {
        if (commit.TooltipLoaded || string.IsNullOrWhiteSpace(SelectedRepoPath))
            return;

        try
        {
            GitCommitTooltipInfo info = await _gitService
                .GetCommitTooltipAsync(SelectedRepoPath, commit.Hash, _lifetime.Token)
                .ConfigureAwait(false);

            await OnUiAsync(() => commit.ApplyTooltip(info)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch
        {
            await OnUiAsync(() => commit.ApplyTooltip(new GitCommitTooltipInfo(string.Empty, 0, 0, 0)))
                .ConfigureAwait(false);
        }
    }

    public void RequestOpenFile(GitFileStatusItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(SelectedRepoPath))
            return;

        string fullPath = Path.GetFullPath(Path.Combine(SelectedRepoPath, item.Path.Replace('/', Path.DirectorySeparatorChar)));
        OpenFileRequested?.Invoke(fullPath);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
        OpenFileRequested = null;
        DiffPreviewRequested = null;
        ErrorOccurred = null;
    }

    private async Task DiscoverAndRefreshAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        IsGitAvailable = await _gitService.IsGitAvailableAsync(_lifetime.Token).ConfigureAwait(false);
        if (!IsGitAvailable)
        {
            await OnUiAsync(() =>
            {
                AvailableRepos.Clear();
                SelectedRepoPath = null;
                ApplyStatusFiles([]);
                Commits.Clear();
                Timeline.Clear();
                CommitFiles.Clear();
                _selectedCommitHash = null;
                StatusMessage = "Git is not installed or not on PATH.";
                ErrorMessage = null;
                IdentitySummary = null;
                _upstreamBranch = null;
                OnPropertyChanged(nameof(HasTimeline));
                OnPropertyChanged(nameof(BranchDisplay));
            }).ConfigureAwait(false);
            return;
        }

        var discovered = new List<string>();
        foreach (string root in _filesViewModel.RootPaths)
        {
            string? repo = _gitService.DiscoverRepo(root);
            if (repo is not null && !discovered.Contains(repo, StringComparer.OrdinalIgnoreCase))
                discovered.Add(repo);
        }

        if (!string.IsNullOrWhiteSpace(_manualRepoPath))
        {
            string? manual = _gitService.DiscoverRepo(_manualRepoPath);
            if (manual is not null && !discovered.Contains(manual, StringComparer.OrdinalIgnoreCase))
                discovered.Insert(0, manual);
        }

        await OnUiAsync(() =>
        {
            AvailableRepos.Clear();
            foreach (string repo in discovered)
                AvailableRepos.Add(repo);

            if (SelectedRepoPath is not null
                && discovered.Any(r => string.Equals(r, SelectedRepoPath, StringComparison.OrdinalIgnoreCase)))
            {
                // keep current
            }
            else
            {
                SelectedRepoPath = discovered.FirstOrDefault();
            }
        }).ConfigureAwait(false);

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task OpenRepositoryAsync()
    {
        string? folder = _filePicker.PickFolder();
        if (string.IsNullOrWhiteSpace(folder))
            return;

        string? repo = _gitService.DiscoverRepo(folder);
        if (repo is null)
        {
            await ReportErrorAsync("Selected folder is not a Git repository (no .git found).").ConfigureAwait(false);
            return;
        }

        _manualRepoPath = repo;
        await DiscoverAndRefreshAsync().ConfigureAwait(false);
        await SelectRepoAsync(repo).ConfigureAwait(false);
    }

    private async Task SelectRepoAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        await OnUiAsync(() => SelectedRepoPath = path).ConfigureAwait(false);
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task RefreshAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        bool ownsBusy = !IsBusy;
        if (ownsBusy)
            IsBusy = true;

        ErrorMessage = null;
        if (ownsBusy)
            StatusMessage = "Refreshing…";

        try
        {
            await RefreshCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            if (ownsBusy)
                IsBusy = false;
        }
    }

    private async Task RefreshCoreAsync()
    {
        if (!IsGitAvailable || string.IsNullOrWhiteSpace(SelectedRepoPath))
        {
            await OnUiAsync(() =>
            {
                ApplyStatusFiles([]);
                Commits.Clear();
                Timeline.Clear();
                CommitFiles.Clear();
                _selectedCommitHash = null;
                BranchName = string.Empty;
                IdentitySummary = null;
                _upstreamBranch = null;
                OnPropertyChanged(nameof(HasTimeline));
                OnPropertyChanged(nameof(BranchDisplay));
            }).ConfigureAwait(false);
            return;
        }

        int version = Interlocked.Increment(ref _refreshVersion);

        try
        {
            string repo = SelectedRepoPath;
            GitRepoStatus status = await _gitService.GetStatusAsync(repo, _lifetime.Token).ConfigureAwait(false);
            IReadOnlyList<GitCommitInfo> commits = await _gitService.GetCommitsAsync(repo, 50, _lifetime.Token).ConfigureAwait(false);
            IReadOnlyList<GitBranchInfo> branches = await _gitService.GetBranchesAsync(repo, _lifetime.Token).ConfigureAwait(false);
            GitUserIdentity identity = await _gitService.GetUserIdentityAsync(repo, _lifetime.Token).ConfigureAwait(false);
            string? upstream = await _gitService.GetUpstreamBranchAsync(repo, _lifetime.Token).ConfigureAwait(false);

            if (version != _refreshVersion)
                return;

            GitCommitItem? reloadCommit = null;
            await OnUiAsync(() =>
            {
                BranchName = status.BranchName;
                IsDetached = status.IsDetached;
                _upstreamBranch = upstream;
                ApplyStatusFiles(status.Files);
                ApplyIdentity(identity);
                OnPropertyChanged(nameof(BranchDisplay));

                string? previousHash = _selectedCommitHash;

                Commits.Clear();
                for (int i = 0; i < commits.Count; i++)
                {
                    bool isHead = i == 0;
                    Commits.Add(GitCommitItem.From(
                        commits[i],
                        isCurrent: isHead,
                        branchLabel: isHead ? status.BranchName : null,
                        upstreamLabel: isHead ? upstream : null));
                }

                Branches.Clear();
                foreach (GitBranchInfo branch in branches)
                {
                    if (!branch.IsCurrent)
                        Branches.Add(branch.Name);
                }

                int total = StagedCount + UnstagedCount;
                StatusMessage = total == 0
                    ? $"On {BranchName} — clean"
                    : $"On {BranchName} — {StagedCount} staged, {UnstagedCount} change(s)";
                CommitCommand.NotifyCanExecuteChanged();
                StageAllAndCommitCommand.NotifyCanExecuteChanged();
                GenerateCommitMessageCommand.NotifyCanExecuteChanged();

                reloadCommit = previousHash is null
                    ? null
                    : Commits.FirstOrDefault(c =>
                        string.Equals(c.Hash, previousHash, StringComparison.OrdinalIgnoreCase));

                if (reloadCommit is null)
                {
                    _selectedCommitHash = null;
                    ReplaceCommitFiles([]);
                    InvalidateDiffPreview();
                }
            }).ConfigureAwait(false);

            if (reloadCommit is not null)
                await LoadCommitFilesAsync(reloadCommit).ConfigureAwait(false);

            await RefreshTimelineAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private async Task RefreshIdentityAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedRepoPath))
            return;

        try
        {
            GitUserIdentity identity = await _gitService
                .GetUserIdentityAsync(SelectedRepoPath, _lifetime.Token)
                .ConfigureAwait(false);
            await OnUiAsync(() => ApplyIdentity(identity)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private void ApplyIdentity(GitUserIdentity identity)
    {
        string name = identity.Name ?? "(not set)";
        string email = identity.Email ?? "(not set)";
        string nameScope = identity.NameIsLocal ? "local" : "global";
        string emailScope = identity.EmailIsLocal ? "local" : "global";
        IdentitySummary = $"{name} <{email}>  (name: {nameScope}, email: {emailScope})";

        if (string.IsNullOrWhiteSpace(LocalUserName) && !string.IsNullOrWhiteSpace(identity.Name))
            LocalUserName = identity.Name;
        if (string.IsNullOrWhiteSpace(LocalUserEmail) && !string.IsNullOrWhiteSpace(identity.Email))
            LocalUserEmail = identity.Email;
    }

    private async Task RefreshTimelineAsync()
    {
        if (!IsGitAvailable || string.IsNullOrWhiteSpace(SelectedRepoPath) || string.IsNullOrWhiteSpace(ActiveFilePath))
        {
            await OnUiAsync(() =>
            {
                Timeline.Clear();
                OnPropertyChanged(nameof(HasTimeline));
            }).ConfigureAwait(false);
            return;
        }

        string? repoForFile = _gitService.DiscoverRepo(ActiveFilePath);
        if (repoForFile is null
            || !string.Equals(repoForFile, SelectedRepoPath, StringComparison.OrdinalIgnoreCase))
        {
            await OnUiAsync(() =>
            {
                Timeline.Clear();
                OnPropertyChanged(nameof(HasTimeline));
            }).ConfigureAwait(false);
            return;
        }

        try
        {
            IReadOnlyList<GitCommitInfo> history = await _gitService
                .GetFileHistoryAsync(SelectedRepoPath, ActiveFilePath, 30, _lifetime.Token)
                .ConfigureAwait(false);

            await OnUiAsync(() =>
            {
                Timeline.Clear();
                foreach (GitCommitInfo commit in history)
                    Timeline.Add(GitCommitItem.From(commit));
                OnPropertyChanged(nameof(HasTimeline));
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch
        {
            await OnUiAsync(() =>
            {
                Timeline.Clear();
                OnPropertyChanged(nameof(HasTimeline));
            }).ConfigureAwait(false);
        }
    }

    private async Task CommitAsync()
    {
        if (!CanCommit())
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .CommitAsync(SelectedRepoPath!, CommitMessage.Trim(), _lifetime.Token)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
                return;
            }

            await OnUiAsync(() => CommitMessage = string.Empty).ConfigureAwait(false);
            StatusMessage = "Commit created.";
            await RefreshAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StageAllAndCommitAsync()
    {
        if (!CanStageAllAndCommit())
            return;

        IsBusy = true;
        try
        {
            if (StagedCount + UnstagedCount > 0)
            {
                GitCommandResult stage = await _gitService
                    .StageAllAsync(SelectedRepoPath!, _lifetime.Token)
                    .ConfigureAwait(false);
                if (!stage.Succeeded)
                {
                    await ReportErrorAsync(Truncate(stage.CombinedOutput)).ConfigureAwait(false);
                    return;
                }
            }

            GitCommandResult result = await _gitService
                .CommitAsync(SelectedRepoPath!, CommitMessage.Trim(), _lifetime.Token)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
                return;
            }

            await OnUiAsync(() => CommitMessage = string.Empty).ConfigureAwait(false);
            StatusMessage = "Staged all and committed.";
            await RefreshAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GenerateCommitMessageAsync()
    {
        if (!CanGenerateCommitMessage())
            return;

        await OnUiAsync(() =>
        {
            IsGeneratingCommitMessage = true;
            ErrorMessage = null;
            StatusMessage = "Generating commit message…";
        }).ConfigureAwait(false);

        try
        {
            string context = await _gitService
                .GetWorkingTreeChangeSummaryAsync(SelectedRepoPath!, _lifetime.Token)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(context))
            {
                await ReportErrorAsync("No staged or unstaged changes to summarize.").ConfigureAwait(false);
                return;
            }

            string? message = await _commitMessageAi.GenerateAsync(context, _lifetime.Token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportErrorAsync(
                    "Embedded AI returned an empty commit message. Enable Embedded FIM in Preferences and ensure the model is downloaded.")
                    .ConfigureAwait(false);
                return;
            }

            await OnUiAsync(() =>
            {
                CommitMessage = message;
                StatusMessage = "Commit message generated.";
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
        finally
        {
            await OnUiAsync(() => IsGeneratingCommitMessage = false).ConfigureAwait(false);
        }
    }

    private async Task StageAllAsync()
    {
        if (!CanMutate() || (StagedCount + UnstagedCount) == 0)
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService.StageAllAsync(SelectedRepoPath!, _lifetime.Token).ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
                await RefreshStatusOnlyAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StageSelectedAsync(GitFileStatusItem? item)
    {
        if (item is null || !CanMutate())
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .StageAsync(SelectedRepoPath!, [item.Path], _lifetime.Token)
                .ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
                await RefreshStatusOnlyAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UnstageSelectedAsync(GitFileStatusItem? item)
    {
        if (item is null || !CanMutate() || !item.IsStaged)
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .UnstageAsync(SelectedRepoPath!, [item.Path], _lifetime.Token)
                .ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
                await RefreshStatusOnlyAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DiscardSelectedAsync(GitFileStatusItem? item)
    {
        if (item is null || !CanMutate())
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result;
            if (item.Kind == GitChangeKind.Untracked)
            {
                result = await _gitService
                    .DeleteUntrackedAsync(SelectedRepoPath!, [item.Path], _lifetime.Token)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await _gitService
                    .DiscardAsync(SelectedRepoPath!, [item.Path], _lifetime.Token)
                    .ConfigureAwait(false);
            }

            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
                await RefreshStatusOnlyAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PreviewDiffAsync(GitFileStatusItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(SelectedRepoPath))
            return;

        if (IsLikelyBinaryPath(item.Path))
            return;

        int version = Interlocked.Increment(ref _previewVersion);
        string repo = SelectedRepoPath;

        try
        {
            var status = item.ToStatus();

            GitFileContents contents = await _gitService
                .GetFileContentsAsync(repo, status, _lifetime.Token)
                .ConfigureAwait(false);

            if (version != Volatile.Read(ref _previewVersion))
                return;

            // Skip huge payloads that freeze the UI.
            if (contents.OldText.Length + contents.NewText.Length > 2_000_000)
                return;

            await OnUiAsync(() =>
            {
                if (version != Volatile.Read(ref _previewVersion))
                    return;
                DiffPreviewRequested?.Invoke(contents);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (version == Volatile.Read(ref _previewVersion))
                await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private async Task PreviewCommitFileAsync(GitCommitFileItem? item)
    {
        if (item is null
            || string.IsNullOrWhiteSpace(SelectedRepoPath)
            || string.IsNullOrWhiteSpace(_selectedCommitHash))
            return;

        if (IsLikelyBinaryPath(item.Path))
            return;

        int version = Interlocked.Increment(ref _previewVersion);
        string repo = SelectedRepoPath;
        string hash = _selectedCommitHash;

        try
        {
            var file = new GitCommitFile(item.Path, item.OriginalPath, item.StatusCode);
            GitFileContents contents = await _gitService
                .GetCommitFileContentsAsync(repo, hash, file, _lifetime.Token)
                .ConfigureAwait(false);

            if (version != Volatile.Read(ref _previewVersion))
                return;

            if (contents.OldText.Length + contents.NewText.Length > 2_000_000)
                return;

            await OnUiAsync(() =>
            {
                if (version != Volatile.Read(ref _previewVersion))
                    return;
                DiffPreviewRequested?.Invoke(contents);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (version == Volatile.Read(ref _previewVersion))
                await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private async Task PreviewTimelineCommitAsync(GitCommitItem? commit)
    {
        if (commit is null
            || string.IsNullOrWhiteSpace(SelectedRepoPath)
            || string.IsNullOrWhiteSpace(ActiveFilePath))
            return;

        string relative;
        try
        {
            relative = Path.GetRelativePath(SelectedRepoPath, ActiveFilePath).Replace('\\', '/');
        }
        catch (ArgumentException)
        {
            return;
        }

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            return;

        if (IsLikelyBinaryPath(relative))
            return;

        int version = Interlocked.Increment(ref _previewVersion);
        string repo = SelectedRepoPath;
        string hash = commit.Hash;

        try
        {
            var file = new GitCommitFile(relative, OriginalPath: null, StatusCode: "M");
            GitFileContents contents = await _gitService
                .GetCommitFileContentsAsync(repo, hash, file, _lifetime.Token)
                .ConfigureAwait(false);

            if (version != Volatile.Read(ref _previewVersion))
                return;

            if (contents.OldText.Length + contents.NewText.Length > 2_000_000)
                return;

            await OnUiAsync(() =>
            {
                if (version != Volatile.Read(ref _previewVersion))
                    return;
                DiffPreviewRequested?.Invoke(contents);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (version == Volatile.Read(ref _previewVersion))
                await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    public async Task LoadCommitFilesAsync(GitCommitItem? commit)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        int version = Interlocked.Increment(ref _commitFilesVersion);
        // Drop in-flight / stale side-by-side previews from the previous commit selection.
        InvalidateDiffPreview();

        if (commit is null || string.IsNullOrWhiteSpace(SelectedRepoPath))
        {
            if (version != Volatile.Read(ref _commitFilesVersion))
                return;

            await OnUiAsync(() =>
            {
                if (version != Volatile.Read(ref _commitFilesVersion))
                    return;
                _selectedCommitHash = null;
                ReplaceCommitFiles([]);
                PreviewCommitFileCommand.NotifyCanExecuteChanged();
            }).ConfigureAwait(false);
            return;
        }

        try
        {
            string hash = commit.Hash;
            string repo = SelectedRepoPath;
            IReadOnlyList<GitCommitFile> files = await _gitService
                .GetCommitFilesAsync(repo, hash, _lifetime.Token)
                .ConfigureAwait(false);

            if (version != Volatile.Read(ref _commitFilesVersion))
                return;

            var items = files.Select(f => GitCommitFileItem.From(f, hash)).ToList();
            await OnUiAsync(() =>
            {
                if (version != Volatile.Read(ref _commitFilesVersion))
                    return;
                _selectedCommitHash = hash;
                ReplaceCommitFiles(items);
                PreviewCommitFileCommand.NotifyCanExecuteChanged();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (version == Volatile.Read(ref _commitFilesVersion))
                await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private void ReplaceCommitFiles(IReadOnlyList<GitCommitFileItem> items)
    {
        CommitFiles.Clear();
        foreach (GitCommitFileItem item in items)
            CommitFiles.Add(item);
    }

    /// <summary>Cancels in-flight previews and clears an already-open diff panel.</summary>
    private void InvalidateDiffPreview()
    {
        Interlocked.Increment(ref _previewVersion);
        var empty = new GitFileContents(string.Empty, "—", string.Empty, string.Empty);
        if (_uiDispatcher is null || _uiDispatcher.CheckAccess())
            DiffPreviewRequested?.Invoke(empty);
        else
            _ = _uiDispatcher.InvokeAsync(() => DiffPreviewRequested?.Invoke(empty), _lifetime.Token);
    }

    private async Task RefreshStatusOnlyAsync()
    {
        if (!IsGitAvailable || string.IsNullOrWhiteSpace(SelectedRepoPath))
            return;

        try
        {
            GitRepoStatus status = await _gitService.GetStatusAsync(SelectedRepoPath, _lifetime.Token).ConfigureAwait(false);
            await OnUiAsync(() =>
            {
                BranchName = status.BranchName;
                IsDetached = status.IsDetached;
                ApplyStatusFiles(status.Files);
                int total = StagedCount + UnstagedCount;
                StatusMessage = total == 0
                    ? $"On {BranchName} — clean"
                    : $"On {BranchName} — {StagedCount} staged, {UnstagedCount} change(s)";
                CommitCommand.NotifyCanExecuteChanged();
                StageAllAndCommitCommand.NotifyCanExecuteChanged();
                GenerateCommitMessageCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(BranchDisplay));
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private void ApplyStatusFiles(IReadOnlyList<GitFileStatus> files)
    {
        var staged = new List<GitFileStatusItem>();
        var unstaged = new List<GitFileStatusItem>();
        foreach (GitFileStatus file in files)
        {
            var item = GitFileStatusItem.From(file);
            if (item.IsStaged)
                staged.Add(item);
            if (item.IsUnstaged || item.Kind == GitChangeKind.Untracked)
                unstaged.Add(item);
        }

        StagedChanges = staged;
        UnstagedChanges = unstaged;
    }

    private static bool IsLikelyBinaryPath(string path)
    {
        string ext = Path.GetExtension(path);
        return ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".xls", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".xlsm", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".pdb", StringComparison.OrdinalIgnoreCase);
    }

    private async Task AddToGitIgnoreAsync(GitFileStatusItem? item)
    {
        if (item is null || !CanMutate())
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .AddToGitIgnoreAsync(SelectedRepoPath!, item.Path, _lifetime.Token)
                .ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
            {
                StatusMessage = $"Added '{item.Path}' to .gitignore.";
                await RefreshStatusOnlyAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PullAsync()
    {
        if (!CanMutate())
            return;

        IsBusy = true;
        StatusMessage = "Pulling…";
        try
        {
            GitCommandResult result = await _gitService.PullAsync(SelectedRepoPath!, _lifetime.Token).ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
                StatusMessage = "Pull completed.";
            await RefreshAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PushAsync()
    {
        if (!CanMutate())
            return;

        IsBusy = true;
        StatusMessage = "Pushing…";
        try
        {
            GitCommandResult result = await _gitService.PushAsync(SelectedRepoPath!, _lifetime.Token).ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
                StatusMessage = "Push completed.";
            await RefreshAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SyncAsync()
    {
        if (!CanMutate())
            return;

        IsBusy = true;
        StatusMessage = "Syncing…";
        try
        {
            GitCommandResult pull = await _gitService.PullAsync(SelectedRepoPath!, _lifetime.Token).ConfigureAwait(false);
            if (!pull.Succeeded)
            {
                await ReportErrorAsync(Truncate(pull.CombinedOutput)).ConfigureAwait(false);
                return;
            }

            GitCommandResult push = await _gitService.PushAsync(SelectedRepoPath!, _lifetime.Token).ConfigureAwait(false);
            if (!push.Succeeded)
            {
                await ReportErrorAsync(Truncate(push.CombinedOutput)).ConfigureAwait(false);
                return;
            }

            StatusMessage = "Sync completed.";
            await RefreshAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateBranchAsync(string? branchName)
    {
        if (!CanMutate() || string.IsNullOrWhiteSpace(branchName))
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .CreateBranchAsync(SelectedRepoPath!, branchName.Trim(), checkout: true, _lifetime.Token)
                .ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
            {
                StatusMessage = $"Created and checked out '{branchName.Trim()}'.";
                await RefreshAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task MergeBranchAsync(string? branchName)
    {
        if (!CanMutate() || string.IsNullOrWhiteSpace(branchName))
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .MergeAsync(SelectedRepoPath!, branchName.Trim(), _lifetime.Token)
                .ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
            {
                StatusMessage = $"Merged '{branchName.Trim()}'.";
                await RefreshAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckoutBranchAsync(string? branchName)
    {
        if (!CanMutate() || string.IsNullOrWhiteSpace(branchName))
            return;

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .CheckoutAsync(SelectedRepoPath!, branchName.Trim(), _lifetime.Token)
                .ConfigureAwait(false);
            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
            {
                StatusMessage = $"Checked out '{branchName.Trim()}'.";
                await RefreshAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveLocalIdentityAsync()
    {
        if (!CanMutate())
            return;

        if (string.IsNullOrWhiteSpace(LocalUserName) && string.IsNullOrWhiteSpace(LocalUserEmail))
        {
            await ReportErrorAsync("Enter a name and/or email to set locally.").ConfigureAwait(false);
            return;
        }

        IsBusy = true;
        try
        {
            GitCommandResult result = await _gitService
                .SetLocalUserIdentityAsync(
                    SelectedRepoPath!,
                    string.IsNullOrWhiteSpace(LocalUserName) ? null : LocalUserName,
                    string.IsNullOrWhiteSpace(LocalUserEmail) ? null : LocalUserEmail,
                    _lifetime.Token)
                .ConfigureAwait(false);

            if (!result.Succeeded)
                await ReportErrorAsync(Truncate(result.CombinedOutput)).ConfigureAwait(false);
            else
            {
                StatusMessage = "Local user.name / user.email saved.";
                await RefreshIdentityAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCommit() =>
        CanMutate()
        && !string.IsNullOrWhiteSpace(CommitMessage)
        && StagedCount > 0;

    private bool CanStageAllAndCommit() =>
        CanMutate()
        && !string.IsNullOrWhiteSpace(CommitMessage)
        && (StagedCount + UnstagedCount) > 0;

    private bool CanGenerateCommitMessage() =>
        !IsGeneratingCommitMessage
        && !IsBusy
        && _commitMessageAi.IsAvailable
        && IsGitAvailable
        && !string.IsNullOrWhiteSpace(SelectedRepoPath)
        && (StagedCount + UnstagedCount) > 0;

    private bool CanMutate() =>
        !IsBusy && IsGitAvailable && !string.IsNullOrWhiteSpace(SelectedRepoPath);

    private bool CanMutateFile(GitFileStatusItem? item) =>
        CanMutate() && item is not null;

    private bool CanPreviewCommitFile(GitCommitFileItem? item) =>
        item is not null
        && IsGitAvailable
        && !string.IsNullOrWhiteSpace(SelectedRepoPath)
        && !string.IsNullOrWhiteSpace(_selectedCommitHash);

    private bool CanPreviewTimelineCommit(GitCommitItem? item) =>
        item is not null
        && IsGitAvailable
        && !string.IsNullOrWhiteSpace(SelectedRepoPath)
        && !string.IsNullOrWhiteSpace(ActiveFilePath);

    private void NotifyCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        OpenRepositoryCommand.NotifyCanExecuteChanged();
        CommitCommand.NotifyCanExecuteChanged();
        StageAllAndCommitCommand.NotifyCanExecuteChanged();
        StageAllCommand.NotifyCanExecuteChanged();
        PullCommand.NotifyCanExecuteChanged();
        PushCommand.NotifyCanExecuteChanged();
        SyncCommand.NotifyCanExecuteChanged();
        StageSelectedCommand.NotifyCanExecuteChanged();
        UnstageSelectedCommand.NotifyCanExecuteChanged();
        DiscardSelectedCommand.NotifyCanExecuteChanged();
        PreviewDiffCommand.NotifyCanExecuteChanged();
        PreviewCommitFileCommand.NotifyCanExecuteChanged();
        PreviewTimelineCommitCommand.NotifyCanExecuteChanged();
        AddToGitIgnoreCommand.NotifyCanExecuteChanged();
        CreateBranchCommand.NotifyCanExecuteChanged();
        MergeBranchCommand.NotifyCanExecuteChanged();
        CheckoutBranchCommand.NotifyCanExecuteChanged();
        SaveLocalIdentityCommand.NotifyCanExecuteChanged();
        GenerateCommitMessageCommand.NotifyCanExecuteChanged();
        SelectRepoCommand.NotifyCanExecuteChanged();
    }

    private async Task ReportErrorAsync(string message)
    {
        await OnUiAsync(() =>
        {
            ErrorMessage = message;
            StatusMessage = null;
            ErrorOccurred?.Invoke(message);
        }).ConfigureAwait(false);
    }

    private async Task OnUiAsync(Action action)
    {
        if (_uiDispatcher is null || _uiDispatcher.CheckAccess())
        {
            action();
            return;
        }

        await _uiDispatcher.InvokeAsync(action, _lifetime.Token).ConfigureAwait(false);
    }

    private static string Truncate(string text, int max = 800)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Git command failed.";
        text = text.Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }
}

public sealed class GitFileStatusItem
{
    public required string Path { get; init; }
    public string? OriginalPath { get; init; }
    public GitChangeKind Kind { get; init; }
    public bool IsStaged { get; init; }
    public bool IsUnstaged { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string IndexStatus { get; init; } = string.Empty;
    public string WorkTreeStatus { get; init; } = string.Empty;

    public string DisplayText => string.IsNullOrEmpty(OriginalPath)
        ? $"{StatusCode}  {Path}"
        : $"{StatusCode}  {OriginalPath} → {Path}";

    public static GitFileStatusItem From(GitFileStatus status) => new()
    {
        Path = status.Path,
        OriginalPath = status.OriginalPath,
        Kind = status.Kind,
        IsStaged = status.IsStaged,
        IsUnstaged = status.IsUnstaged,
        StatusCode = status.DisplayStatus,
        IndexStatus = status.IndexStatus,
        WorkTreeStatus = status.WorkTreeStatus
    };

    public GitFileStatus ToStatus() => new(
        Path,
        OriginalPath,
        Kind,
        IsStaged,
        IsUnstaged,
        IndexStatus,
        WorkTreeStatus);

    /// <summary>
    /// For MM files selected in the STAGED list: preview HEAD↔index, not index↔worktree.
    /// </summary>
    public GitFileStatusItem AsStagedOnlyPreview() => new()
    {
        Path = Path,
        OriginalPath = OriginalPath,
        Kind = Kind,
        IsStaged = true,
        IsUnstaged = false,
        StatusCode = StatusCode,
        IndexStatus = IndexStatus,
        WorkTreeStatus = string.Empty
    };

    public override string ToString() => DisplayText;
}

public sealed class GitCommitItem : ObservableObject
{
    private string _body = string.Empty;
    private int _filesChanged;
    private int _insertions;
    private int _deletions;
    private bool _tooltipLoaded;

    public required string Hash { get; init; }
    public required string ShortHash { get; init; }
    public required string Author { get; init; }
    public required DateTimeOffset AuthorDate { get; init; }
    public required string Subject { get; init; }
    public bool IsCurrent { get; init; }
    public string? BranchLabel { get; init; }
    public string? UpstreamLabel { get; init; }

    public string RelativeDate => GitOutputParser.FormatRelativeDate(AuthorDate);
    public string RelativeDateLong => GitOutputParser.FormatRelativeDateLong(AuthorDate);

    public string AbsoluteDateText =>
        AuthorDate == DateTimeOffset.MinValue
            ? string.Empty
            : AuthorDate.ToLocalTime().ToString("MMMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture);

    public string HeaderDateText =>
        string.IsNullOrEmpty(AbsoluteDateText)
            ? RelativeDateLong
            : $"{RelativeDateLong} ({AbsoluteDateText})";

    public string MetaText => $"{Author}  ·  {RelativeDate}  ·  {ShortHash}";

    public string DisplayText => $"{Subject}  {Author}  {RelativeDate}";

    public string Body
    {
        get => _body;
        private set
        {
            if (SetProperty(ref _body, value))
                OnPropertyChanged(nameof(HasBody));
        }
    }

    public int FilesChanged
    {
        get => _filesChanged;
        private set
        {
            if (SetProperty(ref _filesChanged, value))
            {
                OnPropertyChanged(nameof(HasStats));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    public int Insertions
    {
        get => _insertions;
        private set
        {
            if (SetProperty(ref _insertions, value))
                OnPropertyChanged(nameof(TooltipText));
        }
    }

    public int Deletions
    {
        get => _deletions;
        private set
        {
            if (SetProperty(ref _deletions, value))
                OnPropertyChanged(nameof(TooltipText));
        }
    }

    public bool TooltipLoaded
    {
        get => _tooltipLoaded;
        private set => SetProperty(ref _tooltipLoaded, value);
    }

    public bool HasBody => !string.IsNullOrWhiteSpace(Body);
    public bool HasStats => FilesChanged > 0 || Insertions > 0 || Deletions > 0;
    public bool HasUpstream => !string.IsNullOrWhiteSpace(UpstreamLabel);

    public string TooltipText
    {
        get
        {
            var lines = new List<string>
            {
                Subject,
                HeaderDateText,
                MetaText
            };
            if (HasUpstream)
                lines.Add($"Upstream: {UpstreamLabel}");
            if (HasBody)
                lines.Add(Body);
            if (HasStats)
            {
                string files = FilesChanged == 1 ? "1 file changed" : $"{FilesChanged} files changed";
                string stats = files;
                if (Insertions > 0)
                    stats += $", {Insertions} insertions(+)";
                if (Deletions > 0)
                    stats += $", {Deletions} deletions(-)";
                lines.Add(stats);
            }
            else if (!TooltipLoaded)
            {
                lines.Add("Loading details…");
            }

            return string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }
    }

    public static GitCommitItem From(
        GitCommitInfo commit,
        bool isCurrent = false,
        string? branchLabel = null,
        string? upstreamLabel = null) => new()
    {
        Hash = commit.Hash,
        ShortHash = commit.ShortHash,
        Author = commit.Author,
        AuthorDate = commit.AuthorDate,
        Subject = commit.Subject,
        IsCurrent = isCurrent,
        BranchLabel = isCurrent ? branchLabel : null,
        UpstreamLabel = isCurrent ? upstreamLabel : null
    };

    public void ApplyTooltip(GitCommitTooltipInfo info)
    {
        Body = info.Body?.Trim() ?? string.Empty;
        FilesChanged = info.FilesChanged;
        Insertions = info.Insertions;
        Deletions = info.Deletions;
        TooltipLoaded = true;
        OnPropertyChanged(nameof(TooltipText));
    }

    public override string ToString() => DisplayText;
}

public sealed class GitCommitFileItem
{
    public required string Path { get; init; }
    public string? OriginalPath { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string? CommitHash { get; init; }

    public string DisplayText => string.IsNullOrEmpty(OriginalPath)
        ? $"{StatusCode}  {Path}"
        : $"{StatusCode}  {OriginalPath} → {Path}";

    public static GitCommitFileItem From(GitCommitFile file, string? commitHash = null) => new()
    {
        Path = file.Path,
        OriginalPath = file.OriginalPath,
        StatusCode = file.StatusCode,
        CommitHash = commitHash
    };

    public override string ToString() => DisplayText;
}
