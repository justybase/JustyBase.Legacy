using AppBase.Common;
using DatabaseDataGridView.WinForms.Coloring;
using JustData.Application.Git;
using JustData.ViewModels.Git;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Controls;

public partial class GitControl : UserControl
{
    private readonly GitViewModel _viewModel;
    private readonly BaseWindow _baseWindow;
    private readonly IColorTheme? _colorTheme;
    private readonly Action<string> _openFileHandler;
    private readonly Action<string> _errorHandler;
    private readonly Action<JustData.Application.Git.GitFileContents> _diffPreviewHandler;
    private bool _suppressRepoCombo;
    private bool _applyingDpi;
    private Font? _branchFont;
    private Font? _sectionHeaderFont;
    private Color _sectionBorderColor = Color.FromArgb(200, 200, 204);
    private Color _sectionAccentColor = Color.FromArgb(0, 120, 212);

    private GitFileStatusItem[] _stagedCache = [];
    private GitFileStatusItem[] _unstagedCache = [];
    private GitCommitItem[] _commitsCache = [];
    private GitCommitFileItem[] _commitFilesCache = [];
    private GitCommitItem[] _timelineCache = [];

    private System.Windows.Forms.Timer? _previewDebounceTimer;
    private GitFileStatusItem? _pendingPreviewItem;
    private GitCommitFileItem? _pendingCommitFilePreview;
    private GitCommitItem? _pendingTimelinePreview;
    private bool _commitFilesRefreshQueued;
    private bool _sizingListColumn;

    public GitControl()
    {
        InitializeComponent();
        _viewModel = null!;
        _baseWindow = null!;
        _openFileHandler = _ => { };
        _errorHandler = _ => { };
        _diffPreviewHandler = _ => { };
    }

    public GitControl(GitViewModel viewModel, BaseWindow baseWindow, IColorTheme? colorTheme)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _baseWindow = baseWindow ?? throw new ArgumentNullException(nameof(baseWindow));
        _colorTheme = colorTheme;
        _openFileHandler = path => _ = _baseWindow.OpenSqlFileAsync(path);
        _errorHandler = message =>
        {
            if (IsHandleCreated && !IsDisposed)
                MessageBox.Show(this, message, "Git", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        _diffPreviewHandler = contents => _baseWindow.ShowOrUpdateGitDiff(contents);

        InitializeComponent();
        WireEvents();
        ApplyTheme();
        BindViewModel();
        UpdateEmptyState();
        ApplyDpiMetrics();
        _ = _viewModel.InitializeAsync();
    }

    public void ApplyDpiMetrics()
    {
        if (_pnlHeader is null || _applyingDpi)
            return;

        _applyingDpi = true;
        try
        {
            int dpi = DeviceDpi;
            int pad = DpiScale.Scale(4, dpi);
            int gap = DpiScale.Scale(4, dpi);
            float fontHeight = Font.GetHeight();

            _pnlHeader.Padding = new Padding(pad);
            _lblEmpty.Padding = new Padding(DpiScale.Scale(16, dpi));

            EnsureBranchFont();
            int lineH = Math.Max(DpiScale.Scale(18, dpi), (int)Math.Ceiling(fontHeight) + DpiScale.Scale(4, dpi));
            int buttonH = Math.Max(DpiScale.Scale(26, dpi), (int)Math.Ceiling(fontHeight) + DpiScale.Scale(8, dpi));
            int sectionHeaderH = Math.Max(DpiScale.Scale(26, dpi), (int)Math.Ceiling(fontHeight) + DpiScale.Scale(10, dpi));
            int commitMsgH = Math.Max(DpiScale.Scale(48, dpi), (int)Math.Ceiling(fontHeight * 2.5) + DpiScale.Scale(10, dpi));
            int commitBtnH = Math.Max(DpiScale.Scale(30, dpi), buttonH + DpiScale.Scale(2, dpi));
            int splitterW = Math.Max(DpiScale.Scale(5, dpi), 4);

            if (_pnlHeader.RowStyles.Count >= 1)
                _pnlHeader.RowStyles[0] = new RowStyle(SizeType.Absolute, buttonH + DpiScale.Scale(2, dpi));

            _cmbRepos.MinimumSize = new Size(0, buttonH);
            _cmbRepos.MaximumSize = new Size(0, buttonH + DpiScale.Scale(4, dpi));
            foreach (Button button in new[] { _btnOpenRepo, _btnRefresh, _btnPull, _btnPush, _btnSync, _btnStageAll, _btnMore, _btnSaveIdentity, _btnStageAllCommit, _btnGenerateCommit })
            {
                button.MinimumSize = new Size(0, buttonH);
                button.Padding = new Padding(DpiScale.Scale(6, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(6, dpi), DpiScale.Scale(2, dpi));
                button.Margin = new Padding(0, 0, gap, gap);
            }

            _btnOpenRepo.Margin = new Padding(0, 0, gap, 0);
            _btnRefresh.Margin = new Padding(0);
            _cmbRepos.Margin = new Padding(0, 0, gap, 0);

            _lblBranch.MinimumSize = new Size(0, lineH);
            _lblBranch.Margin = new Padding(0, DpiScale.Scale(1, dpi), 0, DpiScale.Scale(1, dpi));
            _lblStatus.MinimumSize = new Size(0, lineH);
            _lblStatus.Margin = new Padding(0, DpiScale.Scale(1, dpi), 0, 0);

            int headerPadL = DpiScale.Scale(10, dpi);
            int accentW = Math.Max(3, DpiScale.Scale(3, dpi));
            foreach (Panel header in SectionHeaderPanels())
            {
                header.Height = sectionHeaderH;
                header.Padding = new Padding(accentW, 0, 0, 0);
            }

            foreach (Label label in SectionHeaderLabels())
                label.Padding = new Padding(headerPadL, 0, pad, 0);

            EnsureSectionHeaderFont();
            _splitMain.SplitterWidth = splitterW;
            _splitBottom.SplitterWidth = splitterW;
            _splitChangesLists.SplitterWidth = splitterW;
            _splitCommits.SplitterWidth = splitterW;

            _txtCommitMessage.Height = commitMsgH;
            _btnCommit.Height = commitBtnH;
            _btnCommit.Padding = new Padding(0);

            _pnlHeader.PerformLayout();
            int headerHeight = _pnlHeader.PreferredSize.Height;
            if (headerHeight > 0)
                _pnlHeader.Height = headerHeight;

            ApplySplitterMinSizes(dpi);
            ApplySplitterProportions();
            PerformLayout();
        }
        finally
        {
            _applyingDpi = false;
        }
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyDpiMetrics();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        DisposeBranchFont();
        DisposeSectionHeaderFont();
        if (IsHandleCreated)
            ApplyDpiMetrics();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_pnlHeader is null || _applyingDpi)
            return;

        int preferred = _pnlHeader.PreferredSize.Height;
        if (preferred > 0 && Math.Abs(_pnlHeader.Height - preferred) > 1)
        {
            _applyingDpi = true;
            try
            {
                _pnlHeader.Height = preferred;
            }
            finally
            {
                _applyingDpi = false;
            }
        }
    }

    private void ApplySplitterMinSizes(int dpi)
    {
        int mainMin1 = DpiScale.Scale(100, dpi);
        int mainMin2 = DpiScale.Scale(80, dpi);
        int bottomMin = DpiScale.Scale(48, dpi);
        int changesMin = DpiScale.Scale(40, dpi);
        int commitsMin = DpiScale.Scale(40, dpi);

        try
        {
            _splitMain.Panel1MinSize = Math.Min(mainMin1, Math.Max(40, Math.Max(1, Height / 6)));
            _splitMain.Panel2MinSize = Math.Min(mainMin2, Math.Max(40, Math.Max(1, Height / 6)));
            _splitBottom.Panel1MinSize = Math.Min(bottomMin, Math.Max(30, Math.Max(1, Height / 10)));
            _splitBottom.Panel2MinSize = Math.Min(bottomMin, Math.Max(30, Math.Max(1, Height / 10)));
            _splitChangesLists.Panel1MinSize = Math.Min(changesMin, Math.Max(24, Math.Max(1, Height / 12)));
            _splitChangesLists.Panel2MinSize = Math.Min(changesMin, Math.Max(24, Math.Max(1, Height / 12)));
            _splitCommits.Panel1MinSize = Math.Min(commitsMin, Math.Max(24, Math.Max(1, Height / 12)));
            _splitCommits.Panel2MinSize = Math.Min(commitsMin, Math.Max(24, Math.Max(1, Height / 12)));
        }
        catch (InvalidOperationException)
        {
            // SplitContainer rejects mins larger than current size during early layout.
        }
    }

    private void ApplySplitterProportions()
    {
        try
        {
            bool timelineVisible = !_splitBottom.Panel2Collapsed && _pnlTimeline.Visible;
            if (_splitMain.Height > _splitMain.Panel1MinSize + _splitMain.Panel2MinSize + _splitMain.SplitterWidth)
            {
                int percent = timelineVisible ? 40 : 55;
                int desired = Math.Max(_splitMain.Panel1MinSize, _splitMain.Height * percent / 100);
                int max = _splitMain.Height - _splitMain.Panel2MinSize - _splitMain.SplitterWidth;
                _splitMain.SplitterDistance = Math.Clamp(desired, _splitMain.Panel1MinSize, Math.Max(_splitMain.Panel1MinSize, max));
            }

            if (!_splitBottom.Panel2Collapsed
                && _splitBottom.Height > _splitBottom.Panel1MinSize + _splitBottom.Panel2MinSize + _splitBottom.SplitterWidth)
            {
                int desired = Math.Max(_splitBottom.Panel1MinSize, _splitBottom.Height / 2);
                int max = _splitBottom.Height - _splitBottom.Panel2MinSize - _splitBottom.SplitterWidth;
                _splitBottom.SplitterDistance = Math.Clamp(desired, _splitBottom.Panel1MinSize, Math.Max(_splitBottom.Panel1MinSize, max));
            }

            if (_splitCommits.Height > _splitCommits.Panel1MinSize + _splitCommits.Panel2MinSize + _splitCommits.SplitterWidth)
            {
                // ~45% commits list, remainder for FILES IN COMMIT
                int desired = Math.Max(_splitCommits.Panel1MinSize, _splitCommits.Height * 45 / 100);
                int max = _splitCommits.Height - _splitCommits.Panel2MinSize - _splitCommits.SplitterWidth;
                _splitCommits.SplitterDistance = Math.Clamp(desired, _splitCommits.Panel1MinSize, Math.Max(_splitCommits.Panel1MinSize, max));
            }

            // ~40% staged, ~60% unstaged (changes)
            if (_splitChangesLists.Height > _splitChangesLists.Panel1MinSize + _splitChangesLists.Panel2MinSize + _splitChangesLists.SplitterWidth)
            {
                int desired = Math.Max(_splitChangesLists.Panel1MinSize, _splitChangesLists.Height * 40 / 100);
                int max = _splitChangesLists.Height - _splitChangesLists.Panel2MinSize - _splitChangesLists.SplitterWidth;
                _splitChangesLists.SplitterDistance = Math.Clamp(desired, _splitChangesLists.Panel1MinSize, Math.Max(_splitChangesLists.Panel1MinSize, max));
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void EnsureBranchFont()
    {
        if (_branchFont is not null && Equals(_branchFont.FontFamily, Font.FontFamily) && _branchFont.Size == Font.Size)
        {
            _lblBranch.Font = _branchFont;
            return;
        }

        DisposeBranchFont();
        _branchFont = new Font(Font, FontStyle.Bold);
        _lblBranch.Font = _branchFont;
    }

    private void DisposeBranchFont()
    {
        if (_branchFont is null)
            return;
        if (ReferenceEquals(_lblBranch.Font, _branchFont))
            _lblBranch.Font = Font;
        _branchFont.Dispose();
        _branchFont = null;
    }

    private void EnsureSectionHeaderFont()
    {
        if (_sectionHeaderFont is not null
            && Equals(_sectionHeaderFont.FontFamily, Font.FontFamily)
            && Math.Abs(_sectionHeaderFont.Size - Font.Size) < 0.1f)
        {
            ApplySectionHeaderFont();
            return;
        }

        DisposeSectionHeaderFont();
        _sectionHeaderFont = new Font(Font, FontStyle.Bold);
        ApplySectionHeaderFont();
    }

    private void ApplySectionHeaderFont()
    {
        if (_sectionHeaderFont is null)
            return;
        foreach (Label label in SectionHeaderLabels())
            label.Font = _sectionHeaderFont;
    }

    private void DisposeSectionHeaderFont()
    {
        if (_sectionHeaderFont is null)
            return;
        foreach (Label label in SectionHeaderLabels())
        {
            if (ReferenceEquals(label.Font, _sectionHeaderFont))
                label.Font = Font;
        }
        _sectionHeaderFont.Dispose();
        _sectionHeaderFont = null;
    }

    private IEnumerable<Panel> SectionHeaderPanels() =>
    [
        _pnlChangesHeader,
        _pnlStagedHeader,
        _pnlUnstagedHeader,
        _pnlCommitsHeader,
        _pnlCommitFilesHeader,
        _pnlTimelineHeader
    ];

    private IEnumerable<Label> SectionHeaderLabels() =>
    [
        _lblChangesHeader,
        _lblStagedHeader,
        _lblUnstagedHeader,
        _lblCommitsHeader,
        _lblCommitFilesHeader,
        _lblTimelineHeader
    ];

    private void WireEvents()
    {
        foreach (Panel header in SectionHeaderPanels())
            header.Paint += SectionHeader_Paint;

        _btnOpenRepo.Click += async (_, _) => await _viewModel.OpenRepositoryCommand.ExecuteAsync(null);
        _btnRefresh.Click += async (_, _) => await _viewModel.RefreshCommand.ExecuteAsync(null);
        _btnPull.Click += async (_, _) => await _viewModel.PullCommand.ExecuteAsync(null);
        _btnPush.Click += async (_, _) => await _viewModel.PushCommand.ExecuteAsync(null);
        _btnSync.Click += async (_, _) => await _viewModel.SyncCommand.ExecuteAsync(null);
        _btnStageAll.Click += async (_, _) => await _viewModel.StageAllCommand.ExecuteAsync(null);
        _btnCommit.Click += async (_, _) => await _viewModel.CommitCommand.ExecuteAsync(null);
        _btnStageAllCommit.Click += async (_, _) => await _viewModel.StageAllAndCommitCommand.ExecuteAsync(null);
        _btnGenerateCommit.Click += async (_, _) => await _viewModel.GenerateCommitMessageCommand.ExecuteAsync(null);
        _btnMore.Click += (_, _) =>
        {
            _menuMore.Show(_btnMore, new Point(0, _btnMore.Height));
        };
        _btnSaveIdentity.Click += async (_, _) =>
        {
            _viewModel.LocalUserName = _txtUserName.Text;
            _viewModel.LocalUserEmail = _txtUserEmail.Text;
            await _viewModel.SaveLocalIdentityCommand.ExecuteAsync(null);
        };
        _txtUserName.TextChanged += (_, _) => _viewModel.LocalUserName = _txtUserName.Text;
        _txtUserEmail.TextChanged += (_, _) => _viewModel.LocalUserEmail = _txtUserEmail.Text;

        _txtCommitMessage.TextChanged += (_, _) => _viewModel.CommitMessage = _txtCommitMessage.Text;
        _txtCommitMessage.KeyDown += async (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await _viewModel.CommitCommand.ExecuteAsync(null);
            }
        };
        _cmbRepos.SelectedIndexChanged += async (_, _) =>
        {
            if (_suppressRepoCombo)
                return;
            if (_cmbRepos.SelectedItem is string path)
                await _viewModel.SelectRepoCommand.ExecuteAsync(path);
        };

        _previewDebounceTimer = new System.Windows.Forms.Timer { Interval = 150 };
        _previewDebounceTimer.Tick += PreviewDebounceTimer_Tick;

        foreach (ListView list in new[] { _lvStaged, _lvUnstaged, _lvCommits, _lvCommitFiles, _lvTimeline })
        {
            SizeListViewColumn(list);
            list.SizeChanged += (_, _) => SizeListViewColumn(list);
        }

        _lvStaged.RetrieveVirtualItem += (_, e) => RetrieveCachedItem(e, _stagedCache);
        _lvUnstaged.RetrieveVirtualItem += (_, e) => RetrieveCachedItem(e, _unstagedCache);
        _lvCommits.RetrieveVirtualItem += (_, e) => RetrieveCommitItem(e);
        _lvCommitFiles.RetrieveVirtualItem += (_, e) => RetrieveCachedItem(e, _commitFilesCache);
        _lvTimeline.RetrieveVirtualItem += (_, e) => RetrieveCachedItem(e, _timelineCache);
        _lvCommits.ShowItemToolTips = true;

        _lvStaged.SelectedIndexChanged += (_, _) =>
        {
            GitFileStatusItem? item = GetSelectedItem(_lvStaged, _stagedCache);
            if (item is not null)
                SchedulePreview(item, fromStagedList: true);
        };
        _lvUnstaged.SelectedIndexChanged += (_, _) =>
        {
            GitFileStatusItem? item = GetSelectedItem(_lvUnstaged, _unstagedCache);
            if (item is not null)
                SchedulePreview(item, fromStagedList: false);
        };

        _lvStaged.ItemActivate += (_, _) =>
        {
            GitFileStatusItem? item = GetSelectedItem(_lvStaged, _stagedCache);
            if (item is not null)
                _viewModel.RequestOpenFile(item);
        };
        _lvUnstaged.ItemActivate += (_, _) =>
        {
            GitFileStatusItem? item = GetSelectedItem(_lvUnstaged, _unstagedCache);
            if (item is not null)
                _viewModel.RequestOpenFile(item);
        };

        _lvStaged.MouseUp += (s, e) => ChangesList_MouseUp(_lvStaged, _stagedCache, stagedList: true, e);
        _lvUnstaged.MouseUp += (s, e) => ChangesList_MouseUp(_lvUnstaged, _unstagedCache, stagedList: false, e);

        _lvCommits.SelectedIndexChanged += async (_, _) =>
        {
            GitCommitItem? commit = GetSelectedItem(_lvCommits, _commitsCache);
            await _viewModel.LoadCommitFilesAsync(commit);
        };

        _lvCommitFiles.SelectedIndexChanged += (_, _) =>
        {
            GitCommitFileItem? item = GetSelectedItem(_lvCommitFiles, _commitFilesCache);
            if (item is not null)
                ScheduleCommitFilePreview(item);
        };

        _lvTimeline.SelectedIndexChanged += (_, _) =>
        {
            GitCommitItem? commit = GetSelectedItem(_lvTimeline, _timelineCache);
            if (commit is not null)
                ScheduleTimelinePreview(commit);
        };

        _viewModel.OpenFileRequested += _openFileHandler;
        _viewModel.DiffPreviewRequested += _diffPreviewHandler;
        _viewModel.ErrorOccurred += _errorHandler;
    }

    private void BindViewModel()
    {
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.Commits.CollectionChanged += (_, _) => RefreshCommitsList();
        _viewModel.Timeline.CollectionChanged += (_, _) => RefreshTimelineList();
        _viewModel.CommitFiles.CollectionChanged += (_, _) => QueueCommitFilesRefresh();
        _viewModel.AvailableRepos.CollectionChanged += (_, _) => RefreshReposCombo();

        RefreshReposCombo();
        RefreshStagedList();
        RefreshUnstagedList();
        RefreshCommitsList();
        RefreshCommitFilesList();
        RefreshTimelineList();
        SyncStaticFields();
    }

    /// <summary>
    /// Coalesce Clear+Add storms into a single VirtualListSize update on the next UI tick.
    /// </summary>
    private void QueueCommitFilesRefresh()
    {
        if (_commitFilesRefreshQueued)
            return;

        _commitFilesRefreshQueued = true;
        if (IsHandleCreated)
        {
            BeginInvoke(() =>
            {
                _commitFilesRefreshQueued = false;
                RefreshCommitFilesList();
            });
        }
        else
        {
            _commitFilesRefreshQueued = false;
            RefreshCommitFilesList();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ViewModel_PropertyChanged(sender, e));
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(GitViewModel.CommitMessage):
                if (_txtCommitMessage.Text != _viewModel.CommitMessage)
                    _txtCommitMessage.Text = _viewModel.CommitMessage;
                break;
            case nameof(GitViewModel.StagedChanges):
                RefreshStagedList();
                break;
            case nameof(GitViewModel.UnstagedChanges):
                RefreshUnstagedList();
                break;
            case nameof(GitViewModel.StagedCount):
                _lblStagedHeader.Text = $"STAGED ({_viewModel.StagedCount})";
                break;
            case nameof(GitViewModel.UnstagedCount):
                _lblUnstagedHeader.Text = $"CHANGES ({_viewModel.UnstagedCount})";
                break;
            case nameof(GitViewModel.BranchName):
            case nameof(GitViewModel.BranchDisplay):
            case nameof(GitViewModel.IsDetached):
            case nameof(GitViewModel.StatusMessage):
            case nameof(GitViewModel.SelectedRepoPath):
            case nameof(GitViewModel.TimelineFileName):
            case nameof(GitViewModel.IsBusy):
            case nameof(GitViewModel.IsGitAvailable):
            case nameof(GitViewModel.HasRepository):
            case nameof(GitViewModel.ShowEmptyState):
            case nameof(GitViewModel.HasTimeline):
            case nameof(GitViewModel.IdentitySummary):
            case nameof(GitViewModel.LocalUserName):
            case nameof(GitViewModel.LocalUserEmail):
            case nameof(GitViewModel.IsGeneratingCommitMessage):
            case nameof(GitViewModel.CanShowGenerateCommitMessage):
                SyncStaticFields();
                UpdateEmptyState();
                UpdateEnabledState();
                break;
        }
    }

    private void SyncStaticFields()
    {
        _lblBranch.Text = string.IsNullOrWhiteSpace(_viewModel.BranchDisplay)
            ? "—"
            : _viewModel.BranchDisplay;
        _lblStatus.Text = _viewModel.StatusMessage ?? string.Empty;
        _lblIdentity.Text = _viewModel.IdentitySummary ?? string.Empty;

        if (_txtUserName.Text != _viewModel.LocalUserName)
            _txtUserName.Text = _viewModel.LocalUserName;
        if (_txtUserEmail.Text != _viewModel.LocalUserEmail)
            _txtUserEmail.Text = _viewModel.LocalUserEmail;

        _btnGenerateCommit.Visible = _viewModel.CanShowGenerateCommitMessage;

        string timelineTitle = string.IsNullOrWhiteSpace(_viewModel.TimelineFileName)
            ? "TIMELINE"
            : $"TIMELINE  {_viewModel.TimelineFileName}";
        _lblTimelineHeader.Text = timelineTitle;

        if (_cmbRepos.SelectedItem as string != _viewModel.SelectedRepoPath)
        {
            _suppressRepoCombo = true;
            try
            {
                if (_viewModel.SelectedRepoPath is not null
                    && _cmbRepos.Items.Contains(_viewModel.SelectedRepoPath))
                {
                    _cmbRepos.SelectedItem = _viewModel.SelectedRepoPath;
                }
            }
            finally
            {
                _suppressRepoCombo = false;
            }
        }
    }

    private void UpdateEmptyState()
    {
        bool empty = _viewModel.ShowEmptyState;
        _pnlEmpty.Visible = empty;
        _splitMain.Visible = !empty;

        if (!_viewModel.IsGitAvailable)
            _lblEmpty.Text = "Git is not installed or not available on PATH.\nInstall Git for Windows, then click Refresh.";
        else if (!_viewModel.HasRepository)
            _lblEmpty.Text = "No Git repository found in Files folders.\nOpen a repository or add a folder that contains .git.";
    }

    private void UpdateEnabledState()
    {
        bool enabled = _viewModel.HasRepository && !_viewModel.IsBusy && _viewModel.IsGitAvailable;
        _btnPull.Enabled = enabled;
        _btnPush.Enabled = enabled;
        _btnSync.Enabled = enabled;
        _btnStageAll.Enabled = enabled;
        _btnCommit.Enabled = enabled;
        _btnStageAllCommit.Enabled = enabled;
        _btnGenerateCommit.Enabled = enabled && !_viewModel.IsGeneratingCommitMessage;
        _btnMore.Enabled = enabled;
        _btnSaveIdentity.Enabled = enabled;
        _txtUserName.Enabled = enabled;
        _txtUserEmail.Enabled = enabled;
        _txtCommitMessage.Enabled = enabled;
        _btnRefresh.Enabled = !_viewModel.IsBusy;
        _btnOpenRepo.Enabled = !_viewModel.IsBusy;
        _cmbRepos.Enabled = !_viewModel.IsBusy && _viewModel.AvailableRepos.Count > 0;
    }

    private void RefreshReposCombo()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshReposCombo);
            return;
        }

        _suppressRepoCombo = true;
        try
        {
            _cmbRepos.Items.Clear();
            foreach (string repo in _viewModel.AvailableRepos)
                _cmbRepos.Items.Add(repo);

            if (_viewModel.SelectedRepoPath is not null
                && _cmbRepos.Items.Contains(_viewModel.SelectedRepoPath))
            {
                _cmbRepos.SelectedItem = _viewModel.SelectedRepoPath;
            }
        }
        finally
        {
            _suppressRepoCombo = false;
        }
    }

    private void RefreshStagedList()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshStagedList);
            return;
        }

        _stagedCache = _viewModel.StagedChanges.ToArray();
        ApplyVirtualListSize(_lvStaged, _stagedCache.Length);
        _lblStagedHeader.Text = $"STAGED ({_stagedCache.Length})";
    }

    private void RefreshUnstagedList()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshUnstagedList);
            return;
        }

        _unstagedCache = _viewModel.UnstagedChanges.ToArray();
        ApplyVirtualListSize(_lvUnstaged, _unstagedCache.Length);
        _lblUnstagedHeader.Text = $"CHANGES ({_unstagedCache.Length})";
    }

    private void RefreshCommitsList()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshCommitsList);
            return;
        }

        _commitsCache = _viewModel.Commits.ToArray();
        ApplyVirtualListSize(_lvCommits, _commitsCache.Length);
    }

    private void RefreshCommitFilesList()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshCommitFilesList);
            return;
        }

        _commitFilesCache = _viewModel.CommitFiles.ToArray();
        ApplyVirtualListSize(_lvCommitFiles, _commitFilesCache.Length);
        _lblCommitFilesHeader.Text = $"FILES IN COMMIT ({_commitFilesCache.Length})";

        if (_commitFilesCache.Length > 0)
            ApplySplitterProportions();
    }

    private void RefreshTimelineList()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshTimelineList);
            return;
        }

        bool showTimeline = !string.IsNullOrWhiteSpace(_viewModel.ActiveFilePath);
        _pnlTimeline.Visible = showTimeline;
        try
        {
            _splitBottom.Panel2Collapsed = !showTimeline;
        }
        catch (InvalidOperationException)
        {
        }

        _timelineCache = _viewModel.Timeline.ToArray();
        ApplyVirtualListSize(_lvTimeline, _timelineCache.Length);

        ApplySplitterProportions();
    }

    private void ApplyVirtualListSize(ListView list, int count)
    {
        list.BeginUpdate();
        try
        {
            // WinForms VirtualMode: resetting through 0 repairs broken scroll/paint state.
            if (list.VirtualListSize != 0)
                list.VirtualListSize = 0;
            list.VirtualListSize = Math.Max(0, count);
            SizeListViewColumn(list);
            list.Invalidate(true);
        }
        finally
        {
            list.EndUpdate();
        }
    }

    private void SizeListViewColumn(ListView list)
    {
        if (_sizingListColumn || list.Columns.Count == 0 || list.ClientSize.Width <= 0)
            return;

        _sizingListColumn = true;
        try
        {
            // Avoid subtracting scrollbar width up-front (causes H/V scrollbar death spiral).
            int width = Math.Max(1, list.ClientSize.Width - 1);
            if (list.Columns[0].Width != width)
                list.Columns[0].Width = width;
        }
        finally
        {
            _sizingListColumn = false;
        }
    }

    private void SchedulePreview(GitFileStatusItem item, bool fromStagedList)
    {
        _pendingCommitFilePreview = null;
        _pendingTimelinePreview = null;
        // MM in STAGED list must show HEAD↔index, not the unstaged index↔worktree path.
        _pendingPreviewItem = fromStagedList && item.IsStaged && item.IsUnstaged
            ? item.AsStagedOnlyPreview()
            : item;
        if (_previewDebounceTimer is null)
            return;

        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    private void ScheduleCommitFilePreview(GitCommitFileItem item)
    {
        _pendingPreviewItem = null;
        _pendingTimelinePreview = null;
        _pendingCommitFilePreview = item;
        if (_previewDebounceTimer is null)
            return;

        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    private void ScheduleTimelinePreview(GitCommitItem commit)
    {
        _pendingPreviewItem = null;
        _pendingCommitFilePreview = null;
        _pendingTimelinePreview = commit;
        if (_previewDebounceTimer is null)
            return;

        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    private async void PreviewDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _previewDebounceTimer?.Stop();
        GitFileStatusItem? changeItem = _pendingPreviewItem;
        GitCommitFileItem? commitFile = _pendingCommitFilePreview;
        GitCommitItem? timelineCommit = _pendingTimelinePreview;
        _pendingPreviewItem = null;
        _pendingCommitFilePreview = null;
        _pendingTimelinePreview = null;

        if (changeItem is not null)
            await _viewModel.PreviewDiffCommand.ExecuteAsync(changeItem);
        else if (commitFile is not null)
            await _viewModel.PreviewCommitFileCommand.ExecuteAsync(commitFile);
        else if (timelineCommit is not null)
            await _viewModel.PreviewTimelineCommitCommand.ExecuteAsync(timelineCommit);
    }

    private void ChangesList_MouseUp(ListView list, GitFileStatusItem[] cache, bool stagedList, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
            return;

        ListViewItem? hit = list.GetItemAt(e.X, e.Y);
        if (hit is null)
            return;

        list.SelectedIndices.Clear();
        list.SelectedIndices.Add(hit.Index);

        GitFileStatusItem? item = GetSelectedItem(list, cache);
        if (item is null)
            return;

        GitFileStatusItem previewItem = stagedList && item.IsStaged && item.IsUnstaged
            ? item.AsStagedOnlyPreview()
            : item;

        var menu = new ContextMenuStrip();
        menu.Items.Add("View Diff", null, async (_, _) => await _viewModel.PreviewDiffCommand.ExecuteAsync(previewItem));

        if (stagedList)
            menu.Items.Add("Unstage", null, async (_, _) => await _viewModel.UnstageSelectedCommand.ExecuteAsync(item));
        else
            menu.Items.Add("Stage", null, async (_, _) => await _viewModel.StageSelectedCommand.ExecuteAsync(item));

        string discardText = item.Kind == GitChangeKind.Untracked ? "Delete" : "Discard Changes";
        menu.Items.Add(discardText, null, async (_, _) =>
        {
            var confirm = MessageBox.Show(
                this,
                item.Kind == GitChangeKind.Untracked
                    ? $"Delete untracked file '{item.Path}'?"
                    : $"Discard changes in '{item.Path}'?",
                "Git",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
                await _viewModel.DiscardSelectedCommand.ExecuteAsync(item);
        });
        menu.Items.Add("Add to .gitignore", null, async (_, _) =>
        {
            await _viewModel.AddToGitIgnoreCommand.ExecuteAsync(item);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open", null, (_, _) => _viewModel.RequestOpenFile(item));
        menu.Show(list, e.Location);
    }

    private static void RetrieveCachedItem<T>(RetrieveVirtualItemEventArgs e, T[] cache)
    {
        if (e.ItemIndex >= 0 && e.ItemIndex < cache.Length)
            e.Item = new ListViewItem(cache[e.ItemIndex]?.ToString() ?? string.Empty);
        else
            e.Item = new ListViewItem(string.Empty);
    }

    private void RetrieveCommitItem(RetrieveVirtualItemEventArgs e)
    {
        if (e.ItemIndex < 0 || e.ItemIndex >= _commitsCache.Length)
        {
            e.Item = new ListViewItem(string.Empty);
            return;
        }

        GitCommitItem commit = _commitsCache[e.ItemIndex];
        e.Item = new ListViewItem(commit.DisplayText)
        {
            ToolTipText = commit.TooltipText
        };
        if (!commit.TooltipLoaded)
            _ = _viewModel.EnsureCommitTooltipAsync(commit);
    }

    private static T? GetSelectedItem<T>(ListView list, T[] cache) where T : class
    {
        if (list.SelectedIndices.Count == 0)
            return null;

        int index = list.SelectedIndices[0];
        if (index < 0 || index >= cache.Length)
            return null;

        return cache[index];
    }

    private async Task CreateBranchAsync()
    {
        string? name = PromptText("Create Branch", "Branch name:");
        if (string.IsNullOrWhiteSpace(name))
            return;
        await _viewModel.CreateBranchCommand.ExecuteAsync(name);
    }

    private async Task CheckoutBranchAsync()
    {
        var choices = _viewModel.Branches
            .Where(b => !string.Equals(b, _viewModel.BranchName, StringComparison.Ordinal))
            .ToList();
        if (choices.Count == 0)
        {
            MessageBox.Show(this, "No other local branches to check out.", "Git", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string? selected = PromptChoice("Checkout Branch", "Select branch to check out:", choices);
        if (string.IsNullOrWhiteSpace(selected))
            return;
        await _viewModel.CheckoutBranchCommand.ExecuteAsync(selected);
    }

    private async Task MergeBranchAsync()
    {
        var choices = _viewModel.Branches
            .Where(b => !string.Equals(b, _viewModel.BranchName, StringComparison.Ordinal))
            .ToList();
        if (choices.Count == 0)
        {
            MessageBox.Show(this, "No other local branches to merge.", "Git", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string? selected = PromptChoice("Merge Branch", "Select branch to merge into current:", choices);
        if (string.IsNullOrWhiteSpace(selected))
            return;
        await _viewModel.MergeBranchCommand.ExecuteAsync(selected);
    }

    private string? PromptText(string title, string label)
    {
        int dpi = DeviceDpi;
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = DpiScale.Scale(new Size(360, 130), dpi),
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            AutoScaleMode = AutoScaleMode.Dpi,
            Font = Font
        };
        int pad = DpiScale.Scale(12, dpi);
        int fieldH = Math.Max(DpiScale.Scale(28, dpi), (int)Math.Ceiling(Font.GetHeight()) + DpiScale.Scale(8, dpi));
        int btnW = DpiScale.Scale(75, dpi);
        int btnH = Math.Max(DpiScale.Scale(28, dpi), fieldH);
        var lbl = new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(pad, pad)
        };
        var txt = new TextBox
        {
            Location = new Point(pad, pad + DpiScale.Scale(24, dpi)),
            Width = form.ClientSize.Width - pad * 2,
            Height = fieldH
        };
        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(btnW, btnH),
            Location = new Point(form.ClientSize.Width - pad - btnW * 2 - DpiScale.Scale(8, dpi), form.ClientSize.Height - pad - btnH)
        };
        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(btnW, btnH),
            Location = new Point(form.ClientSize.Width - pad - btnW, form.ClientSize.Height - pad - btnH)
        };
        form.Controls.AddRange([lbl, txt, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
    }

    private string? PromptChoice(string title, string label, IReadOnlyList<string> choices)
    {
        int dpi = DeviceDpi;
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = DpiScale.Scale(new Size(360, 130), dpi),
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            AutoScaleMode = AutoScaleMode.Dpi,
            Font = Font
        };
        int pad = DpiScale.Scale(12, dpi);
        int fieldH = Math.Max(DpiScale.Scale(28, dpi), (int)Math.Ceiling(Font.GetHeight()) + DpiScale.Scale(8, dpi));
        int btnW = DpiScale.Scale(75, dpi);
        int btnH = Math.Max(DpiScale.Scale(28, dpi), fieldH);
        var lbl = new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(pad, pad)
        };
        var cmb = new ComboBox
        {
            Location = new Point(pad, pad + DpiScale.Scale(24, dpi)),
            Width = form.ClientSize.Width - pad * 2,
            Height = fieldH,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (string choice in choices)
            cmb.Items.Add(choice);
        if (cmb.Items.Count > 0)
            cmb.SelectedIndex = 0;
        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(btnW, btnH),
            Location = new Point(form.ClientSize.Width - pad - btnW * 2 - DpiScale.Scale(8, dpi), form.ClientSize.Height - pad - btnH)
        };
        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(btnW, btnH),
            Location = new Point(form.ClientSize.Width - pad - btnW, form.ClientSize.Height - pad - btnH)
        };
        form.Controls.AddRange([lbl, cmb, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog(this) == DialogResult.OK ? cmb.SelectedItem as string : null;
    }

    private void ApplyTheme()
    {
        Color mainBack = _colorTheme?.MainBack ?? SystemColors.Control;
        Color mainFore = _colorTheme?.MainFore ?? SystemColors.ControlText;
        bool dark = _colorTheme?.IsDark(mainBack) ?? false;
        Color panelBack = dark ? ControlPaint.Light(mainBack, 0.03f) : Color.White;
        // Stronger contrast so section headers read as bands at high DPI / light themes.
        Color headerBack = dark
            ? ControlPaint.Light(mainBack, 0.14f)
            : Color.FromArgb(228, 230, 235);
        Color toolbarBack = dark
            ? ControlPaint.Light(mainBack, 0.08f)
            : Color.FromArgb(236, 238, 242);
        Color accent = dark ? Color.FromArgb(55, 120, 200) : Color.FromArgb(0, 120, 212);
        _sectionBorderColor = dark
            ? ControlPaint.Light(mainBack, 0.28f)
            : Color.FromArgb(180, 184, 192);
        _sectionAccentColor = accent;

        BackColor = mainBack;
        ForeColor = mainFore;
        foreach (Control control in GetAllControls(this))
        {
            if (control is Button or TextBox or ListBox or ListView or ComboBox)
            {
                control.BackColor = panelBack;
                control.ForeColor = mainFore;
            }
            else if (control is Panel or TableLayoutPanel or FlowLayoutPanel or SplitContainer or SplitterPanel)
            {
                control.BackColor = mainBack;
                control.ForeColor = mainFore;
            }
            else
            {
                control.BackColor = mainBack;
                control.ForeColor = mainFore;
            }
        }

        _pnlHeader.BackColor = toolbarBack;
        _pnlRepoRow.BackColor = toolbarBack;
        _pnlToolbar.BackColor = toolbarBack;

        foreach (Panel header in SectionHeaderPanels())
        {
            header.BackColor = headerBack;
            header.Invalidate();
        }

        foreach (Label label in SectionHeaderLabels())
        {
            label.BackColor = headerBack;
            label.ForeColor = mainFore;
        }

        EnsureSectionHeaderFont();
        _btnCommit.BackColor = accent;
        _btnCommit.ForeColor = Color.White;
        _btnCommit.FlatStyle = FlatStyle.Flat;
        _btnCommit.FlatAppearance.BorderSize = 0;
        _lblEmpty.ForeColor = mainFore;
        _pnlEmpty.BackColor = mainBack;
    }

    private void SectionHeader_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
            return;

        float dpiScale = Math.Max(1f, DeviceDpi / 96f);
        int accentW = Math.Max(3, (int)Math.Round(3 * dpiScale));
        int borderH = Math.Max(1, (int)Math.Round(1 * dpiScale));

        using (var accentBrush = new SolidBrush(_sectionAccentColor))
            e.Graphics.FillRectangle(accentBrush, 0, 0, accentW, panel.Height);

        using (var borderPen = new Pen(_sectionBorderColor, borderH))
        {
            int y = panel.Height - borderH;
            e.Graphics.DrawLine(borderPen, 0, y, panel.Width, y);
        }
    }

    private static IEnumerable<Control> GetAllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in GetAllControls(child))
                yield return nested;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_previewDebounceTimer is not null)
            {
                _previewDebounceTimer.Stop();
                _previewDebounceTimer.Tick -= PreviewDebounceTimer_Tick;
                _previewDebounceTimer.Dispose();
                _previewDebounceTimer = null;
            }

            foreach (Panel header in SectionHeaderPanels())
                header.Paint -= SectionHeader_Paint;

            DisposeSectionHeaderFont();

            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.OpenFileRequested -= _openFileHandler;
                _viewModel.DiffPreviewRequested -= _diffPreviewHandler;
                _viewModel.ErrorOccurred -= _errorHandler;
            }
            DisposeBranchFont();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
