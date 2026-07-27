using JustData.ViewModels.Explorer;
using AppBase.Common;
using AppBase.Services;
using System.Drawing;
using System.Windows.Forms;
using JustData.Application.Schema;

namespace JustyBaseLegacy.UI.Controls;

/// <summary>
/// WinForms adapter for <see cref="DatabaseExplorerViewModel"/>. It owns TreeNode,
/// icons, expansion, context actions, focus, and bulk updates; schema state stays in
/// the VM/repository.
/// </summary>
public sealed class MvvmDatabaseExplorerControl : UserControl
{
    private readonly DatabaseExplorerViewModel _viewModel;
    private readonly TreeView _treeView;
    private readonly TextBox _filterBox;
    private readonly DataGridView _fastBrowser;
    private readonly Panel _filterPanel;
    private readonly Panel _toolbarPanel;
    private readonly Button _addConnectionButton;
    private readonly Button _editConnectionButton;
    private readonly Button _refreshButton;
    private readonly Button _collapseAllButton;
    private readonly ComboBox _databaseComboBox;
    private readonly Label _filterClearLabel;
    private readonly ContextMenuStrip _contextMenu;
    private readonly System.Windows.Forms.Timer _searchTimer;
    private readonly TableLayoutPanel _layout;
    private readonly Dictionary<ExplorerNodeViewModel, TreeNode> _treeNodes = [];
    private static readonly object LoadingMarker = new();

    /// <summary>Raised when the user clicks the "Add Connection" button.</summary>
    public event EventHandler? AddConnectionRequested;
    /// <summary>Raised when the user clicks the "Edit Connection" button.</summary>
    public event EventHandler? EditConnectionRequested;

    public MvvmDatabaseExplorerControl(DatabaseExplorerViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Name = "mvvmDatabaseExplorerControl";
        Dock = DockStyle.Fill;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        // ── Toolbar with connection management and utility buttons ──
        _addConnectionButton = new Button
        {
            Text = "+ Add",
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(6, 0, 6, 0),
            UseVisualStyleBackColor = true,
            Cursor = Cursors.Hand
        };
        _addConnectionButton.Click += (_, _) => AddConnectionRequested?.Invoke(this, EventArgs.Empty);

        _editConnectionButton = new Button
        {
            Text = "⚙ Edit",
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(6, 0, 6, 0),
            UseVisualStyleBackColor = true,
            Cursor = Cursors.Hand
        };
        _editConnectionButton.Click += (_, _) => EditConnectionRequested?.Invoke(this, EventArgs.Empty);

        _refreshButton = new Button
        {
            Text = "↻",
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(6, 0, 6, 0),
            UseVisualStyleBackColor = true,
            Cursor = Cursors.Hand
        };
        _refreshButton.Click += async (_, _) =>
        {
            try
            {
                await RefreshAsync();
            }
            catch (OperationCanceledException)
            {
                // Superseded refresh — ignore.
            }
            catch (Exception ex)
            {
                FileDiagnosticLog.WriteError("Schema refresh button failed", ex);
            }
        };

        _collapseAllButton = new Button
        {
            Text = "⊟",
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(6, 0, 6, 0),
            UseVisualStyleBackColor = true,
            Cursor = Cursors.Hand
        };
        _collapseAllButton.Click += (_, _) => CollapseAllNodes();

        var toolTip = new ToolTip();
        toolTip.SetToolTip(_refreshButton, "Refresh schema (F5)");
        toolTip.SetToolTip(_collapseAllButton, "Collapse all");

        var flowLayout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Padding = new Padding(2, 0, 2, 0),
            WrapContents = false
        };
        flowLayout.Controls.Add(_addConnectionButton);
        flowLayout.Controls.Add(_editConnectionButton);
        flowLayout.Controls.Add(new Label { Text = "|", ForeColor = Color.Gray, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(4, 0, 4, 0) });
        flowLayout.Controls.Add(_refreshButton);
        flowLayout.Controls.Add(_collapseAllButton);

        _toolbarPanel = new Panel
        {
            Name = "connectionToolbarPanel",
            Dock = DockStyle.Top,
            Height = DpiScale.Scale(28, DeviceDpi),
            TabStop = false
        };
        _toolbarPanel.Controls.Add(flowLayout);

        // ── Filter row: database ComboBox + search TextBox ──
        _databaseComboBox = new ComboBox
        {
            Name = "cbWhatDb",
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };
        // Connection selection is managed externally via SwitchConnectionCommand/InitializeAsync

        _filterBox = new TextBox { Name = "tbFastSchemaSearch", Dock = DockStyle.Fill, PlaceholderText = "Filter schema" };
        _filterClearLabel = new Label
        {
            Text = "✕",
            ForeColor = Color.Gray,
            AutoSize = false,
            Size = new Size(18, 18),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Visible = false,
        };
        _filterClearLabel.Click += (_, _) =>
        {
            _filterBox.Clear();
            _filterBox.Focus();
        };
        toolTip.SetToolTip(_filterClearLabel, "Clear filter");

        // Row 3: database combo + search panel (stacked vertically in filterPanel)
        // We keep one filterPanel container but add the combo above the search box.
        var filterTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };
        filterTable.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale.Scale(24, DeviceDpi)));
        filterTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        filterTable.Controls.Add(_databaseComboBox, 0, 0);

        var searchPanel = new Panel { Dock = DockStyle.Fill, Height = 24, Padding = new Padding(0) };
        searchPanel.Controls.Add(_filterBox);
        searchPanel.Controls.Add(_filterClearLabel);

        // Assign _filterPanel BEFORE hooking Resize events and BEFORE
        // adding searchPanel to the tree (which may trigger Resize).
        _filterPanel = new Panel
        {
            Name = "filterPanel",
            Dock = DockStyle.Fill,
            Height = DpiScale.Scale(48, DeviceDpi),
            Padding = new Padding(0, 0, 0, 0)
        };
        _filterPanel.Controls.Add(filterTable);

        searchPanel.Resize += (_, _) => PositionFilterClearLabel();
        filterTable.Controls.Add(searchPanel, 0, 1);
        PositionFilterClearLabel();
        _fastBrowser = new DataGridView
        {
            Name = "dgvFastDbBrowser",
            Dock = DockStyle.Fill,
            Height = 0,
            Visible = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ColumnHeadersVisible = true,
            RowHeadersVisible = false
        };
        _fastBrowser.Columns.Add("Type", "Type");
        _fastBrowser.Columns.Add("Name", "Name");
        _fastBrowser.Columns.Add("Database", "Database");
        _fastBrowser.Columns.Add("Description", "Description");
        _fastBrowser.Columns.Add("Owner", "Owner");
        _fastBrowser.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _fastBrowser.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _fastBrowser.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _fastBrowser.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _fastBrowser.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _treeView = new TreeView
        {
            Name = "databaseTreeView",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0),  // tiny gap from toolbar
            HideSelection = false,
            FullRowSelect = true,
            ShowNodeToolTips = true,
            ShowPlusMinus = true,
            ShowLines = true,
            ShowRootLines = true,
            Indent = 16,
            ItemHeight = 22
        };
        _contextMenu = new ContextMenuStrip();
        var copyDdl = new ToolStripMenuItem("Copy DDL") { Name = "copyDdlMenuItem" };
        copyDdl.Click += async (_, _) => await CopySelectedDdlAsync();
        _contextMenu.Items.Add(copyDdl);
        _treeView.ContextMenuStrip = _contextMenu;
        _searchTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await SearchAsync();
        };

        // Use TableLayoutPanel for precise, overlap-free layout.
        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            // Force 100% width/height so docking resolves correctly.
            Width = 100,
            Height = 100,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Padding = new Padding(0)
        };
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale.Scale(28, DeviceDpi)));  // Row 0: toolbar
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));                              // Row 1: tree (fills remaining)
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));                               // Row 2: fast browser (collapsed until search)
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale.Scale(48, DeviceDpi)));    // Row 3: database combo + search

        _layout.Controls.Add(_toolbarPanel, 0, 0);
        _layout.Controls.Add(_treeView, 0, 1);
        _layout.Controls.Add(_fastBrowser, 0, 2);
        _layout.Controls.Add(_filterPanel, 0, 3);

        Controls.Add(_layout);

        _treeView.BeforeExpand += async (_, args) => await ExpandNodeAsync(args.Node);
        _treeView.AfterSelect += (_, args) => _viewModel.SelectedNode = args.Node.Tag as ExplorerNodeViewModel;
        _treeView.NodeMouseClick += (_, args) =>
        {
            _treeView.SelectedNode = args.Node;
            _viewModel.SelectedNode = args.Node.Tag as ExplorerNodeViewModel;
            if (args.Button == MouseButtons.Right
                && args.Node.Tag is ExplorerNodeViewModel contextNode)
            {
                _treeView.ContextMenuStrip = ContextMenuFactory?.Invoke(contextNode) ?? _contextMenu;
            }
            if (args.Button == MouseButtons.Left
                && args.Node.Tag is ExplorerNodeViewModel node
                && node.HasChildren
                && !node.ChildrenLoaded)
            {
                args.Node.Expand();
            }
        };
        _treeView.NodeMouseDoubleClick += async (_, args) =>
        {
            _viewModel.SelectedNode = args.Node.Tag as ExplorerNodeViewModel;
            await CopySelectedDdlAsync();
        };
        _filterBox.KeyDown += async (_, args) =>
        {
            if (args.KeyCode == Keys.Enter)
            {
                args.Handled = true;
                args.SuppressKeyPress = true;
                _searchTimer.Stop();
                await SearchAsync();
            }
            else if (args.KeyCode == Keys.Escape)
            {
                args.Handled = true;
                args.SuppressKeyPress = true;
                _searchTimer.Stop();
                _filterBox.Text = string.Empty;
                _viewModel.Filter = string.Empty;
                _viewModel.SearchResults.Clear();
                RenderSearchResults();
                _viewModel.CancelCommand.Execute(null);
            }
        };
        _filterBox.TextChanged += (_, _) =>
        {
            _filterClearLabel.Visible = _filterBox.Text.Length > 0;
            _searchTimer.Stop();
            if (string.IsNullOrWhiteSpace(_filterBox.Text))
            {
                _viewModel.Filter = string.Empty;
                _viewModel.SearchResults.Clear();
                RenderSearchResults();
                return;
            }
            _searchTimer.Start();
        };
        _fastBrowser.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex < 0 || _fastBrowser.Rows[args.RowIndex].Tag is not ExplorerNodeViewModel result)
                return;
            await SelectObjectAsync(
                result.Path.Connection,
                result.Path.Database,
                result.Path.Schema ?? string.Empty,
                result.Name,
                result.Kind);
        };
        _treeView.KeyDown += async (_, args) =>
        {
            if (args.KeyCode == Keys.F5)
            {
                args.Handled = true;
                await RefreshAsync();
            }
            else if (args.KeyCode == Keys.Escape)
            {
                args.Handled = true;
                _viewModel.CancelCommand.Execute(null);
            }
        };
        ApplyDpiMetrics();
        Load += async (_, _) => await InitializeAsync();
    }

    public TreeView DatabaseTreeView => _treeView;
    public TextBox TbFastSchemaSearch => _filterBox;
    public DataGridView DgvFastDbBrowser => _fastBrowser;
    public DatabaseExplorerViewModel ViewModel => _viewModel;
    public Func<ExplorerNodeViewModel, ContextMenuStrip?>? ContextMenuFactory { get; set; }
    public void CollapseAllNodes() => _treeView.CollapseAll();
    public ComboBox CbWhatDb => _databaseComboBox;

    /// <summary>Sets enabled/disabled state for all interactive controls.</summary>
    public void SetControlsEnabled(bool enabled)
    {
        _treeView.Enabled = enabled;
        _filterBox.Enabled = enabled;
        _databaseComboBox.Enabled = enabled;
        _fastBrowser.Enabled = enabled;
        _addConnectionButton.Enabled = enabled;
        _editConnectionButton.Enabled = enabled;
        _refreshButton.Enabled = enabled;
        _collapseAllButton.Enabled = enabled;
    }

    /// <summary>
    /// Extracts the connection name from the currently selected tree node.
    /// Mirror of BaseWindow.SchemaRefresh.GetSelectedConnectionName().
    /// </summary>
    public string? GetSelectedConnectionName()
    {
        if (_viewModel.SelectedNode is ExplorerNodeViewModel node
            && !string.IsNullOrWhiteSpace(node.Path.Connection))
        {
            return node.Path.Connection;
        }
        return _viewModel.ConnectionName;
    }

    /// <summary>
    /// Determines whether the tree needs a full reload (clear + rebuild) when
    /// switching to the requested connection, or whether the current tree state
    /// can be reused as-is.
    /// </summary>
    public static bool RequiresConnectionReload(string? currentConnection, int rootCount, string requestedConnection)
        => rootCount == 0
            || !string.Equals(currentConnection, requestedConnection, StringComparison.OrdinalIgnoreCase);

    private void PositionFilterClearLabel()
    {
        // Null guard: Resize events can fire during construction before _filterPanel is assigned.
        if (_filterPanel is null) return;

        // The clear label sits in the search panel, not directly in _filterPanel.
        // Offset Y by the combo row height (24px) so it appears centered in the search box.
        _filterClearLabel.Location = new Point(
            _filterPanel.Width - _filterClearLabel.Width - 3,
            24 + (_filterPanel.Height - 24 - _filterClearLabel.Height) / 2);
    }

    public void ApplyDpiMetrics()
    {
        int dpi = DeviceDpi;
        int padding = DpiScale.Scale(4, dpi);
        int fieldHeight = Math.Max(
            DpiScale.Scale(24, dpi),
            (int)Math.Ceiling(Font.GetHeight()) + DpiScale.Scale(8, dpi));

        Padding = new Padding(padding);
        MinimumSize = DpiScale.Scale(new Size(240, 280), dpi);
        _layout.RowStyles[0] = new RowStyle(SizeType.Absolute, DpiScale.Scale(28, dpi));
        bool hasSearchResults = _fastBrowser.Rows.Count > 0;
        _fastBrowser.Visible = hasSearchResults;
        _layout.RowStyles[2] = new RowStyle(
            SizeType.Absolute,
            hasSearchResults
                ? Math.Min(DpiScale.Scale(220, dpi), Math.Max(DpiScale.Scale(96, dpi), Height / 3))
                : 0F);
        _layout.RowStyles[3] = new RowStyle(SizeType.Absolute, DpiScale.Scale(48, dpi));
        _toolbarPanel.Height = DpiScale.Scale(28, dpi);
        _filterPanel.Height = DpiScale.Scale(48, dpi);
        _filterBox.MinimumSize = new Size(0, fieldHeight);
        _treeView.Indent = DpiScale.Scale(16, dpi);
        _treeView.ItemHeight = Math.Max(
            DpiScale.Scale(24, dpi),
            (int)Math.Ceiling(_treeView.Font.GetHeight()) + DpiScale.Scale(8, dpi));
        _fastBrowser.RowTemplate.Height = Math.Max(
            DpiScale.Scale(24, dpi),
            (int)Math.Ceiling(_fastBrowser.Font.GetHeight()) + DpiScale.Scale(8, dpi));
        _fastBrowser.ColumnHeadersHeight = _fastBrowser.RowTemplate.Height + DpiScale.Scale(4, dpi);
        PositionFilterClearLabel();
        PerformLayout();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyDpiMetrics();
    }

    public ImageList? TreeViewImageList
    {
        get => _treeView.ImageList;
        set
        {
            _treeView.ImageList = value;
            RenderRoots();
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _viewModel.InitializeAsync(refresh: false, cancellationToken: cancellationToken);
        RenderRoots();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _viewModel.RefreshAsync(cancellationToken);
        RenderRoots();
    }

    /// <summary>Refreshes the tree for a specific connection and updates the ViewModel's ConnectionName.</summary>
    public async Task RefreshAsync(string connectionName, CancellationToken cancellationToken = default)
    {
        _viewModel.ConnectionName = connectionName;
        await _viewModel.RefreshAsync(cancellationToken);
        RenderRoots();
    }

    /// <summary>Loads roots from the existing repository state without a full re-download.</summary>
    public async Task InitializeAsync(string connectionName, CancellationToken cancellationToken = default)
    {
        _viewModel.ConnectionName = connectionName;
        await _viewModel.InitializeAsync(connectionName, refresh: false, cancellationToken: cancellationToken);
        RenderRoots();
    }

    public async Task<bool> SelectObjectAsync(
        string connectionName,
        string? databaseName,
        string schemaName,
        string objectName,
        SchemaNodeKind? objectKind = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionName)
            || string.IsNullOrWhiteSpace(schemaName)
            || string.IsNullOrWhiteSpace(objectName))
            return false;

        if (!string.Equals(_viewModel.ConnectionName, connectionName, StringComparison.OrdinalIgnoreCase)
            || _viewModel.RootNodes.Count == 0)
        {
            await _viewModel.InitializeAsync(connectionName, refresh: false, cancellationToken).ConfigureAwait(true);
            RenderRoots();
        }

        TreeNode? connectionNode = FindTreeNode(node =>
            node.Tag is ExplorerNodeViewModel vm
            && vm.Kind == SchemaNodeKind.Connection
            && vm.Name.Equals(connectionName, StringComparison.OrdinalIgnoreCase));
        if (connectionNode?.Tag is not ExplorerNodeViewModel connectionVm)
            return false;

        await ExpandTreeNodeAsync(connectionNode, cancellationToken).ConfigureAwait(true);
        TreeNode? databaseNode = FindChild(connectionNode, node =>
            node.Tag is ExplorerNodeViewModel vm
            && vm.Kind == SchemaNodeKind.Database
            && (string.IsNullOrWhiteSpace(databaseName)
                || vm.Name.Equals(databaseName, StringComparison.OrdinalIgnoreCase)
                || vm.Path.Database?.Equals(databaseName, StringComparison.OrdinalIgnoreCase) == true));
        if (databaseNode?.Tag is not ExplorerNodeViewModel databaseVm)
            return false;

        await ExpandTreeNodeAsync(databaseNode, cancellationToken).ConfigureAwait(true);
        TreeNode? schemaNode = FindChild(databaseNode, node =>
            node.Tag is ExplorerNodeViewModel vm
            && vm.Kind == SchemaNodeKind.Schema
            && (vm.Name.Trim('"').Equals(schemaName.Trim('"'), StringComparison.OrdinalIgnoreCase)
                || vm.Path.Schema?.Trim('"').Equals(schemaName.Trim('"'), StringComparison.OrdinalIgnoreCase) == true));
        if (schemaNode?.Tag is not ExplorerNodeViewModel schemaVm)
            return false;

        await ExpandTreeNodeAsync(schemaNode, cancellationToken).ConfigureAwait(true);
        TreeNode? objectNode = FindChild(schemaNode, node =>
            node.Tag is ExplorerNodeViewModel vm
            && vm.Name.Trim('"').Equals(objectName.Trim('"'), StringComparison.OrdinalIgnoreCase)
            && (objectKind is null || vm.Kind == objectKind));
        if (objectNode?.Tag is not ExplorerNodeViewModel objectVm)
            return false;

        _viewModel.SelectedNode = objectVm;
        objectNode.EnsureVisible();
        _treeView.SelectedNode = objectNode;
        _treeView.Focus();
        return true;
    }

    private async Task SearchAsync()
    {
        _viewModel.Filter = _filterBox.Text;
        await _viewModel.SearchAsync();
        RenderSearchResults();
    }

    private void RenderSearchResults()
    {
        if (IsDisposed) return;
        _fastBrowser.Rows.Clear();
        foreach (ExplorerNodeViewModel result in _viewModel.SearchResults)
        {
            int rowIndex = _fastBrowser.Rows.Add(
                result.Model.ProviderKind ?? result.Kind.ToString(),
                result.Name,
                result.Path.Database ?? string.Empty,
                result.Model.Description ?? string.Empty,
                result.Model.Owner ?? string.Empty);
            _fastBrowser.Rows[rowIndex].Tag = result;
        }

        // Collapse the search grid when empty so its column headers do not peek through.
        bool hasResults = _fastBrowser.Rows.Count > 0;
        float newHeight = hasResults
            ? Math.Min(DpiScale.Scale(220, DeviceDpi), Math.Max(DpiScale.Scale(96, DeviceDpi), Height / 3))
            : 0F;
        _fastBrowser.Visible = hasResults;
        _layout.RowStyles[2] = new RowStyle(SizeType.Absolute, newHeight);
        PerformLayout();
    }

    private async Task ExpandNodeAsync(TreeNode treeNode)
    {
        await ExpandTreeNodeAsync(treeNode);
    }

    private async Task ExpandTreeNodeAsync(TreeNode treeNode, CancellationToken cancellationToken = default)
    {
        if (treeNode.Tag is not ExplorerNodeViewModel node) return;
        await _viewModel.ExpandAsync(node, cancellationToken).ConfigureAwait(true);
        treeNode.Expand();
    }

    private void RenderRoots()
    {
        if (IsDisposed) return;
        _treeView.BeginUpdate();
        try
        {
            ClearNodeBindings();
            _treeView.Nodes.Clear();
            foreach (var root in _viewModel.RootNodes)
            {
                _treeView.Nodes.Add(CreateTreeNode(root));
            }
        }
        finally
        {
            _treeView.EndUpdate();
        }
    }

    private TreeNode CreateTreeNode(ExplorerNodeViewModel node)
    {
        var treeNode = new TreeNode(node.Model.DisplayName ?? node.Name) { Name = node.Id, Tag = node };
        string? imageKey = node.Kind switch
        {
            SchemaNodeKind.Connection => "server_connect.png",
            SchemaNodeKind.Database => "database.png",
            SchemaNodeKind.Schema => "Folder.png",
            SchemaNodeKind.Table => "Table.bmp",
            SchemaNodeKind.View => "application_view_tile.png",
            SchemaNodeKind.Procedure => "bug.png",
            SchemaNodeKind.Function => "sum.png",
            SchemaNodeKind.Alias => "table_link.png",
            SchemaNodeKind.Synonym => "application_lightning.png",
            SchemaNodeKind.Column => "table_column.png",
            _ => "bullet_white.png"
        };
        treeNode.ImageKey = imageKey;
        treeNode.SelectedImageKey = imageKey;
        _treeNodes[node] = treeNode;
        node.ChildrenAppended += OnChildrenAppended;
        node.PropertyChanged += OnNodePropertyChanged;
        foreach (ExplorerNodeViewModel child in node.Children)
            treeNode.Nodes.Add(CreateTreeNode(child));
        if (node.HasChildren && !node.ChildrenLoaded && treeNode.Nodes.Count == 0)
            treeNode.Nodes.Add(CreateLoadingTreeNode());
        return treeNode;
    }

    private static TreeNode CreateLoadingTreeNode() => new("Loading…")
    {
        Name = "schemaLoading",
        Tag = LoadingMarker,
        ImageKey = "hourglass.png",
        SelectedImageKey = "hourglass.png"
    };

    private void OnChildrenAppended(object? sender, ExplorerChildrenAppendedEventArgs args)
    {
        if (sender is not ExplorerNodeViewModel node || !_treeNodes.TryGetValue(node, out TreeNode? treeNode))
            return;
        if (InvokeRequired)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke((MethodInvoker)(() => OnChildrenAppended(sender, args)));
            }
            catch (InvalidOperationException)
            {
                // The control can be disposed between the lifecycle check and BeginInvoke.
            }
            return;
        }

        _treeView.BeginUpdate();
        try
        {
            RemoveLoadingTreeNodes(treeNode);
            foreach (ExplorerNodeViewModel child in args.Children)
                treeNode.Nodes.Add(CreateTreeNode(child));
        }
        finally
        {
            _treeView.EndUpdate();
        }
    }

    private void OnNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(ExplorerNodeViewModel.ChildrenLoaded)
            || sender is not ExplorerNodeViewModel node
            || !node.ChildrenLoaded
            || !_treeNodes.TryGetValue(node, out TreeNode? treeNode))
            return;
        if (InvokeRequired)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke((MethodInvoker)(() => OnNodePropertyChanged(sender, args)));
            }
            catch (InvalidOperationException)
            {
                // The control can be disposed between the lifecycle check and BeginInvoke.
            }
            return;
        }
        RemoveLoadingTreeNodes(treeNode);
    }

    private static void RemoveLoadingTreeNodes(TreeNode treeNode)
    {
        foreach (TreeNode loadingNode in treeNode.Nodes.Cast<TreeNode>().Where(child => ReferenceEquals(child.Tag, LoadingMarker)).ToArray())
            treeNode.Nodes.Remove(loadingNode);
    }

    private void ClearNodeBindings()
    {
        foreach ((ExplorerNodeViewModel node, _) in _treeNodes)
        {
            node.ChildrenAppended -= OnChildrenAppended;
            node.PropertyChanged -= OnNodePropertyChanged;
        }
        _treeNodes.Clear();
    }

    private TreeNode? FindTreeNode(Func<TreeNode, bool> predicate)
    {
        foreach (TreeNode root in _treeView.Nodes)
        {
            TreeNode? match = FindTreeNode(root, predicate);
            if (match is not null) return match;
        }
        return null;
    }

    private static TreeNode? FindTreeNode(TreeNode node, Func<TreeNode, bool> predicate)
    {
        if (predicate(node)) return node;
        foreach (TreeNode child in node.Nodes)
        {
            TreeNode? match = FindTreeNode(child, predicate);
            if (match is not null) return match;
        }
        return null;
    }

    private static TreeNode? FindChild(TreeNode parent, Func<TreeNode, bool> predicate) =>
        parent.Nodes.Cast<TreeNode>().FirstOrDefault(predicate);

    private async Task CopySelectedDdlAsync()
    {
        if (_viewModel.SelectedNode is null) return;
        await _viewModel.LoadDdlAsync(_viewModel.SelectedNode);
        if (!string.IsNullOrWhiteSpace(_viewModel.LastDdl))
            Clipboard.SetText(_viewModel.LastDdl);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchTimer.Stop();
            _searchTimer.Dispose();
            ClearNodeBindings();
        }
        base.Dispose(disposing);
    }
}
