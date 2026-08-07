using AppBase.Common;
using DatabaseDataGridView.WinForms.Coloring;
using JustyBase.Ai.Git;
using JustyBase.Core.Git;
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
    private readonly Action<JustyBase.Core.Git.GitFileContents> _diffPreviewHandler;
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
        EnableDoubleBuffering();
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
        EnableDoubleBuffering();
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
            // Identity label is hidden; skip row height override.

            _cmbRepos.MinimumSize = new Size(0, buttonH);
            _cmbRepos.MaximumSize = new Size(0, buttonH + DpiScale.Scale(4, dpi));
            foreach (Button button in new[] { _btnOpenRepo, _btnRefresh, _btnPull, _btnPush, _btnSync, _btnMore })
            {
                button.MinimumSize = new Size(0, buttonH);
                button.Padding = new Padding(DpiScale.Scale(6, dpi), DpiScale.Scale(2, dpi), DpiScale.Scale(6, dpi), DpiScale.Scale(2, dpi));
                button.Margin = new Padding(0, 0, gap, gap);
            }

            // Commit action buttons: uniform height, compact width
            int commitBtnRowH = Math.Max(DpiScale.Scale(24, dpi), (int)Math.Ceiling(fontHeight) + DpiScale.Scale(4, dpi));
            foreach (Button button in new[] { _btnGenerateCommit, _btnCommit })
            {
                button.Height = commitBtnRowH;
                button.MinimumSize = new Size(0, commitBtnRowH);
                button.MaximumSize = new Size(0, commitBtnRowH);
                button.Padding = new Padding(DpiScale.Scale(6, dpi), 0, DpiScale.Scale(6, dpi), 0);
                button.Margin = new Padding(0, 0, gap, 0);
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
        _actionBtnFont?.Dispose();
        _actionBtnFont = null;
        _statusBadgeFont?.Dispose();
        _statusBadgeFont = null;
        _fileIconFont?.Dispose();
        _fileIconFont = null;
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
        _btnStageAllChanges.Click += async (_, _) => await _viewModel.StageAllCommand.ExecuteAsync(null);
        _btnUnstageAllChanges.Click += async (_, _) => await _viewModel.UnstageAllCommand.ExecuteAsync(null);
        _btnCommit.Click += async (_, _) => await _viewModel.CommitCommand.ExecuteAsync(null);
        _btnGenerateCommit.Click += async (_, _) => await _viewModel.GenerateCommitMessageCommand.ExecuteAsync(null);
        _btnMore.Click += (_, _) =>
        {
            _menuMore.Show(_btnMore, new Point(0, _btnMore.Height));
        };


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

        foreach (ListView list in new[] { _lvCommits, _lvTimeline })
        {
            SizeListViewColumn(list);
            list.SizeChanged += (_, _) => SizeListViewColumn(list);
        }

        // Enable double-buffering on ListViews to eliminate flicker.
        SetDoubleBuffered(_lvStaged);
        SetDoubleBuffered(_lvUnstaged);
        SetDoubleBuffered(_lvCommitFiles);

        // Owner-draw lists (staged/unstaged get VSCode-style; commit-files get colored status)
        _lvStaged.OwnerDraw = true;
        _lvStaged.DrawColumnHeader += (_, _) => { };
        _lvStaged.DrawSubItem += (_, e) => DrawChangeItemWithAction(e!, isStaged: true);
        _lvStaged.MouseUp += ChangesList_MouseUp_Actions;
        _lvStaged.MouseMove += (_, e) => TrackHover(_lvStaged, e, isStaged: true);
        _lvStaged.MouseLeave += (_, _) => { _hoveredStagedIndex = -1; _lvStaged.Invalidate(); };
        _lvStaged.SizeChanged += (_, _) => SizeListViewColumn(_lvStaged);
        _lvUnstaged.OwnerDraw = true;
        _lvUnstaged.DrawColumnHeader += (_, _) => { };
        _lvUnstaged.DrawSubItem += (_, e) => DrawChangeItemWithAction(e!, isStaged: false);
        _lvUnstaged.MouseUp += ChangesList_MouseUp_Actions;
        _lvUnstaged.MouseMove += (_, e) => TrackHover(_lvUnstaged, e, isStaged: false);
        _lvUnstaged.MouseLeave += (_, _) => { _hoveredUnstagedIndex = -1; _lvUnstaged.Invalidate(); };
        _lvUnstaged.SizeChanged += (_, _) => SizeListViewColumn(_lvUnstaged);
        _lvCommitFiles.OwnerDraw = true;
        _lvCommitFiles.DrawColumnHeader += (_, _) => { };
        _lvCommitFiles.DrawSubItem += (_, e) => DrawCommitFileItem(e!);
        _lvCommitFiles.MouseMove += (_, e) =>
        {
            ListViewItem? hit = _lvCommitFiles.GetItemAt(e.X, e.Y);
            int newIndex = hit?.Index ?? -1;
            int prevIndex = _hoveredCommitFilesIndex;
            if (newIndex == prevIndex) return;
            _hoveredCommitFilesIndex = newIndex;
            int lo = Math.Min(prevIndex, newIndex);
            int hi = Math.Max(prevIndex, newIndex);
            if (lo >= 0 && hi < _lvCommitFiles.VirtualListSize)
                _lvCommitFiles.RedrawItems(lo, hi, false);
            else if (newIndex >= 0 && newIndex < _lvCommitFiles.VirtualListSize)
                _lvCommitFiles.RedrawItems(newIndex, newIndex, false);
        };
        _lvCommitFiles.MouseLeave += (_, _) => { _hoveredCommitFilesIndex = -1; _lvCommitFiles.Invalidate(); };
        _lvCommitFiles.SizeChanged += (_, _) => SizeListViewColumn(_lvCommitFiles);
        SizeListViewColumn(_lvStaged);
        SizeListViewColumn(_lvUnstaged);
        SizeListViewColumn(_lvCommitFiles);

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
        _btnStageAllChanges.Enabled = enabled;
        _btnUnstageAllChanges.Enabled = enabled;
        _btnCommit.Enabled = enabled;
        _btnGenerateCommit.Enabled = enabled && !_viewModel.IsGeneratingCommitMessage;
        _btnMore.Enabled = enabled;

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
        _lblCommitFilesHeader.Text = _commitFilesCache.Length > 0
            ? $"FILES IN COMMIT ({_commitFilesCache.Length})"
            : "FILES IN COMMIT";

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
        // Owner-drawn lists still need a valid ListViewItem for selection/highlighting.
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

    private void ShowIdentityDialog()
    {
        int dpi = DeviceDpi;
        using var form = new Form
        {
            Text = "Set Git Identity",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = DpiScale.Scale(new Size(400, 150), dpi),
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
        int labelW = DpiScale.Scale(80, dpi);

        var lblName = new Label { Text = "Name:", AutoSize = false, Width = labelW, Height = fieldH, Location = new Point(pad, pad), TextAlign = ContentAlignment.MiddleLeft };
        var txtName = new TextBox { Location = new Point(pad + labelW + DpiScale.Scale(4, dpi), pad), Width = form.ClientSize.Width - pad * 3 - labelW, Height = fieldH, Text = _viewModel.LocalUserName ?? string.Empty };
        var lblEmail = new Label { Text = "Email:", AutoSize = false, Width = labelW, Height = fieldH, Location = new Point(pad, pad + fieldH + DpiScale.Scale(8, dpi)), TextAlign = ContentAlignment.MiddleLeft };
        var txtEmail = new TextBox { Location = new Point(pad + labelW + DpiScale.Scale(4, dpi), pad + fieldH + DpiScale.Scale(8, dpi)), Width = form.ClientSize.Width - pad * 3 - labelW, Height = fieldH, Text = _viewModel.LocalUserEmail ?? string.Empty };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Size = new Size(btnW, btnH), Location = new Point(form.ClientSize.Width - pad - btnW * 2 - DpiScale.Scale(8, dpi), form.ClientSize.Height - pad - btnH) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(btnW, btnH), Location = new Point(form.ClientSize.Width - pad - btnW, form.ClientSize.Height - pad - btnH) };
        form.Controls.AddRange([lblName, txtName, lblEmail, txtEmail, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _viewModel.LocalUserName = txtName.Text;
            _viewModel.LocalUserEmail = txtEmail.Text;
            _ = _viewModel.SaveLocalIdentityCommand.ExecuteAsync(null);
        }
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

    // ── VSCode-style owner-draw for staged/unstaged items ──────────

    private const int FileIconSize = 18;
    private const int SmallActionBtnSize = 22;
    private const int StatusBadgeWidth = 32;
    private Font? _actionBtnFont;
    private Font? _statusBadgeFont;
    private Font? _fileIconFont;

    // Hovered item tracking for action buttons
    private int _hoveredStagedIndex = -1;
    private int _hoveredUnstagedIndex = -1;
    private int _hoveredCommitFilesIndex = -1;

    private void DrawChangeItemWithAction(DrawListViewSubItemEventArgs e, bool isStaged)
    {
        if (e.ItemIndex < 0) return;

        var cache = isStaged ? _stagedCache : _unstagedCache;
        if (e.ItemIndex >= cache.Length) return;
        GitFileStatusItem item = cache[e.ItemIndex];

        bool selected = (e.ItemState & ListViewItemStates.Selected) != 0;
        bool hovered = isStaged ? e.ItemIndex == _hoveredStagedIndex : e.ItemIndex == _hoveredUnstagedIndex;
        bool showActions = hovered || selected;
        Color back = selected ? SystemColors.Highlight : hovered ? ControlPaint.Light(BackColor, 0.92f) : BackColor;
        Color fore = selected ? SystemColors.HighlightText : ForeColor;

        int dpi = DeviceDpi;
        int pad = DpiScale.Scale(4, dpi);
        int iconSize = Math.Max(FileIconSize, (int)(Font.GetHeight() + 2));
        int rowH = e.Bounds.Height;

        // 1. Background
        using (var bgBrush = new SolidBrush(back))
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

        // 2. File type icon (left)
        int iconX = e.Bounds.Left + pad;
        int iconY = e.Bounds.Top + (rowH - iconSize) / 2;
        DrawFileTypeIcon(e.Graphics, item.Path, iconX, iconY, iconSize);

        // 3. Status letter (far right)
        string statusCode = item.StatusCode?.Trim() ?? "?";
        Color statusColor = GetStatusColor(statusCode);
        int statusX = e.Bounds.Right - StatusBadgeWidth - pad;

        EnsureStatusBadgeFont();
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (var statusBrush = new SolidBrush(statusColor))
        {
            e.Graphics.DrawString(statusCode, _statusBadgeFont!, statusBrush, new RectangleF(statusX, e.Bounds.Top, StatusBadgeWidth, rowH), sf);
        }

        // 4. Action buttons (when hovered/selected)
        int actionAreaRight = statusX - pad;
        if (showActions)
        {
            int btnSize = Math.Max(SmallActionBtnSize, (int)(Font.GetHeight() + 6));
            int btnY = e.Bounds.Top + (rowH - btnSize) / 2;

            // Stage/Unstage button (rightmost action)
            int stageBtnX = actionAreaRight - btnSize;
            string stageSymbol = isStaged ? "\u2190" : "+";
            Color stageColor = isStaged ? Color.FromArgb(180, 100, 100) : Color.FromArgb(50, 160, 50);
            DrawSmallActionButton(e.Graphics, stageSymbol, stageBtnX, btnY, btnSize, stageColor, selected);
            actionAreaRight = stageBtnX - DpiScale.Scale(3, dpi);

            // Discard button
            int discardBtnX = actionAreaRight - btnSize;
            DrawSmallActionButton(e.Graphics, "\u2715", discardBtnX, btnY, btnSize, Color.FromArgb(200, 70, 70), selected);
            actionAreaRight = discardBtnX - DpiScale.Scale(3, dpi);
        }

        // 5. Filename (between icon and actions)
        int textX = iconX + iconSize + DpiScale.Scale(6, dpi);
        int textWidth = actionAreaRight - textX;
        if (textWidth > 0)
        {
            string displayPath = string.IsNullOrEmpty(item.OriginalPath) ? item.Path : item.OriginalPath;
            TextRenderer.DrawText(e.Graphics, displayPath, Font, new Rectangle(textX, e.Bounds.Top, textWidth, rowH), fore, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private void DrawFileTypeIcon(Graphics g, string path, int x, int y, int size)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        (Color bg, string symbol) = ext switch
        {
            ".sql"  => (Color.FromArgb(50, 120, 200), "S"),
            ".json" => (Color.FromArgb(200, 160, 40), "{}"),
            ".xml"  => (Color.FromArgb(100, 180, 100), "<>"),
            ".cs"   => (Color.FromArgb(120, 80, 180), "C#"),
            ".config" => (Color.FromArgb(160, 160, 160), "CFG"),
            ".gitignore" => (Color.FromArgb(200, 80, 80), "GI"),
            _       => (Color.FromArgb(100, 140, 180), ext.Length > 0 ? ext[1..].ToUpperInvariant() : "?")
        };

        // Rounded rectangle background
        var prevSmoothing = g.SmoothingMode;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var bgBrush = new SolidBrush(bg))
        {
            var rect = new Rectangle(x, y, size, size);
            int radius = Math.Max(3, size / 5);
            using var rrect = GetRoundedRect(rect, radius);
            g.FillPath(bgBrush, rrect);
        }
        g.SmoothingMode = prevSmoothing;

        // Symbol text
        EnsureFileIconFont();
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (var foreBrush = new SolidBrush(Color.White))
        {
            g.DrawString(symbol, _fileIconFont!, foreBrush, new RectangleF(x, y, size, size), sf);
        }
    }

    private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void DrawSmallActionButton(Graphics g, string symbol, int x, int y, int size, Color color, bool onDarkBackground)
    {
        var prevSmoothing = g.SmoothingMode;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // More opaque circular background for better visibility
        int bgAlpha = onDarkBackground ? 120 : 60;
        Color bgColor = Color.FromArgb(bgAlpha, onDarkBackground ? Color.White : color);
        using (var bgBrush = new SolidBrush(bgColor))
        {
            g.FillEllipse(bgBrush, x, y, size, size);
        }

        // Symbol with high contrast
        Color symColor = onDarkBackground ? color : Color.FromArgb(220, color);
        EnsureActionFonts();
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (var foreBrush = new SolidBrush(symColor))
        {
            g.DrawString(symbol, _actionBtnFont!, foreBrush, new RectangleF(x, y, size, size), sf);
        }

        g.SmoothingMode = prevSmoothing;
    }

    private void ChangesList_MouseUp_Actions(object? sender, MouseEventArgs e)
    {
        if (sender is not ListView list || e.Button != MouseButtons.Left) return;

        ListViewItem? hit = list.GetItemAt(e.X, e.Y);
        if (hit is null) return;

        bool isStaged = list == _lvStaged;
        // Action buttons are clickable when hovered or selected
        bool isSelected = list.SelectedIndices.Contains(hit.Index);
        bool isHovered = isStaged ? hit.Index == _hoveredStagedIndex : hit.Index == _hoveredUnstagedIndex;
        if (!isHovered && !isSelected) return;

        var cache = isStaged ? _stagedCache : _unstagedCache;
        if (hit.Index < 0 || hit.Index >= cache.Length) return;
        GitFileStatusItem item = cache[hit.Index];

        int dpi = DeviceDpi;
        int pad = DpiScale.Scale(4, dpi);
        int btnSize = Math.Max(SmallActionBtnSize, (int)(Font.GetHeight() + 6));
        int statusX = list.ClientSize.Width - StatusBadgeWidth - pad;
        int actionAreaRight = statusX - pad;

        // Check stage/unstage button (rightmost)
        int stageBtnX = actionAreaRight - btnSize;
        var stageBtnRect = new Rectangle(stageBtnX, hit.Bounds.Top + (hit.Bounds.Height - btnSize) / 2, btnSize, btnSize);
        if (stageBtnRect.Contains(e.Location))
        {
            if (isStaged)
                _ = _viewModel.UnstageSelectedCommand.ExecuteAsync(item);
            else
                _ = _viewModel.StageSelectedCommand.ExecuteAsync(item);
            return;
        }

        // Check discard button (with confirmation)
        int discardBtnX = stageBtnX - pad - btnSize;
        var discardBtnRect = new Rectangle(discardBtnX, hit.Bounds.Top + (hit.Bounds.Height - btnSize) / 2, btnSize, btnSize);
        if (discardBtnRect.Contains(e.Location))
        {
            string discardText = item.Kind == GitChangeKind.Untracked ? "Delete" : "Discard Changes";
            var confirm = MessageBox.Show(
                list, $"{discardText} '{item.Path}'?",
                "Git", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
                _ = _viewModel.DiscardSelectedCommand.ExecuteAsync(item);
            return;
        }
    }

    // ── Owner-draw: colored status letters for commit files ───────────

    private void DrawCommitFileItem(DrawListViewSubItemEventArgs e)
    {
        if (e.ItemIndex < 0 || e.ItemIndex >= _commitFilesCache.Length) return;
        GitCommitFileItem item = _commitFilesCache[e.ItemIndex];

        bool selected = (e.ItemState & ListViewItemStates.Selected) != 0;
        bool hovered = e.ItemIndex == _hoveredCommitFilesIndex;
        Color back = selected ? SystemColors.Highlight : hovered ? ControlPaint.Light(BackColor, 0.92f) : BackColor;
        Color fore = selected ? SystemColors.HighlightText : ForeColor;

        int dpi = DeviceDpi;
        int pad = DpiScale.Scale(4, dpi);
        int iconSize = Math.Max(FileIconSize, (int)(Font.GetHeight() + 2));
        int rowH = e.Bounds.Height;

        // 1. Background
        using (var bgBrush = new SolidBrush(back))
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

        // 2. File type icon (left)
        int iconX = e.Bounds.Left + pad;
        int iconY = e.Bounds.Top + (rowH - iconSize) / 2;
        DrawFileTypeIcon(e.Graphics, item.Path, iconX, iconY, iconSize);

        // 3. Status letter (far right)
        string statusCode = item.StatusCode?.Trim() ?? "?";
        Color statusColor = GetStatusColor(statusCode);
        int statusX = e.Bounds.Right - StatusBadgeWidth - pad;

        EnsureStatusBadgeFont();
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (var statusBrush = new SolidBrush(statusColor))
        {
            e.Graphics.DrawString(statusCode, _statusBadgeFont!, statusBrush, new RectangleF(statusX, e.Bounds.Top, StatusBadgeWidth, rowH), sf);
        }

        // 4. Filename (between icon and status)
        int textX = iconX + iconSize + DpiScale.Scale(6, dpi);
        int textWidth = statusX - textX - pad;
        if (textWidth > 0)
        {
            string displayPath = string.IsNullOrEmpty(item.OriginalPath) ? item.Path : item.OriginalPath;
            TextRenderer.DrawText(e.Graphics, displayPath, Font, new Rectangle(textX, e.Bounds.Top, textWidth, rowH), fore, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private static Color GetStatusColor(string statusCode)
    {
        if (string.IsNullOrEmpty(statusCode)) return SystemColors.GrayText;
        char c = statusCode[0];
        return c switch
        {
            'A' => Color.FromArgb(70, 180, 70),   // green
            'M' => Color.FromArgb(200, 160, 40),   // orange/gold
            'D' => Color.FromArgb(210, 60, 60),    // red
            'R' => Color.FromArgb(70, 130, 210),   // blue
            'C' => Color.FromArgb(160, 100, 200),  // purple
            'U' => Color.FromArgb(180, 120, 50),   // brown
            '?' => Color.FromArgb(160, 160, 160),  // gray (untracked)
            _   => SystemColors.GrayText
        };
    }

    private void TrackHover(ListView list, MouseEventArgs e, bool isStaged)
    {
        ListViewItem? hit = list.GetItemAt(e.X, e.Y);
        int newIndex = hit?.Index ?? -1;
        int prevIndex = isStaged ? _hoveredStagedIndex : _hoveredUnstagedIndex;
        if (newIndex == prevIndex) return;
        if (isStaged) _hoveredStagedIndex = newIndex; else _hoveredUnstagedIndex = newIndex;
        // Only redraw the two affected items, not the whole list — prevents flicker.
        int lo = Math.Min(prevIndex, newIndex);
        int hi = Math.Max(prevIndex, newIndex);
        if (lo >= 0 && hi < list.VirtualListSize)
            list.RedrawItems(lo, hi, false);
        else if (newIndex >= 0 && newIndex < list.VirtualListSize)
            list.RedrawItems(newIndex, newIndex, false);
    }

    private void EnsureActionFonts()
    {
        float sz = Font.Size * 0.85f;
        if (_actionBtnFont is not null && Math.Abs(_actionBtnFont.Size - sz) < 0.1f)
            return;
        _actionBtnFont?.Dispose();
        _actionBtnFont = new Font(Font.FontFamily, sz, FontStyle.Bold);
    }

    private void EnsureFileIconFont()
    {
        float sz = Font.Size * 0.55f;
        if (_fileIconFont is not null && Math.Abs(_fileIconFont.Size - sz) < 0.1f)
            return;
        _fileIconFont?.Dispose();
        _fileIconFont = new Font(Font.FontFamily, sz, FontStyle.Bold);
    }

    private void EnsureStatusBadgeFont()
    {
        float sz = Font.Size * 0.8f;
        if (_statusBadgeFont is not null && Math.Abs(_statusBadgeFont.Size - sz) < 0.1f)
            return;
        _statusBadgeFont?.Dispose();
        _statusBadgeFont = new Font(Font.FontFamily, sz, FontStyle.Bold);
    }

    private static void SetDoubleBuffered(Control control)
    {
        typeof(Control).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, control, [true]);
    }

    private void EnableDoubleBuffering()
    {
        SetDoubleBuffered(this);
        foreach (Control control in GetAllControls(this))
            SetDoubleBuffered(control);
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
            _actionBtnFont?.Dispose();
            _actionBtnFont = null;
            _statusBadgeFont?.Dispose();
            _statusBadgeFont = null;
            _fileIconFont?.Dispose();
            _fileIconFont = null;
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
