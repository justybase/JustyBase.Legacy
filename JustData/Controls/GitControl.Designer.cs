namespace JustyBaseLegacy.UI.Controls;

#nullable enable
partial class GitControl
{
    private System.ComponentModel.IContainer? components = null;

    private System.Windows.Forms.TableLayoutPanel _pnlHeader = null!;
    private System.Windows.Forms.TableLayoutPanel _pnlRepoRow = null!;
    private System.Windows.Forms.ComboBox _cmbRepos = null!;
    private System.Windows.Forms.Button _btnOpenRepo = null!;
    private System.Windows.Forms.Button _btnRefresh = null!;
    private System.Windows.Forms.Label _lblBranch = null!;
    private System.Windows.Forms.FlowLayoutPanel _pnlToolbar = null!;
    private System.Windows.Forms.Button _btnPull = null!;
    private System.Windows.Forms.Button _btnPush = null!;
    private System.Windows.Forms.Button _btnSync = null!;
    private System.Windows.Forms.Button _btnStageAll = null!;
    private System.Windows.Forms.Button _btnCreateBranch = null!;
    private System.Windows.Forms.Button _btnMergeBranch = null!;
    private System.Windows.Forms.Button _btnMore = null!;
    private System.Windows.Forms.ContextMenuStrip _menuMore = null!;
    private System.Windows.Forms.Label _lblStatus = null!;
    private System.Windows.Forms.Label _lblIdentity = null!;
    private System.Windows.Forms.TextBox _txtUserName = null!;
    private System.Windows.Forms.TextBox _txtUserEmail = null!;
    private System.Windows.Forms.Button _btnSaveIdentity = null!;
    private System.Windows.Forms.TableLayoutPanel _pnlIdentity = null!;
    private System.Windows.Forms.Panel _pnlEmpty = null!;
    private System.Windows.Forms.Label _lblEmpty = null!;
    private System.Windows.Forms.SplitContainer _splitMain = null!;
    private System.Windows.Forms.SplitContainer _splitBottom = null!;
    private System.Windows.Forms.Panel _pnlChanges = null!;
    private System.Windows.Forms.Panel _pnlChangesHeader = null!;
    private System.Windows.Forms.Label _lblChangesHeader = null!;
    private System.Windows.Forms.TextBox _txtCommitMessage = null!;
    private System.Windows.Forms.Button _btnCommit = null!;
    private System.Windows.Forms.Button _btnStageAllCommit = null!;
    private System.Windows.Forms.Button _btnGenerateCommit = null!;
    private System.Windows.Forms.FlowLayoutPanel _pnlCommitActions = null!;
    private System.Windows.Forms.SplitContainer _splitChangesLists = null!;
    private System.Windows.Forms.Panel _pnlStaged = null!;
    private System.Windows.Forms.Panel _pnlStagedHeader = null!;
    private System.Windows.Forms.Label _lblStagedHeader = null!;
    private System.Windows.Forms.ListView _lvStaged = null!;
    private System.Windows.Forms.Panel _pnlUnstaged = null!;
    private System.Windows.Forms.Panel _pnlUnstagedHeader = null!;
    private System.Windows.Forms.Label _lblUnstagedHeader = null!;
    private System.Windows.Forms.ListView _lvUnstaged = null!;
    private System.Windows.Forms.Panel _pnlCommits = null!;
    private System.Windows.Forms.Panel _pnlCommitsHeader = null!;
    private System.Windows.Forms.Label _lblCommitsHeader = null!;
    private System.Windows.Forms.SplitContainer _splitCommits = null!;
    private System.Windows.Forms.ListView _lvCommits = null!;
    private System.Windows.Forms.Panel _pnlCommitFiles = null!;
    private System.Windows.Forms.Panel _pnlCommitFilesHeader = null!;
    private System.Windows.Forms.Label _lblCommitFilesHeader = null!;
    private System.Windows.Forms.ListView _lvCommitFiles = null!;
    private System.Windows.Forms.Panel _pnlTimeline = null!;
    private System.Windows.Forms.Panel _pnlTimelineHeader = null!;
    private System.Windows.Forms.Label _lblTimelineHeader = null!;
    private System.Windows.Forms.ListView _lvTimeline = null!;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _pnlHeader = new System.Windows.Forms.TableLayoutPanel();
        _pnlRepoRow = new System.Windows.Forms.TableLayoutPanel();
        _cmbRepos = new System.Windows.Forms.ComboBox();
        _btnOpenRepo = new System.Windows.Forms.Button();
        _btnRefresh = new System.Windows.Forms.Button();
        _lblBranch = new System.Windows.Forms.Label();
        _pnlToolbar = new System.Windows.Forms.FlowLayoutPanel();
        _btnPull = new System.Windows.Forms.Button();
        _btnPush = new System.Windows.Forms.Button();
        _btnSync = new System.Windows.Forms.Button();
        _btnStageAll = new System.Windows.Forms.Button();
        _btnCreateBranch = new System.Windows.Forms.Button();
        _btnMergeBranch = new System.Windows.Forms.Button();
        _btnMore = new System.Windows.Forms.Button();
        _menuMore = new System.Windows.Forms.ContextMenuStrip(components);
        _lblStatus = new System.Windows.Forms.Label();
        _lblIdentity = new System.Windows.Forms.Label();
        _txtUserName = new System.Windows.Forms.TextBox();
        _txtUserEmail = new System.Windows.Forms.TextBox();
        _btnSaveIdentity = new System.Windows.Forms.Button();
        _pnlIdentity = new System.Windows.Forms.TableLayoutPanel();
        _pnlEmpty = new System.Windows.Forms.Panel();
        _lblEmpty = new System.Windows.Forms.Label();
        _splitMain = new System.Windows.Forms.SplitContainer();
        _splitBottom = new System.Windows.Forms.SplitContainer();
        _pnlChanges = new System.Windows.Forms.Panel();
        _pnlChangesHeader = new System.Windows.Forms.Panel();
        _lblChangesHeader = new System.Windows.Forms.Label();
        _txtCommitMessage = new System.Windows.Forms.TextBox();
        _btnCommit = new System.Windows.Forms.Button();
        _btnStageAllCommit = new System.Windows.Forms.Button();
        _btnGenerateCommit = new System.Windows.Forms.Button();
        _pnlCommitActions = new System.Windows.Forms.FlowLayoutPanel();
        _splitChangesLists = new System.Windows.Forms.SplitContainer();
        _pnlStaged = new System.Windows.Forms.Panel();
        _pnlStagedHeader = new System.Windows.Forms.Panel();
        _lblStagedHeader = new System.Windows.Forms.Label();
        _lvStaged = CreateVirtualListView();
        _pnlUnstaged = new System.Windows.Forms.Panel();
        _pnlUnstagedHeader = new System.Windows.Forms.Panel();
        _lblUnstagedHeader = new System.Windows.Forms.Label();
        _lvUnstaged = CreateVirtualListView();
        _pnlCommits = new System.Windows.Forms.Panel();
        _pnlCommitsHeader = new System.Windows.Forms.Panel();
        _lblCommitsHeader = new System.Windows.Forms.Label();
        _splitCommits = new System.Windows.Forms.SplitContainer();
        _lvCommits = CreateVirtualListView();
        _pnlCommitFiles = new System.Windows.Forms.Panel();
        _pnlCommitFilesHeader = new System.Windows.Forms.Panel();
        _lblCommitFilesHeader = new System.Windows.Forms.Label();
        _lvCommitFiles = CreateVirtualListView();
        _pnlTimeline = new System.Windows.Forms.Panel();
        _pnlTimelineHeader = new System.Windows.Forms.Panel();
        _lblTimelineHeader = new System.Windows.Forms.Label();
        _lvTimeline = CreateVirtualListView();

        ((System.ComponentModel.ISupportInitialize)_splitMain).BeginInit();
        _splitMain.Panel1.SuspendLayout();
        _splitMain.Panel2.SuspendLayout();
        _splitMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_splitBottom).BeginInit();
        _splitBottom.Panel1.SuspendLayout();
        _splitBottom.Panel2.SuspendLayout();
        _splitBottom.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_splitChangesLists).BeginInit();
        _splitChangesLists.Panel1.SuspendLayout();
        _splitChangesLists.Panel2.SuspendLayout();
        _splitChangesLists.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_splitCommits).BeginInit();
        _splitCommits.Panel1.SuspendLayout();
        _splitCommits.Panel2.SuspendLayout();
        _splitCommits.SuspendLayout();
        _pnlHeader.SuspendLayout();
        _pnlRepoRow.SuspendLayout();
        _pnlToolbar.SuspendLayout();
        _pnlEmpty.SuspendLayout();
        _pnlChanges.SuspendLayout();
        _pnlChangesHeader.SuspendLayout();
        _pnlStaged.SuspendLayout();
        _pnlStagedHeader.SuspendLayout();
        _pnlUnstaged.SuspendLayout();
        _pnlUnstagedHeader.SuspendLayout();
        _pnlCommits.SuspendLayout();
        _pnlCommitsHeader.SuspendLayout();
        _pnlCommitFiles.SuspendLayout();
        _pnlCommitFilesHeader.SuspendLayout();
        _pnlTimeline.SuspendLayout();
        _pnlTimelineHeader.SuspendLayout();
        SuspendLayout();

        // Repo row
        _pnlRepoRow.ColumnCount = 3;
        _pnlRepoRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _pnlRepoRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlRepoRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlRepoRow.RowCount = 1;
        _pnlRepoRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlRepoRow.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlRepoRow.Margin = new System.Windows.Forms.Padding(0);
        _pnlRepoRow.Controls.Add(_cmbRepos, 0, 0);
        _pnlRepoRow.Controls.Add(_btnOpenRepo, 1, 0);
        _pnlRepoRow.Controls.Add(_btnRefresh, 2, 0);

        _cmbRepos.Dock = System.Windows.Forms.DockStyle.Fill;
        _cmbRepos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _cmbRepos.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);

        _btnOpenRepo.Text = "Open";
        _btnOpenRepo.AutoSize = true;
        _btnOpenRepo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        _btnOpenRepo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        _btnOpenRepo.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
        _btnOpenRepo.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);

        _btnRefresh.Text = "↻";
        _btnRefresh.AutoSize = true;
        _btnRefresh.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        _btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        _btnRefresh.Margin = new System.Windows.Forms.Padding(0);
        _btnRefresh.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);

        _lblBranch.AutoSize = true;
        _lblBranch.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblBranch.Text = "—";
        _lblBranch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblBranch.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
        _lblBranch.UseMnemonic = false;

        _pnlToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlToolbar.WrapContents = false;
        _pnlToolbar.AutoSize = true;
        _pnlToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        _pnlToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
        _pnlToolbar.Margin = new System.Windows.Forms.Padding(0);
        _pnlToolbar.Controls.Add(_btnPull);
        _pnlToolbar.Controls.Add(_btnPush);
        _pnlToolbar.Controls.Add(_btnSync);
        _pnlToolbar.Controls.Add(_btnStageAll);
        _pnlToolbar.Controls.Add(_btnMore);

        ConfigureToolButton(_btnPull, "Pull");
        ConfigureToolButton(_btnPush, "Push");
        ConfigureToolButton(_btnSync, "Sync");
        ConfigureToolButton(_btnStageAll, "Stage All");
        ConfigureToolButton(_btnMore, "More ▾");
        ConfigureToolButton(_btnCreateBranch, "Branch");
        ConfigureToolButton(_btnMergeBranch, "Merge");

        _menuMore.Items.Add("Create Branch…", null, async (_, _) => await CreateBranchAsync());
        _menuMore.Items.Add("Checkout Branch…", null, async (_, _) => await CheckoutBranchAsync());
        _menuMore.Items.Add("Merge Branch…", null, async (_, _) => await MergeBranchAsync());

        _lblStatus.AutoSize = true;
        _lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblStatus.UseMnemonic = false;

        _pnlIdentity.ColumnCount = 3;
        _pnlIdentity.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        _pnlIdentity.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        _pnlIdentity.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlIdentity.RowCount = 2;
        _pnlIdentity.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlIdentity.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlIdentity.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlIdentity.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
        _txtUserName.PlaceholderText = "user.name";
        _txtUserName.Dock = System.Windows.Forms.DockStyle.Fill;
        _txtUserEmail.PlaceholderText = "user.email";
        _txtUserEmail.Dock = System.Windows.Forms.DockStyle.Fill;
        ConfigureToolButton(_btnSaveIdentity, "Save ID");
        _lblIdentity.AutoSize = false;
        _lblIdentity.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblIdentity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblIdentity.UseMnemonic = false;
        _lblIdentity.AutoEllipsis = true;
        _pnlIdentity.Controls.Add(_txtUserName, 0, 0);
        _pnlIdentity.Controls.Add(_txtUserEmail, 1, 0);
        _pnlIdentity.Controls.Add(_btnSaveIdentity, 2, 0);
        _pnlIdentity.Controls.Add(_lblIdentity, 0, 1);
        _pnlIdentity.SetColumnSpan(_lblIdentity, 3);

        _pnlHeader.ColumnCount = 1;
        _pnlHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _pnlHeader.RowCount = 5;
        _pnlHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _pnlHeader.AutoSize = true;
        _pnlHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        _pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
        _pnlHeader.Padding = new System.Windows.Forms.Padding(6);
        _pnlHeader.Controls.Add(_pnlRepoRow, 0, 0);
        _pnlHeader.Controls.Add(_lblBranch, 0, 1);
        _pnlHeader.Controls.Add(_pnlToolbar, 0, 2);
        _pnlHeader.Controls.Add(_pnlIdentity, 0, 3);
        _pnlHeader.Controls.Add(_lblStatus, 0, 4);

        _pnlEmpty.Controls.Add(_lblEmpty);
        _pnlEmpty.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblEmpty.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        _lblEmpty.Padding = new System.Windows.Forms.Padding(16);

        // Changes section
        _pnlChangesHeader.Controls.Add(_lblChangesHeader);
        _pnlChangesHeader.Dock = System.Windows.Forms.DockStyle.Top;
        _lblChangesHeader.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblChangesHeader.Text = "SOURCE CONTROL";
        _lblChangesHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblChangesHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);

        _btnCommit.Text = "✓ Commit";
        _btnCommit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        _btnCommit.AutoSize = true;
        _btnStageAllCommit.Text = "Stage All & Commit";
        _btnStageAllCommit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        _btnStageAllCommit.AutoSize = true;
        _btnGenerateCommit.Text = "✨";
        _btnGenerateCommit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        _btnGenerateCommit.AutoSize = true;
        _btnGenerateCommit.Visible = false;
        _pnlCommitActions.Dock = System.Windows.Forms.DockStyle.Top;
        _pnlCommitActions.AutoSize = true;
        _pnlCommitActions.WrapContents = false;
        _pnlCommitActions.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
        _pnlCommitActions.Controls.Add(_btnCommit);
        _pnlCommitActions.Controls.Add(_btnStageAllCommit);
        _pnlCommitActions.Controls.Add(_btnGenerateCommit);

        _txtCommitMessage.Dock = System.Windows.Forms.DockStyle.Top;
        _txtCommitMessage.Multiline = true;
        _txtCommitMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        _txtCommitMessage.PlaceholderText = "Message (Ctrl+Enter to commit)";

        _pnlStagedHeader.Controls.Add(_lblStagedHeader);
        _pnlStagedHeader.Dock = System.Windows.Forms.DockStyle.Top;
        _lblStagedHeader.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblStagedHeader.Text = "STAGED";
        _lblStagedHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblStagedHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
        _lvStaged.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlStaged.Controls.Add(_lvStaged);
        _pnlStaged.Controls.Add(_pnlStagedHeader);
        _pnlStaged.Dock = System.Windows.Forms.DockStyle.Fill;

        _pnlUnstagedHeader.Controls.Add(_lblUnstagedHeader);
        _pnlUnstagedHeader.Dock = System.Windows.Forms.DockStyle.Top;
        _lblUnstagedHeader.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblUnstagedHeader.Text = "CHANGES";
        _lblUnstagedHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblUnstagedHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
        _lvUnstaged.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlUnstaged.Controls.Add(_lvUnstaged);
        _pnlUnstaged.Controls.Add(_pnlUnstagedHeader);
        _pnlUnstaged.Dock = System.Windows.Forms.DockStyle.Fill;

        _splitChangesLists.Dock = System.Windows.Forms.DockStyle.Fill;
        _splitChangesLists.Orientation = System.Windows.Forms.Orientation.Horizontal;
        _splitChangesLists.Panel1.Controls.Add(_pnlStaged);
        _splitChangesLists.Panel2.Controls.Add(_pnlUnstaged);

        _pnlChanges.Controls.Add(_splitChangesLists);
        _pnlChanges.Controls.Add(_pnlCommitActions);
        _pnlChanges.Controls.Add(_txtCommitMessage);
        _pnlChanges.Controls.Add(_pnlChangesHeader);
        _pnlChanges.Dock = System.Windows.Forms.DockStyle.Fill;

        // Commits + files
        _pnlCommitsHeader.Controls.Add(_lblCommitsHeader);
        _pnlCommitsHeader.Dock = System.Windows.Forms.DockStyle.Top;
        _lblCommitsHeader.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblCommitsHeader.Text = "COMMITS";
        _lblCommitsHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblCommitsHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);

        _pnlCommitFilesHeader.Controls.Add(_lblCommitFilesHeader);
        _pnlCommitFilesHeader.Dock = System.Windows.Forms.DockStyle.Top;
        _lblCommitFilesHeader.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblCommitFilesHeader.Text = "FILES IN COMMIT";
        _lblCommitFilesHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblCommitFilesHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
        _lvCommitFiles.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlCommitFiles.Controls.Add(_lvCommitFiles);
        _pnlCommitFiles.Controls.Add(_pnlCommitFilesHeader);
        _pnlCommitFiles.Dock = System.Windows.Forms.DockStyle.Fill;

        _lvCommits.Dock = System.Windows.Forms.DockStyle.Fill;
        _splitCommits.Dock = System.Windows.Forms.DockStyle.Fill;
        _splitCommits.Orientation = System.Windows.Forms.Orientation.Horizontal;
        _splitCommits.Panel1.Controls.Add(_lvCommits);
        _splitCommits.Panel2.Controls.Add(_pnlCommitFiles);

        _pnlCommits.Controls.Add(_splitCommits);
        _pnlCommits.Controls.Add(_pnlCommitsHeader);
        _pnlCommits.Dock = System.Windows.Forms.DockStyle.Fill;

        // Timeline
        _pnlTimelineHeader.Controls.Add(_lblTimelineHeader);
        _pnlTimelineHeader.Dock = System.Windows.Forms.DockStyle.Top;
        _lblTimelineHeader.Dock = System.Windows.Forms.DockStyle.Fill;
        _lblTimelineHeader.Text = "TIMELINE";
        _lblTimelineHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _lblTimelineHeader.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
        _lvTimeline.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlTimeline.Controls.Add(_lvTimeline);
        _pnlTimeline.Controls.Add(_pnlTimelineHeader);
        _pnlTimeline.Dock = System.Windows.Forms.DockStyle.Fill;
        _pnlTimeline.Visible = false;

        _splitBottom.Dock = System.Windows.Forms.DockStyle.Fill;
        _splitBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
        _splitBottom.Panel1.Controls.Add(_pnlCommits);
        _splitBottom.Panel2.Controls.Add(_pnlTimeline);

        _splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
        _splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
        _splitMain.Panel1.Controls.Add(_pnlChanges);
        _splitMain.Panel2.Controls.Add(_splitBottom);

        Controls.Add(_splitMain);
        Controls.Add(_pnlEmpty);
        Controls.Add(_pnlHeader);
        Name = "GitControl";
        Size = new System.Drawing.Size(300, 600);
        AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

        _splitMain.Panel1.ResumeLayout(false);
        _splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_splitMain).EndInit();
        _splitMain.ResumeLayout(false);
        _splitBottom.Panel1.ResumeLayout(false);
        _splitBottom.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_splitBottom).EndInit();
        _splitBottom.ResumeLayout(false);
        _splitChangesLists.Panel1.ResumeLayout(false);
        _splitChangesLists.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_splitChangesLists).EndInit();
        _splitChangesLists.ResumeLayout(false);
        _splitCommits.Panel1.ResumeLayout(false);
        _splitCommits.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_splitCommits).EndInit();
        _splitCommits.ResumeLayout(false);
        _pnlHeader.ResumeLayout(false);
        _pnlHeader.PerformLayout();
        _pnlRepoRow.ResumeLayout(false);
        _pnlRepoRow.PerformLayout();
        _pnlToolbar.ResumeLayout(false);
        _pnlToolbar.PerformLayout();
        _pnlEmpty.ResumeLayout(false);
        _pnlChanges.ResumeLayout(false);
        _pnlChangesHeader.ResumeLayout(false);
        _pnlStaged.ResumeLayout(false);
        _pnlStagedHeader.ResumeLayout(false);
        _pnlUnstaged.ResumeLayout(false);
        _pnlUnstagedHeader.ResumeLayout(false);
        _pnlCommits.ResumeLayout(false);
        _pnlCommitsHeader.ResumeLayout(false);
        _pnlCommitFiles.ResumeLayout(false);
        _pnlCommitFilesHeader.ResumeLayout(false);
        _pnlTimeline.ResumeLayout(false);
        _pnlTimelineHeader.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }        private static System.Windows.Forms.ListView CreateVirtualListView()
    {
        var list = new System.Windows.Forms.ListView
        {
            View = System.Windows.Forms.View.Details,
            HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None,
            FullRowSelect = true,
            MultiSelect = false,
            VirtualMode = true,
            Scrollable = true,
            HideSelection = false,
            BorderStyle = System.Windows.Forms.BorderStyle.None,
            UseCompatibleStateImageBehavior = false
        };
        list.Columns.Add(string.Empty, -2);
        return list;
    }

    private static void ConfigureToolButton(System.Windows.Forms.Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
        button.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
        button.MinimumSize = new System.Drawing.Size(0, 0);
    }
}
