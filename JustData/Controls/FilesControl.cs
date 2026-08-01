using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Services.Utilities;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JustyBaseLegacy.UI.Helpers;
using JustData.ViewModels.Files;


namespace JustyBaseLegacy.UI.Controls
{
    public partial class FilesControl : UserControl
    {
        private ToolTip _toolTip;
        private readonly ToolStripMenuItem filesAddMenuItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem remove = new ToolStripMenuItem();
        private readonly ToolStripMenuItem newFolder = new ToolStripMenuItem();
        private readonly ToolStripMenuItem newFile = new ToolStripMenuItem();
        private readonly ToolStripMenuItem filesCollapseMenuItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem filesExpandMenuItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem filesDeleteMenuItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem filesOpenInExplorerMenuItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem filesOpenInGridMenuItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem filesRefreshMenuItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem cmExpandToLastKnown = new ToolStripMenuItem();
        private readonly CustomToolStripSeparator toolStripSeparator12;

        private readonly IUiHelperService _uiHelperService;
        private readonly IColorTheme _colorTheme;
        private readonly BaseWindow _baseWindow;
        private readonly IApplicationSettingsContext _applicationSettingsContext;
        private readonly FilesViewModel _filesViewModel;
        private readonly ImageList _filesImageList = new ImageList();
        private Panel _searchFiltersPanel;
        private TextBox _textBoxExtensions;
        private CheckBox _checkWholeWord;
        private CheckBox _checkMatchCase;
        private CheckBox _checkUseRegex;
        private Button _buttonSearchOptions;
        private Button _buttonClearSearch;
        private Button _buttonAddFolder;
        private ContextMenuStrip _searchOptionsMenu;
        private ToolStripMenuItem _menuWholeWord;
        private ToolStripMenuItem _menuMatchCase;
        private ToolStripMenuItem _menuUseRegex;
        private Label _searchStatus;
        private bool _searchOptionsVisible = false;
        private bool _stylingHooksAdded;
        private bool _isLayingOutSearchUi;

        private readonly IFileSearchEngine _fileSearchEngine;

        public FilesControl(IUiHelperService uiHelperService,
            IColorTheme colorTheme,
            BaseWindow baseWindow,
            IApplicationSettingsContext applicationSettingsContext, ImageList imageList, FilesViewModel filesViewModel,
            IFileSearchEngine fileSearchEngine)
        {
            _uiHelperService = uiHelperService;
            _colorTheme = colorTheme;
            _baseWindow = baseWindow;
            _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
            _filesViewModel = filesViewModel ?? throw new ArgumentNullException(nameof(filesViewModel));
            _fileSearchEngine = fileSearchEngine ?? throw new ArgumentNullException(nameof(fileSearchEngine));
            toolStripSeparator12 = new CustomToolStripSeparator(_applicationSettingsContext.Config.UseSpecialColoring, Color.FromArgb(_applicationSettingsContext.Config.StripFore[0], _applicationSettingsContext.Config.StripFore[1], _applicationSettingsContext.Config.StripFore[2]), Color.FromArgb(_applicationSettingsContext.Config.StripBack[0], _applicationSettingsContext.Config.StripBack[1], _applicationSettingsContext.Config.StripBack[2]));
            InitializeComponent();
            InitializeSearchUi();
            InitializeTooltip();
            InitializeSearchPlaceholder();
            ApplyModernStyling();
            filesTreeView.ImageList = _filesImageList;
            filesTreeView.BeforeExpand += filesTreeView_BeforeExpand;
            filesTreeView.AfterCollapse += new TreeViewEventHandler(filesTreeView_AfterCollapse);
            filesTreeView.AfterLabelEdit += new NodeLabelEditEventHandler(TreeViewPliki_AfterLabelEdit);

            filesTreeView.ItemDrag += new ItemDragEventHandler(filesTreeView_ItemDrag);
            filesTreeView.KeyDown += new KeyEventHandler(filesTreeView_KeyDown);
            filesTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(filesTreeView_NodeMouseClick);
            filesTreeView.NodeMouseDoubleClick += new TreeNodeMouseClickEventHandler(filesTreeView_NodeMouseDoubleClick_1);


            textBoxFileSearch.TextChanged += new EventHandler(textBoxFileSearch_TextChanged);
            textBoxFileSearch.KeyDown += TextBoxFileSearch_KeyDown;

            // 
            // filesAddMenuItem
            // 
            filesAddMenuItem.Name = "filesAddMenuItem";
            filesAddMenuItem.Size = new Size(180, 22);
            filesAddMenuItem.Text = "Add Folder";
            filesAddMenuItem.Click += FilesAddFolder_Click;
            // 
            // remove
            // 
            remove.Enabled = false;
            remove.Name = "remove";
            remove.Size = new Size(180, 22);
            remove.Text = "Delete";
            remove.Click += FilesFileSystemAction_Click;
            // 
            // newFolder
            // 
            newFolder.Enabled = false;
            newFolder.Name = "newFolder";
            newFolder.Size = new Size(180, 22);
            newFolder.Text = "Create Folder";
            newFolder.Click += FilesFileSystemAction_Click;
            // 
            // newFile
            // 
            newFile.Enabled = false;
            newFile.Name = "newFile";
            newFile.Size = new Size(180, 22);
            newFile.Text = "Create File";
            newFile.Click += FilesFileSystemAction_Click;
            // 
            // filesCollapseMenuItem
            // 
            filesCollapseMenuItem.Name = "filesCollapseMenuItem";
            filesCollapseMenuItem.Size = new Size(180, 22);
            filesCollapseMenuItem.Text = "Collapse All";
            filesCollapseMenuItem.Click += FilesCollapse_Click;
            // 
            // filesExpandMenuItem
            // 
            filesExpandMenuItem.Name = "filesExpandMenuItem";
            filesExpandMenuItem.Size = new Size(180, 22);
            filesExpandMenuItem.Text = "Expand All";
            filesExpandMenuItem.Click += FilesExpand_Click;
            // 
            // filesDeleteMenuItem
            // 
            filesDeleteMenuItem.Enabled = false;
            filesDeleteMenuItem.Name = "filesDeleteMenuItem";
            filesDeleteMenuItem.Size = new Size(180, 22);
            filesDeleteMenuItem.Text = "Remove Entity";
            filesDeleteMenuItem.Click += FilesDeleteFolder_Click;
            // 
            // filesOpenInExplorerMenuItem
            // 
            filesOpenInExplorerMenuItem.Name = "filesOpenInExplorerMenuItem";
            filesOpenInExplorerMenuItem.Size = new Size(180, 22);
            filesOpenInExplorerMenuItem.Text = "Open in Explorer";
            filesOpenInExplorerMenuItem.Click += FilesOpenExplorer_Click;
            // 
            // filesOpenInGridMenuItem
            // 
            filesOpenInGridMenuItem.Name = "filesOpenInGridMenuItem";
            filesOpenInGridMenuItem.Size = new Size(180, 22);
            filesOpenInGridMenuItem.Text = "Open in Grid";
            filesOpenInGridMenuItem.Click += FilesOpenInGrid_Click;
            // 
            // filesRefreshMenuItem
            // 
            filesRefreshMenuItem.Name = "filesRefreshMenuItem";
            filesRefreshMenuItem.Size = new Size(180, 22);
            filesRefreshMenuItem.Text = "Refresh";
            filesRefreshMenuItem.Click += filesRefreshMenuItem_Click;
            // 
            // cmExpandToLastKnown
            // 
            cmExpandToLastKnown.Name = "cmExpandToLastKnown";
            cmExpandToLastKnown.Size = new Size(180, 22);
            cmExpandToLastKnown.Text = "Expand last position";
            cmExpandToLastKnown.Click += CmPlikiExpand2_Click;
            // 
            // toolStripSeparator12
            // 
            toolStripSeparator12.Name = "toolStripSeparator12";
            toolStripSeparator12.Size = new Size(177, 6);


            ContextMenuStrip filesContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            // 
            // filesContextMenuStrip
            // 
            filesContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            filesRefreshMenuItem,
            filesCollapseMenuItem,
            filesExpandMenuItem,
            cmExpandToLastKnown,
            filesAddMenuItem,
            filesDeleteMenuItem,
            filesOpenInExplorerMenuItem,
            filesOpenInGridMenuItem,
            toolStripSeparator12,
            newFolder,
            newFile,
            remove});
            filesContextMenuStrip.Name = "filesContextMenuStrip";
            filesContextMenuStrip.Size = new System.Drawing.Size(181, 252);
            filesContextMenuStrip.Renderer = _colorTheme.GetRenderer();
            filesTreeView.ContextMenuStrip = filesContextMenuStrip;
            filesTreeView.ImageIndex = 1;
            filesTreeView.LabelEdit = true;
            filesTreeView.Location = new System.Drawing.Point(3, 3);
            filesTreeView.SelectedImageIndex = 1;

            this.Dock = DockStyle.Fill;
            this.HandleCreated += FilesControl_HandleCreated;
            ApplyDpiMetrics();
        }

        private void InitializeSearchUi()
        {
            _textBoxExtensions = new TextBox { PlaceholderText = "Files to include: *.sql, *.cs", TabIndex = 2 };
            _checkWholeWord = new CheckBox { Text = "Whole word", AutoSize = true, TabIndex = 3 };
            _checkMatchCase = new CheckBox { Text = "Match case", AutoSize = true, TabIndex = 4 };
            _checkUseRegex = new CheckBox { Text = "Regex", AutoSize = true, TabIndex = 5 };
            _buttonSearchOptions = new Button { Text = "⋯", TabIndex = 6, AccessibleName = "Search options" };
            _buttonClearSearch = new Button { Text = "×", TabIndex = 7, AccessibleName = "Clear search" };
            _buttonAddFolder = new Button { Text = "📂", TabIndex = 8, AccessibleName = "Add folder", AutoSize = true };
            _searchStatus = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Text = "Ready" };

            _menuWholeWord = new ToolStripMenuItem("Whole word") { CheckOnClick = true };
            _menuMatchCase = new ToolStripMenuItem("Match case") { CheckOnClick = true };
            _menuUseRegex = new ToolStripMenuItem("Regular expression") { CheckOnClick = true };
            _searchOptionsMenu = new ContextMenuStrip();
            _searchOptionsMenu.Items.AddRange(new ToolStripItem[]
            {
                _menuWholeWord,
                _menuMatchCase,
                new ToolStripSeparator(),
                _menuUseRegex
            });

            _searchFiltersPanel = new Panel { Dock = DockStyle.Top, Height = 30, Visible = false };
            _searchFiltersPanel.Controls.Add(_checkUseRegex);
            _searchFiltersPanel.Controls.Add(_checkMatchCase);
            _searchFiltersPanel.Controls.Add(_checkWholeWord);

            panelSearchContainer.Dock = DockStyle.None;
            panelSearchContainer.Anchor = AnchorStyles.None;
            panelSearchContainer.Margin = new Padding(0, 0, 0, 3);
            panelSearchContainer.Height = 30;
            _buttonClearSearch.Dock = DockStyle.Right;
            _buttonSearchOptions.Dock = DockStyle.Right;
            _buttonAddFolder.Dock = DockStyle.Right;
            _buttonClearSearch.Width = 30;
            _buttonSearchOptions.Width = 30;
            _buttonAddFolder.Width = 30;
            textBoxFileSearch.Dock = DockStyle.Fill;
            labelSearchIcon.Dock = DockStyle.Left;
            labelSearchIcon.Text = string.Empty;
            panelSearchContainer.Controls.Add(_buttonClearSearch);
            panelSearchContainer.Controls.Add(_buttonSearchOptions);
            panelSearchContainer.Controls.Add(_buttonAddFolder);

            _textBoxExtensions.Dock = DockStyle.None;
            _textBoxExtensions.Anchor = AnchorStyles.None;
            _textBoxExtensions.Height = 26;
            _textBoxExtensions.Margin = new Padding(0, 0, 0, 3);
            _searchStatus.Dock = DockStyle.None;
            _searchStatus.Anchor = AnchorStyles.None;
            _searchStatus.Height = 22;

            Controls.Add(_searchStatus);
            Controls.Add(_searchFiltersPanel);
            Controls.Add(_textBoxExtensions);
            Controls.SetChildIndex(filesTreeView, Controls.Count - 1);
            filesTreeView.Dock = DockStyle.None;
            filesTreeView.Anchor = AnchorStyles.None;

            _buttonSearchOptions.Click += (_, _) =>
            {
                // Use a real popup so the options are always visible above the
                // tree and are not affected by the parent docking layout.
                _searchOptionsMenu.Show(_buttonSearchOptions, 0, _buttonSearchOptions.Height);
            };
            _buttonClearSearch.Click += (_, _) =>
            {
                textBoxFileSearch.Clear();
                textBoxFileSearch.Focus();
            };
            _buttonAddFolder.Click += async (_, _) =>
            {
                await FilesAddFolder_ClickAsync();
            };
            _textBoxExtensions.TextChanged += (_, _) => ScheduleFilenameSearch();
            _checkWholeWord.CheckedChanged += (_, _) => UpdateSearchStatus();
            _checkMatchCase.CheckedChanged += (_, _) => UpdateSearchStatus();
            _checkUseRegex.CheckedChanged += (_, _) => UpdateSearchStatus();
            _menuWholeWord.CheckedChanged += (_, _) =>
            {
                _checkWholeWord.Checked = _menuWholeWord.Checked;
                UpdateSearchStatus();
            };
            _menuMatchCase.CheckedChanged += (_, _) =>
            {
                _checkMatchCase.Checked = _menuMatchCase.Checked;
                UpdateSearchStatus();
            };
            _menuUseRegex.CheckedChanged += (_, _) =>
            {
                _checkUseRegex.Checked = _menuUseRegex.Checked;
                UpdateSearchStatus();
            };
        }

        public void ApplyDpiMetrics()
        {
            int dpi = DeviceDpi;
            int padding = DpiScale.Scale(6, dpi);
            int fieldHeight = Math.Max(DpiScale.Scale(28, dpi), (int)Math.Ceiling(textBoxFileSearch.Font.GetHeight()) + padding);
            panelSearchContainer.Height = fieldHeight;
            _textBoxExtensions.Height = fieldHeight;
            _searchFiltersPanel.Height = _searchOptionsVisible ? fieldHeight + DpiScale.Scale(6, dpi) : 0;
            int optionLeft = DpiScale.Scale(6, dpi);
            foreach (CheckBox option in new[] { _checkWholeWord, _checkMatchCase, _checkUseRegex })
            {
                option.Top = Math.Max(0, (fieldHeight - option.Height) / 2);
                option.Left = optionLeft;
                optionLeft += option.Width + DpiScale.Scale(12, dpi);
            }
            _searchStatus.Height = DpiScale.Scale(22, dpi);
            filesTreeView.ItemHeight = Math.Max(DpiScale.Scale(24, dpi), (int)Math.Ceiling(filesTreeView.Font.GetHeight()) + DpiScale.Scale(8, dpi));
            FilesImageListHelper.EnsurePopulated(_filesImageList, dpi, _colorTheme.TreeViewForeColor);
            LayoutSearchUi(fieldHeight, dpi);
            filesTreeView.Invalidate();
        }

        private void LayoutSearchUi(int fieldHeight, int dpi)
        {
            if (_isLayingOutSearchUi || _textBoxExtensions is null || _searchStatus is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            _isLayingOutSearchUi = true;
            try
            {
                LayoutSearchUiCore(fieldHeight, dpi);
            }
            finally
            {
                _isLayingOutSearchUi = false;
            }
        }

        private void LayoutSearchUiCore(int fieldHeight, int dpi)
        {

            int outer = Math.Max(Padding.Left, DpiScale.Scale(6, dpi));
            int gap = DpiScale.Scale(4, dpi);
            int width = Math.Max(1, ClientSize.Width - outer * 2);
            int top = Math.Max(Padding.Top, DpiScale.Scale(6, dpi));

            panelSearchContainer.SetBounds(outer, top, width, fieldHeight);
            top += fieldHeight + gap;
            _textBoxExtensions.SetBounds(outer, top, width, fieldHeight);
            top += fieldHeight + gap;

            if (_searchOptionsVisible)
            {
                _searchFiltersPanel.SetBounds(outer, top, width, _searchFiltersPanel.Height);
                top += _searchFiltersPanel.Height + gap;
            }
            else
            {
                _searchFiltersPanel.SetBounds(outer, top, width, 0);
            }

            int statusTop = Math.Max(top, ClientSize.Height - _searchStatus.Height - Padding.Bottom);
            _searchStatus.SetBounds(outer, statusTop, width, _searchStatus.Height);
            filesTreeView.SetBounds(outer, top, width, Math.Max(1, statusTop - top - gap));
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_textBoxExtensions is not null)
            {
                int dpi = DeviceDpi;
                int fieldHeight = Math.Max(DpiScale.Scale(28, dpi), (int)Math.Ceiling(textBoxFileSearch.Font.GetHeight()) + DpiScale.Scale(6, dpi));
                LayoutSearchUi(fieldHeight, dpi);
            }
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyDpiMetrics();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _filesImageList.Dispose();
                _searchOptionsMenu?.Dispose();
            }
            base.Dispose(disposing);
        }

        private async void FilesControl_HandleCreated(object sender, EventArgs e)
        {
            try
            {
                if (!this.DesignMode) // Don't run in designer
                {
                    await _filesViewModel.InitializeAsync(_applicationSettingsContext.Config.StartsFolderPaths,
                        _applicationSettingsContext.Config.SortByLastWrite,
                        _applicationSettingsContext.Config.SortByName,
                        _extensionsToSearch);
                    await AddDirs(_filesViewModel.RootPaths.ToList(), clearExisting: true);
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Files panel initialization failed: {exception.GetType().Name}");
            }
        }

        private void filesTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                (sender as TreeView).SelectedNode = e.Node;

            TreeNode node = (sender as TreeView).SelectedNode;
            if (e.Button == MouseButtons.Right && node != null)
            {
                if (node.Level == 0)
                {
                    filesDeleteMenuItem.Enabled = true;
                }
                else
                {
                    filesDeleteMenuItem.Enabled = false;
                }

                if (Directory.Exists(node.Name))
                {
                    newFolder.Enabled = true;
                    newFile.Enabled = true;
                    remove.Enabled = true;
                }
                else if (File.Exists(node.Name))
                {
                    remove.Enabled = true;
                }
                else
                {
                    newFolder.Enabled = false;
                    newFile.Enabled = false;
                    remove.Enabled = false;
                }
            }
            else
            {
                filesDeleteMenuItem.Enabled = false;
                newFolder.Enabled = false;
                newFile.Enabled = false;
                remove.Enabled = false;
            }
        }

        private async void filesTreeView_NodeMouseDoubleClick_1(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                string path = e.Node.Name;
                int lineNumber = -1;

                if (e.Node.Parent != null && e.Node.Tag is int) // It's a line node from search results
                {
                    path = e.Node.Parent.Name;
                    lineNumber = (int)e.Node.Tag;
                }

                if (File.Exists(path))
                {
                    FileAttributes attr = File.GetAttributes(path);
                    if (!attr.HasFlag(FileAttributes.Directory))
                    {
                        FastColoredTextBox? fc = null;
                        if (path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                        {
                            fc = await _baseWindow.OpenSqlFileAsync(path);
                        }
                        else
                        {
                            var p = new Process();
                            p.StartInfo = new ProcessStartInfo(path)
                            {
                                UseShellExecute = true
                            };
                            p.Start();
                        }

                        if (fc is not null && lineNumber > 0)
                        {
                            fc.SetSelectedLine(lineNumber - 1);
                            fc.Selection = new FastColoredTextBoxNS.Range(fc, lineNumber - 1);
                        }

                    }
                }
            }
            // If the file is not found, handle the exception and inform the user.
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception exc)
            {
                MessageBox.Show(this, exc.Message, "Cannot open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void filesRefreshMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                await RefreshFiles();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Files refresh error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilesOpenInGrid_Click(object sender, EventArgs e)
        {
            Form f = new Form();
            f.Width = 800;
            f.Height = 494;

            ThemedDataGridView d = new ThemedDataGridView();
            d.Columns.Add("folder", "folder");
            d.Columns.Add("file", "file");
            _colorTheme.ColorDataGridView(d);
            _uiHelperService.DoubleBufDateGridView(d);

            foreach (TreeNode item in filesTreeView.Nodes)
            {
                int n = item.Text.IndexOf('-');
                if (n != -1)
                {
                    d.Rows.Add([item.Text.Substring(0, n - 1), item.Text.Substring(n + 2)]);
                }
                else
                {
                    d.Rows.Add(["", item.Text]);
                }
            }

            d.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            d.AllowUserToAddRows = false;
            d.AllowUserToDeleteRows = false;
            d.AllowUserToResizeRows = false;
            d.AllowUserToDeleteRows = false;
            d.ReadOnly = true;
            d.Dock = DockStyle.Fill;
            f.Controls.Add(d);
            f.Show();
        }

        private async void FilesAddFolder_Click(object sender, EventArgs e)
        {
            await FilesAddFolder_ClickAsync();
        }

        private async Task FilesAddFolder_ClickAsync()
        {
            try
            {
                FolderBrowserDialog p = new System.Windows.Forms.FolderBrowserDialog();
                p.ShowDialog();
                if (String.IsNullOrEmpty(p.SelectedPath))
                    return;

                if (!_filesViewModel.RootPaths.Contains(p.SelectedPath, StringComparer.OrdinalIgnoreCase))
                    await _filesViewModel.AddRootAsync(p.SelectedPath);
                if (!_applicationSettingsContext.Config.StartsFolderPaths.Contains(p.SelectedPath, StringComparer.OrdinalIgnoreCase))
                    _applicationSettingsContext.Config.StartsFolderPaths.Add(p.SelectedPath);

                await AddDirs(_filesViewModel.RootPaths.ToList(), false);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Add folder error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void filesTreeView_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (_serachMode)
                return;
            e.Node.Nodes.Clear();
            e.Node.Nodes.Add("fool", "fool");
        }

        private void FilesCollapse_Click(object sender, EventArgs e)
        {
            filesTreeView.CollapseAll();
        }

        private void FilesExpand_Click(object sender, EventArgs e)
        {
            filesTreeView.BeginUpdate();
            filesTreeView.ExpandAll();
            filesTreeView.EndUpdate();
        }

        private async void FilesDeleteFolder_Click(object sender, EventArgs e)
        {
            try
            {
                var sel = filesTreeView.SelectedNode;
                if (sel != null)
                {
                    string zaznzNode = sel.Name;
                    _applicationSettingsContext.Config.StartsFolderPaths.Remove(zaznzNode);
                    await _filesViewModel.RemoveRootAsync(zaznzNode);
                    await AddDirs(_filesViewModel.RootPaths.ToList(), true);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Remove folder error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilesOpenExplorer_Click(object sender, EventArgs e)
        {
            var sel = filesTreeView.SelectedNode;
            if (sel != null)
            {
                string zaznzNode = sel.Name;
                if (zaznzNode[2] == '/' || zaznzNode[0] == '/')
                {
                    zaznzNode = zaznzNode.Replace('/', '\\');
                }
                Process.Start("explorer.exe", $"/select, {zaznzNode}");
            }
        }

        private const string PlaceholderText = "Search files...";
        private void filesTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (_serachMode)
            {
                if (e.Node.FirstNode.Text == PlaceholderText)
                {
                    string plainToSearch = e.Node.FirstNode.Name;
                    var matchingLines = GetMatchingLines(e.Node.Name, plainToSearch);
                    e.Node.Nodes.Clear();
                    foreach (var lineInfo in matchingLines)
                    {
                        string snippet = CreateSnippet(lineInfo.Item2, plainToSearch);
                        var lineNode = e.Node.Nodes.Add($"line:{lineInfo.Item1}", $"{lineInfo.Item1}: {snippet}");
                        lineNode.Tag = lineInfo.Item1;
                    }
                }
                return;
            }

            TreeNode node = e.Node;

            if (node.Nodes[0].Text == "fool")
            {
                node.Nodes.Clear();
                try
                {
                    if (Directory.Exists(node.Name) && _fileStructure.TryGetValue(node.Name, out var res))
                    {
                        foreach (var (name, type) in res.contents)
                        {
                            var n1 = node.Nodes.Add(name, name.Substring(name.LastIndexOf('\\') + 1));
                            if (type == DiscEntityType.directoryObject)
                            {
                                n1.Nodes.Add("fool", "fool");
                                n1.ImageIndex = 0;
                                n1.SelectedImageIndex = 0;
                            }
                        }
                    }
                    else
                    {
                        filesTreeView.Nodes.Remove(node);
                    }
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.WriteLine($"File list refresh failed: {exception.GetType().Name}");
                }
            }
        }

        private CancellationTokenSource _searchFileCancellation;


        private List<(int LineNumber, string LineText)> GetMatchingLines(string path, string toFind)
        {
            var matches = new List<(int, string)>();
            if (!File.Exists(path))
                return matches;

            try
            {
                
                Regex searchRegex = null;

                if (toFind.StartsWith("ww:"))
                {
                    if (toFind.Length > 3)
                    {
                        searchRegex = new Regex($"\b{Regex.Escape(toFind.Substring(3))}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(3));
                    }
                    else
                    {
                        return matches;
                    }
                }


                int i = 0;
                using (var fileStream = File.OpenRead(path))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        bool isMatch = false;
                        if (searchRegex != null)
                        {
                            if (searchRegex.IsMatch(line))
                            {
                                isMatch = true;
                            }
                        }
                        else
                        {
                            if (line.Contains(toFind, StringComparison.OrdinalIgnoreCase))
                            {
                                isMatch = true;
                            }
                        }

                        if (isMatch)
                        {
                            matches.Add((i + 1, line));
                        }
                        i++;
                    }
                }                
            }
            catch (Exception)
            {
                // Ignore exceptions like file in use, etc.
            }
            return matches;
        }

        private string CreateSnippet(string line, string toSearch)
        {
            string trimmedLine = line.Trim();
            int matchIndex = trimmedLine.IndexOf(toSearch, StringComparison.OrdinalIgnoreCase);

            if (matchIndex == -1)
            {
                return trimmedLine; // Fallback to showing the trimmed line
            }

            int snippetStart = Math.Max(0, matchIndex - 10);
            int snippetEnd = Math.Min(trimmedLine.Length, matchIndex + toSearch.Length + 10);

            string prefix = (snippetStart > 0) ? "... " : "";
            string suffix = (snippetEnd < trimmedLine.Length) ? " ..." : "";

            string snippet = trimmedLine.Substring(snippetStart, snippetEnd - snippetStart);

            return $"{prefix}{snippet}{suffix}";
        }

        private async void TextBoxFileSearch_KeyDown(object sender, KeyEventArgs e)
        {
            // Don't process when placeholder is active
            if (_isPlaceholderActive)
                return;

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SearchContentAsync();
                return;
            }

            string toSearch = (sender as TextBox).Text;

            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    _typingSearchTimer?.Stop();

                    if (toSearch.Length == 0)
                    {
                        await AddDirs(_filesViewModel.RootPaths.ToList(), true);
                        //uwidocznijFolderyRoot(czyKasowac: true);
                    }
                    else
                    {
                        filesTreeView.Enabled = false;
                        var filesWithLines = new List<string>();
                        _searchFileCancellation = new CancellationTokenSource();

                        int timeout = _applicationSettingsContext.Config.FileSearchTimeout;

                        var fileSearchTask = Task.Run(() =>
                        {
                            Parallel.ForEach(_fileList, new ParallelOptions { MaxDegreeOfParallelism = 16 }, path =>
                            {
                            if (_searchFileCancellation.IsCancellationRequested)
                            {
                                lock (filesWithLines)
                                {
                                    if (!filesWithLines.Contains($"aborted - exceeded {_applicationSettingsContext.Config.FileSearchTimeout} ms"))
                                    {
                                        filesWithLines.Add($"aborted - exceeded {_applicationSettingsContext.Config.FileSearchTimeout} ms");
                                        }
                                    }
                                    return;
                                }

                                if (IsFileContains(path, toSearch)) // performance optimization
                                {
                                    filesWithLines.Add(path);
                                }
                            });
                        });

                        if (await Task.WhenAny(fileSearchTask, Task.Delay(timeout, _searchFileCancellation.Token)) == fileSearchTask)
                        {
                            await fileSearchTask;
                        }
                        else
                        {
                            _searchFileCancellation.Cancel();
                        }

                        var sortedFiles = filesWithLines.OrderByDescending(p =>
                        {
                            if (p.StartsWith("aborted")) return DateTime.MaxValue;
                            try { return File.GetLastWriteTime(p); }
                            catch { return DateTime.MinValue; }
                        });

                        filesTreeView.Invoke(() =>
                        {
                            filesTreeView.Enabled = true;
                            filesTreeView.BeginUpdate();

                            filesTreeView.Nodes.Clear();

                            string plainToSearch = toSearch;
                            if (toSearch.StartsWith("ww:") && toSearch.Length > 3)
                            {
                                plainToSearch = toSearch.Substring(3);
                            }

                            int l = 0;
                            foreach (var path in sortedFiles)
                            {
                                if (path.StartsWith("aborted"))
                                {
                                    filesTreeView.Nodes.Add(path, path);
                                    continue;
                                }

                                DateTime lastWriteTime;
                                try { lastWriteTime = File.GetLastWriteTime(path); }
                                catch { continue; }

                                var fileNode = filesTreeView.Nodes.Add(path, $"{lastWriteTime}: {Path.GetFileName(path)} - {Path.GetDirectoryName(path)}");
                                fileNode.Name = path;

                                TreeNode lineNode = new TreeNode()
                                {
                                    Text = PlaceholderText,
                                    Name = plainToSearch
                                };
                                fileNode.Nodes.Add(lineNode);

                                if (l++ > 200)
                                {
                                    break;
                                }
                            }

                            if (filesWithLines.Count > 200)
                            {
                                filesTreeView.Nodes.Add("200 items exceeded", "200 items exceeded");
                            }
                            else if (filesWithLines.Count == 0)
                            {
                                filesTreeView.Nodes.Add("no results", "no results");
                            }
                            filesTreeView.EndUpdate();
                            _serachMode = true;
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Search error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void filesTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (_serachMode)
                return;
            if (e.KeyCode == Keys.F5)
            {
                await AddDirs(_filesViewModel.RootPaths.ToList(), true);
            }
            else if (e.KeyCode == Keys.F2)
            {
                if (filesTreeView.SelectedNode != null)
                {
                    if (_fileStructure.TryGetValue(filesTreeView.SelectedNode.Name, out var tmp1))
                    {
                        int lvl = tmp1.level;
                        if (lvl > 0)
                        {
                            filesTreeView.SelectedNode.BeginEdit();
                        }
                    }
                    else
                    {
                        filesTreeView.SelectedNode.BeginEdit();
                    }
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                if (filesTreeView.SelectedNode != null)
                {
                    DeleteFileOrFolderFull(filesTreeView.SelectedNode);
                }
            }
        }


        private void FilesFileSystemAction_Click(object sender, EventArgs e)
        {
            var sel = filesTreeView.SelectedNode;
            if (sel != null && !String.IsNullOrEmpty(sel.Name))
            {
                if (!sel.IsExpanded && sender != remove)
                {
                    sel.Expand();
                }

                string parentPath = sel.Name;
                if (sender == newFolder && Directory.Exists(sel.Name))
                {
                    AddFileStart(sel, parentPath);
                }
                else if (sender == newFile && Directory.Exists(sel.Name))
                {
                    AddFolderStart(sel, parentPath);
                }
                else if (sender == remove)
                {
                    DeleteFileOrFolderFull(sel);
                }
            }
        }

        private bool IsFileContains(string path, string toFind)
        {
            if (!File.Exists(path))
                return false;

            if (toFind.StartsWith("ww:"))
            {
                var searchRegex = new Regex($"\\b{Regex.Escape(toFind[3..])}\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3));
                string txt = File.ReadAllText(path);

                string res = " ";
                _baseWindow.GetTextCommentRanges(txt, ref res);
                return searchRegex.IsMatch(res);
            }

            SearchInFilesAvx2Bmi1 search = new SearchInFilesAvx2Bmi1(path, toFind);
            return (search.FindInFileSmallSteps(path, toFind) >= 0);
        }


        private async Task AddDirs(List<string> folderPaths, bool clearExisting = true)
        {
            filesAddMenuItem.Enabled = false;
            filesRefreshMenuItem.Enabled = false;
            textBoxFileSearch.Enabled = false;

            if (clearExisting)
            {
                _fileList.Clear();
                _folderList.Clear();
                _fileStructure.Clear();

                if (filesTreeView.InvokeRequired)
                {
                    filesTreeView.Invoke(() =>
                    {
                        filesTreeView.Nodes.Clear();
                    });
                }
                else
                {
                    filesTreeView.Nodes.Clear();
                }
            }

            TreeNode[] nodes = new TreeNode[folderPaths.Count];
            for (int i = 0; i < folderPaths.Count; i++)
            {
                TreeNode n = filesTreeView.Nodes.Add(folderPaths[i], $"{folderPaths[i]} - adding...");
                nodes[i] = n;
                n.ImageIndex = 0;
                n.SelectedImageIndex = 0;
            }

            for (int i = 0; i < folderPaths.Count; i++)
            {
                string folderPath = folderPaths[i];
                if (Directory.Exists(folderPath))
                {
                    var fw = new FileSystemWatcher(folderPath)
                    {
                        NotifyFilter = NotifyFilters.FileName
                    };
                    fw.EnableRaisingEvents = true;
                    fw.IncludeSubdirectories = true;
                    fw.Created += Fw_Created;
                    fw.Renamed += Fw_Renamed;
                    fw.Deleted += Fw_Deleted;
                    _searchFileWatcher.Add(fw);
                }
            }

            await Task.Run(() => // Reading from disk may take a while.
            {
                Stack<string> dirs = new Stack<string>(100);
                foreach (string folderPath in folderPaths)
                {
                    dirs.Clear();
                    dirs.Push(folderPath);

                    int baseDepth = folderPath.Count(arg => arg == '\\');
                    while (dirs.Count > 0)
                    {
                        var akt = dirs.Pop();
                        string currentDir = akt;

                        if (!Directory.Exists(currentDir))
                        {
                            continue;
                        }

                        var c = (currentDir.Count(arg => arg == '\\') - baseDepth, new List<(string name, DiscEntityType type)>());
                        _fileStructure[currentDir] = c;

                        //node = akt.Item2;
                        string[] subDirs = null;
                        try
                        {
                            subDirs = Directory.GetDirectories(currentDir);
                            List<string> tmp = new List<string>();
                            for (int i = 0; i < subDirs.Length; i++)
                            {
                                if (subDirs[i].Contains("\\."))
                                {
                                    continue;
                                }
                                tmp.Add(subDirs[i]);
                            }
                            subDirs = tmp.ToArray();
                            tmp = null;
                        }
                        catch (UnauthorizedAccessException /*exc*/)
                        {
                            continue;
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show(this, exc.Message, "Cannot add folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        string[] files = null;
                        if (_applicationSettingsContext.Config.SortByLastWrite)
                        {
                            files = new DirectoryInfo(currentDir).GetFiles().OrderByDescending(f => f.LastWriteTime).Select(f => f.FullName).ToArray();
                        }
                        else if (_applicationSettingsContext.Config.SortByName)
                        {
                            files = new DirectoryInfo(currentDir).GetFiles().OrderByDescending(f => f.LastWriteTime).Select(f => f.FullName).ToArray();
                        }
                        else
                        {
                            files = Directory.GetFiles(currentDir);
                        }

                        foreach (string file in files)
                        {
                            if (file.EndsWithAny(_extensionsToSearch, StringComparison.OrdinalIgnoreCase))
                            {
                                _fileList.Add(file);
                                if (_fileStructure.TryGetValue(currentDir, out var kkey))
                                {
                                    kkey.contents.Add((file, DiscEntityType.fileObject));
                                }
                            }
                        }

                        foreach (string d in subDirs)
                        {
                            _folderList.Add(d);
                            if (_fileStructure.TryGetValue(currentDir, out var kkey))
                            {
                                kkey.contents.Add((d, DiscEntityType.directoryObject));
                                dirs.Push(d);
                            }
                        }
                    }
                }
            });

            if (filesTreeView.InvokeRequired)
            {
                filesTreeView.Invoke(() =>
                {
                    for (int i = 0; i < folderPaths.Count; i++)
                    {
                        nodes[i].Text = nodes[i].Name;
                        nodes[i].Nodes.Add("fool", "fool");
                    }
                    filesRefreshMenuItem.Enabled = true;
                    filesAddMenuItem.Enabled = true;
                    textBoxFileSearch.Enabled = true;
                });
            }
            else
            {
                for (int i = 0; i < folderPaths.Count; i++)
                {
                    nodes[i].Text = nodes[i].Name;
                    nodes[i].Nodes.Add("fool", "fool");
                }

                this.filesRefreshMenuItem.Enabled = true;
                filesAddMenuItem.Enabled = true;
                textBoxFileSearch.Enabled = true;
            }

        }


        private readonly string[] _extensionsToSearch = new string[]
        {
            ".sql",
            ".txt",
            ".dtsx",
            ".cs",
            ".py",
            ".ps1",
            ".vb",
            ".vbs",
            ".json",
            ".xml",
            ".html"
        };

        private void Fw_Created(object s, FileSystemEventArgs e)
        {
            try
            {
                lock (_fileList)
                {
                    if (!_fileList.Contains(e.FullPath) && _extensionsToSearch.Contains(Path.GetExtension(e.FullPath), StringComparer.OrdinalIgnoreCase))
                    {
                        _fileList.Add(e.FullPath);

                        var dir = Path.GetDirectoryName(e.FullPath);
                        if (!_folderList.Contains(dir, StringComparer.OrdinalIgnoreCase))
                        {
                            _folderList.Add(dir);
                        }

                        if (_fileStructure.TryGetValue(dir, out var vl))
                        {
                            vl.contents.Add((e.FullPath, DiscEntityType.fileObject));
                        }
                        else if (_fileStructure.TryGetValue(Path.GetDirectoryName(dir), out var root))
                        {
                            root.contents ??= new List<(string name, DiscEntityType type)>();

                            root.contents.Add((dir, DiscEntityType.directoryObject));
                            List<(string name, DiscEntityType type)> contents = new List<(string name, DiscEntityType type)>()
                            {
                                (e.FullPath, DiscEntityType.fileObject)
                            };

                            _fileStructure[dir] = (root.level + 1, contents);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"File operation failed: {ex.GetType().Name}");
            }
        }


        private void Fw_Deleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                lock (_fileList)
                {
                    if (_fileList.Contains(e.FullPath))
                    {
                        _fileList.Remove(e.FullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"File operation failed: {ex.GetType().Name}");
            }
        }


        private void Fw_Renamed(object s, RenamedEventArgs e)
        {
            try
            {
                lock (_fileList)
                {
                    var dir = Path.GetDirectoryName(e.FullPath);

                    if (!_fileList.Contains(e.FullPath) && _extensionsToSearch.Contains(Path.GetExtension(e.FullPath), StringComparer.OrdinalIgnoreCase))
                    {
                        _fileList.Add(e.FullPath);
                        if (!_folderList.Contains(dir, StringComparer.OrdinalIgnoreCase))
                        {
                            _folderList.Add(dir);
                        }

                        if (!_fileStructure.ContainsKey(dir))
                        {
                            var root = _fileStructure[Path.GetDirectoryName(dir)];
                            _fileStructure[dir] = (root.level + 1, new List<(string name, DiscEntityType type)>());
                        }
                    }

                    if (_fileList.Contains(e.OldFullPath))
                    {
                        _fileList.Remove(e.OldFullPath);
                    }

                    if (_fileStructure.TryGetValue(dir, out var res))
                    {
                        if (res.contents.Contains((e.OldFullPath, DiscEntityType.fileObject)))
                        {
                            res.contents.Remove((e.OldFullPath, DiscEntityType.fileObject));
                        }

                        if (!res.contents.Contains((e.FullPath, DiscEntityType.fileObject)))
                        {
                            res.contents.Add((e.FullPath, DiscEntityType.fileObject));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"File operation failed: {ex.GetType().Name}");
            }
        }

        private async Task RefreshFiles()
        {
            CurrentSelectedTreeNode = filesTreeView.SelectedNode;
            BuildExpandedState(filesTreeView, null, CurrentSelectedTreeNode);
                await AddDirs(_filesViewModel.RootPaths.ToList(), clearExisting: true);
        }

        public void BuildExpandedState(TreeView treeView, List<TreeNode> expandedItems, TreeNode root)
        {
            if (expandedItems is null)
            {
                return;
            }

            expandedItems.Clear();

            foreach (TreeNode item in treeView.Nodes)
            {
                if (item.IsExpanded)
                {
                    expandedItems.Add(item);
                }
            }

            int i = 0;
            while (i < expandedItems.Count)
            {
                TreeNode node = expandedItems[i++];
                foreach (TreeNode item in node.Nodes)
                {
                    if (item.IsExpanded)
                    {
                        expandedItems.Add(item);
                    }
                }
            }
        }


        private string _currentAction = null;
        private void AddFileStart(TreeNode sel, string parentPath)
        {
            string folderName = "your name";
            string folderPath = $"{parentPath}\\{folderName}";
            TreeNode n = sel.Nodes.Add(folderPath, folderName);
            n.ImageIndex = 0;
            n.SelectedImageIndex = 0;
            filesTreeView.SelectedNode = n;
            n.BeginEdit();
            _currentAction = "addFolder";
        }

        private void AddFolderStart(TreeNode sel, string parentPath)
        {
            string filename = "new.sql";
            string folderPath = $"{parentPath}\\{filename}";
            TreeNode n = sel.Nodes.Add(folderPath, filename);
            n.ImageIndex = 1;
            n.SelectedImageIndex = 1;
            filesTreeView.SelectedNode = n;
            n.BeginEdit();
            _currentAction = "addFile";
        }

        private void filesTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            var node = (TreeNode)e.Item;

            if (node.Name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && File.Exists(node.Name))
            {
                DataObject data = new DataObject(DataFormats.FileDrop, new string[] { node.Name });
                DoDragDrop(data, DragDropEffects.Link);
            }
        }


        private void DeleteFileOrFolderFull(TreeNode sel)
        {
            var r = MessageBox.Show(this, "Remove permanently?", "Confirm removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes)
            {
                var r2 = MessageBox.Show(this, $"Remove permanently \"{sel.Name}\"?", "Confirm removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r2 == DialogResult.Yes)
                {
                    if (File.Exists(sel.Name))
                    {
                        try
                        {
                            File.Delete(sel.Name);
                            filesTreeView.Nodes.Remove(sel);
                            _fileList.Remove(sel.Name);
                            string dir = Path.GetDirectoryName(sel.Name);
                            var list = _fileStructure[dir].contents;
                            int i = 0;
                            foreach (var (name, _) in list)
                            {
                                if (name == sel.Name)
                                {
                                    break;
                                }
                                i++;
                            }
                            list.RemoveAt(i);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else if (Directory.Exists(sel.Name))
                    {
                        try
                        {
                            string dir = sel.Name;

                            if (_filesViewModel.RootPaths.Contains(dir, StringComparer.OrdinalIgnoreCase))
                            {
                                MessageBox.Show(this, "The root directory cannot be removed.", "Remove folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            Directory.Delete(dir, recursive: true);
                            filesTreeView.Nodes.Remove(sel);
                            _folderList.Remove(dir);

                            List<string> hList = new List<string>();


                            foreach (var file in _folderList)
                            {
                                if (file.StartsWith(dir))
                                {
                                    hList.Add(file);
                                }
                            }
                            foreach (var file in hList)
                            {
                                _folderList.Remove(file);
                            }
                            hList.Clear();

                            foreach (var file in _fileList)
                            {
                                if (file.StartsWith(dir))
                                {
                                    hList.Add(file);
                                }
                            }
                            foreach (var file in hList)
                            {
                                _fileList.Remove(file);
                            }
                            hList.Clear();

                            foreach (var item in _fileStructure)
                            {
                                if (item.Key.StartsWith(dir))
                                {
                                    hList.Add(item.Key);
                                }
                            }
                            foreach (var key in hList)
                            {
                                _fileStructure.Remove(key);
                            }

                            string dir2 = Path.GetDirectoryName(sel.Name);
                            var list = _fileStructure[dir2].contents;
                            int i = 0;
                            foreach (var (name, _) in list)
                            {
                                if (name == sel.Name)
                                {
                                    break;
                                }
                                i++;
                            }
                            list.RemoveAt(i);

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        private void CmPlikiExpand2_Click(object sender, EventArgs e)
        {
            ExpandLastKnown(filesTreeView, null);
        }

        private TreeNode CurrentSelectedTreeNode { get; set; }

        public void ExpandLastKnown(TreeView treeView, List<TreeNode>? expandedItems)
        {
            foreach (TreeNode item in expandedItems)
            {
                var tn = treeView.Nodes.Find(item.Name, true);
                foreach (TreeNode item1 in tn)
                {
                    item1.Expand();
                }
            }

            if (CurrentSelectedTreeNode != null)
            {
                var tn = treeView.Nodes.Find(CurrentSelectedTreeNode.Name, true);
                if (tn.Length >= 1)
                {
                    treeView.SelectedNode = tn[0];
                }
            }
        }


        private void TreeViewPliki_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label == null || e.Node.Parent is not TreeNode par)// escape clicked
            {
                if (!String.IsNullOrEmpty(_currentAction))
                {
                    filesTreeView.Nodes.Remove(e.Node);
                    _currentAction = null;
                }
                e.CancelEdit = true;
                return;
            }
            if (_currentAction == "addFolder")
            {
                try
                {
                    string parentPath = par.Name;
                    string folderPath = $"{par.Name}\\{e.Label}";
                    Directory.CreateDirectory(folderPath);
                    e.Node.Text = e.Label;
                    e.Node.Name = folderPath;
                    _folderList.Add(folderPath);
                    _fileStructure[parentPath].contents.Add((folderPath, DiscEntityType.directoryObject));
                    _fileStructure[folderPath] = new(_fileStructure[parentPath].level + 1, new List<(string name, DiscEntityType type)>());
                }
                catch (Exception ex)
                {
                    e.CancelEdit = true;
                    _currentAction = null;
                    MessageBox.Show(this, ex.Message, "Rename error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else if (_currentAction == "addFile")
            {
                try
                {
                    string parentPath = par.Name;

                    string fileName = e.Label;
                    bool modification = false;
                    if (!fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".sql";
                        modification = true;
                    }

                    int i = 1;
                    while (File.Exists($"{par.Name}\\{fileName}"))
                    {
                        fileName = string.Concat(fileName.AsSpan(0, fileName.Length - 4), $"{i++}.sql");
                        modification = true;
                    }
                    string filePath = $"{par.Name}\\{fileName}";
                    var f = File.CreateText(filePath);
                    f.Close();

                    e.Node.Text = fileName;
                    e.Node.Name = filePath;

                    _fileStructure[parentPath].contents.Add((filePath, DiscEntityType.fileObject));
                    _fileList.Add(filePath);
                    if (modification)
                    {
                        par.Collapse();
                    }

                }
                catch (Exception ex)
                {
                    e.CancelEdit = true;
                    _currentAction = null;
                    MessageBox.Show(this, ex.Message, "Rename error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (e.Label != null && e.Label != e.Node.Text && _currentAction == null) // = rename
            {
                string parentPath = par.Name;
                string newName = e.Label;
                string oldName = e.Node.Text;
                string newPath = $"{parentPath}\\{newName}";
                string oldPath = $"{parentPath}\\{oldName}";
                //string filePath = e.Node.Name; // = $"{parentPath}\\{oldName}"
                if (File.Exists($"{parentPath}\\{oldName}"))
                {
                    try
                    {
                        File.Move($"{parentPath}\\{oldName}", $"{parentPath}\\{newName}");
                        e.Node.Text = newName;
                        e.Node.Name = $"{parentPath}\\{newName}";

                        _fileList.Remove($"{parentPath}\\{oldName}");
                        _fileList.Add($"{parentPath}\\{newName}");

                        _fileStructure[parentPath].contents.Remove(($"{parentPath}\\{oldName}", DiscEntityType.fileObject));
                        _fileStructure[parentPath].contents.Add(($"{parentPath}\\{newName}", DiscEntityType.fileObject));

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Rename file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.CancelEdit = true;
                    }
                }
                else if (Directory.Exists($"{parentPath}\\{oldName}"))
                {
                    try
                    {
                        Directory.Move($"{parentPath}\\{oldName}", $"{parentPath}\\{newName}");
                        e.Node.Text = newName;
                        e.Node.Name = $"{parentPath}\\{newName}";
                        _folderList.Remove($"{parentPath}\\{oldName}");
                        _folderList.Add($"{parentPath}\\{newName}");


                        int n = _fileList.Count;
                        for (int i = 0; i < n; i++)
                        {
                            if (_fileList[i].StartsWith(oldPath))
                            {
                                _fileList[i] = string.Concat(newPath, _fileList[i].AsSpan(oldPath.Length));
                            }
                        }
                        n = _folderList.Count;
                        for (int i = 0; i < n; i++)
                        {
                            if (_folderList[i].StartsWith(oldPath))
                            {
                                _folderList[i] = string.Concat(newPath, _folderList[i].AsSpan(oldPath.Length));
                            }
                        }
                        _fileStructure[parentPath].contents.Remove((oldPath, DiscEntityType.directoryObject));
                        _fileStructure[parentPath].contents.Add((newPath, DiscEntityType.directoryObject));

                        Dictionary<string, (int level, List<(string name, DiscEntityType type)> contents)> strukturaPlikow2 = new Dictionary<string, (int level, List<(string name, DiscEntityType type)> contents)>();

                        foreach (var item in _fileStructure)
                        {
                            if (!item.Key.StartsWith(oldPath))
                            {
                                strukturaPlikow2[item.Key] = _fileStructure[item.Key];
                            }
                            else
                            {
                                string key = string.Concat(newPath, item.Key.AsSpan(oldPath.Length));
                                List<(string name, DiscEntityType type)> z1 = new List<(string name, DiscEntityType type)>();
                                for (int i = 0; i < _fileStructure[item.Key].contents.Count; i++)
                                {
                                    z1.Add((string.Concat(newPath, _fileStructure[item.Key].contents[i].name.AsSpan(oldPath.Length)), _fileStructure[item.Key].contents[i].type));
                                }
                                strukturaPlikow2[key] = new(_fileStructure[item.Key].level, z1);
                            }
                        }
                        this._fileStructure = strukturaPlikow2;

                        e.Node.Name = e.Node.FullPath;

                        List<TreeNode> ln1 = new List<TreeNode>();
                        foreach (TreeNode item in e.Node.Nodes)
                        {
                            ln1.Add(item);
                        }

                        int cnt = 0;
                        while (cnt < ln1.Count)
                        {
                            TreeNode node = ln1[cnt];
                            if (node.Name != "fool" && node.FullPath != node.Name)
                            {
                                node.Name = node.FullPath;
                            }
                            foreach (TreeNode item in node.Nodes)
                            {
                                ln1.Add(item);
                            }
                            cnt++;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Rename directory error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.CancelEdit = true;
                    }
                }
            }

            _currentAction = null;
        }


        private void InitializeTooltip()
        {
            _toolTip = new ToolTip
            {
                IsBalloon = true,
                ToolTipIcon = ToolTipIcon.Info,
                AutoPopDelay = 5000,
                InitialDelay = 1000,
                ReshowDelay = 500,
                ShowAlways = true
            };
            _toolTip.SetToolTip(this.panelSearchContainer, "Type a name to filter files. Press Enter to search file contents.");
            _toolTip.SetToolTip(this._textBoxExtensions, "Comma- or semicolon-separated extensions, for example: *.sql, *.cs");
            _toolTip.SetToolTip(this._buttonSearchOptions, "Search options");
            _toolTip.SetToolTip(this._buttonAddFolder, "Add folder to workspace");
        }

        private bool _isPlaceholderActive = true;
        private readonly string _placeholderText = "Search files...";

        private void InitializeSearchPlaceholder()
        {
            // Set initial placeholder state
            textBoxFileSearch.Text = _placeholderText;
            textBoxFileSearch.ForeColor = Color.Gray;
            _isPlaceholderActive = true;

            // Add event handlers for placeholder functionality
            textBoxFileSearch.Enter += TextBoxFileSearch_Enter;
            textBoxFileSearch.Leave += TextBoxFileSearch_Leave;
        }

        private void TextBoxFileSearch_Enter(object sender, EventArgs e)
        {
            if (_isPlaceholderActive)
            {
                textBoxFileSearch.Text = "";
                textBoxFileSearch.ForeColor = _colorTheme.TextBoxForeColor;
                _isPlaceholderActive = false;
            }
        }

        private void TextBoxFileSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxFileSearch.Text))
            {
                textBoxFileSearch.Text = _placeholderText;
                textBoxFileSearch.ForeColor = Color.Gray;
                _isPlaceholderActive = true;
            }
        }

        private void ApplyModernStyling()
        {
            _colorTheme.InitColors();
            BackColor = _colorTheme.MainBack;
            ForeColor = _colorTheme.MainFore;
            filesTreeView.BackColor = _colorTheme.TreeViewBackColor;
            filesTreeView.ForeColor = _colorTheme.TreeViewForeColor;
            filesTreeView.LineColor = _colorTheme.TreeViewLineColor;
            filesTreeView.BorderStyle = BorderStyle.None;
            panelSearchContainer.BackColor = _colorTheme.TextBoxBackColor;
            textBoxFileSearch.BackColor = _colorTheme.TextBoxBackColor;
            textBoxFileSearch.ForeColor = _colorTheme.TextBoxForeColor;
            textBoxFileSearch.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            _textBoxExtensions.BackColor = _colorTheme.TextBoxBackColor;
            _textBoxExtensions.ForeColor = _colorTheme.TextBoxForeColor;
            _textBoxExtensions.Font = textBoxFileSearch.Font;
            _searchFiltersPanel.BackColor = _colorTheme.MainBack;
            _searchOptionsMenu.Renderer = _colorTheme.GetRenderer();
            _searchStatus.ForeColor = _colorTheme.MainFore;
            _searchStatus.BackColor = _colorTheme.MainBack;
            labelSearchIcon.ForeColor = _colorTheme.TextBoxForeColor;
            foreach (CheckBox option in new[] { _checkWholeWord, _checkMatchCase, _checkUseRegex })
            {
                option.ForeColor = _colorTheme.MainFore;
                option.BackColor = _colorTheme.MainBack;
            }
            foreach (Button button in new[] { _buttonSearchOptions, _buttonClearSearch, _buttonAddFolder })
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.BackColor = _colorTheme.TextBoxBackColor;
                button.ForeColor = _colorTheme.TextBoxForeColor;
                button.Font = textBoxFileSearch.Font;
            }

            if (!_stylingHooksAdded)
            {
                filesTreeView.Paint += TreeViewPliki_Paint;
                panelSearchContainer.Paint += PanelSearchContainer_Paint;
                panelSearchContainer.Resize += (_, _) => CenterTextBoxVertically();
                textBoxFileSearch.Enter += (_, _) => panelSearchContainer.Invalidate();
                textBoxFileSearch.Leave += (_, _) => panelSearchContainer.Invalidate();
                _stylingHooksAdded = true;
            }

            CenterTextBoxVertically();
            ApplyDpiMetrics();
        }

        private void TreeViewPliki_Paint(object sender, PaintEventArgs e)
        {
            // Draw a subtle border around the TreeView
            var rect = new Rectangle(0, 0, filesTreeView.Width - 1, filesTreeView.Height - 1);
            Color border = _colorTheme.IsDark(_colorTheme.TreeViewBackColor)
                ? Color.FromArgb(90, _colorTheme.TreeViewForeColor)
                : Color.FromArgb(150, _colorTheme.TreeViewForeColor);
            using (var pen = new Pen(border, Math.Max(1, DpiScale.Scale(1, DeviceDpi))))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void PanelSearchContainer_Paint(object sender, PaintEventArgs e)
        {
            // Draw a modern border around the search container
            var rect = new Rectangle(0, 0, panelSearchContainer.Width - 1, panelSearchContainer.Height - 1);
            Color borderColor = textBoxFileSearch.Focused
                ? _colorTheme.TextBoxForeColor
                : Color.FromArgb(110, _colorTheme.TextBoxForeColor);

            using (var pen = new Pen(borderColor, DpiScale.Scale(textBoxFileSearch.Focused ? 2 : 1, DeviceDpi)))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void CenterTextBoxVertically()
        {
            if (panelSearchContainer != null && textBoxFileSearch != null)
            {
                int containerHeight = panelSearchContainer.ClientSize.Height;
                int textBoxHeight = textBoxFileSearch.Height;
                int topPosition = (containerHeight - textBoxHeight) / 2;

                // Ensure minimum top position
                if (topPosition < 2) topPosition = 2;

                textBoxFileSearch.Top = topPosition;
            }
        }

        private int DELAY_TIME = 500;
        private Dictionary<string, (int level, List<(string name, DiscEntityType type)> contents)> _fileStructure = new Dictionary<string, (int level, List<(string name, DiscEntityType type)> contents)>();
        private readonly List<String> _fileList = new List<string>();
        private readonly List<String> _folderList = new List<string>();
        private readonly List<FileSystemWatcher> _searchFileWatcher = new List<FileSystemWatcher>();

        private void FileSearchAction()
        {
            string searchTerm = textBoxFileSearch.Text;
            var patterns = _fileSearchEngine.NormalizeExtensionPatterns(_textBoxExtensions.Text);
            bool IsIncluded(string path) => patterns.Count == 0 || patterns.Any(pattern => string.Equals(Path.GetExtension(path), pattern, StringComparison.OrdinalIgnoreCase));
            var matchingFiles = _fileList.FindAll(arg => IsIncluded(arg) && arg.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();
            var matchingFolders = _folderList.FindAll(arg => arg.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();

            if (matchingFiles.Count < 200 || matchingFolders.Count < 200)
            {
                filesTreeView.Nodes.Clear();
            }

            filesTreeView.BeginUpdate();
            if (matchingFiles.Count < 200)
            {
                foreach (var item in matchingFiles)
                {
                    var n1 = filesTreeView.Nodes.Add(item, item);
                    n1.ImageIndex = 1;
                    n1.SelectedImageIndex = 1;
                }
            }

            if (matchingFolders.Count < 200)
            {
                foreach (var item in matchingFolders)
                {
                    var n1 = filesTreeView.Nodes.Add(item, item);
                    n1.ImageIndex = 0;
                    n1.SelectedImageIndex = 0;
                }
            }
            filesTreeView.EndUpdate();
        }

        private void ScheduleFilenameSearch()
        {
            if (_isPlaceholderActive || textBoxFileSearch.Text.Length < 3)
                return;

            _typingSearchTimer ??= new System.Windows.Forms.Timer { Interval = DELAY_TIME };
            _typingSearchTimer.Stop();
            _typingSearchTimer.Tag = textBoxFileSearch.Text;
            _typingSearchTimer.Start();
        }

        private void UpdateSearchStatus()
        {
            if (_searchStatus is null)
                return;

            var modes = new List<string>();
            if (_checkWholeWord.Checked) modes.Add("whole word");
            if (_checkMatchCase.Checked) modes.Add("case-sensitive");
            if (_checkUseRegex.Checked) modes.Add("regex");
            _searchStatus.Text = modes.Count == 0 ? "Fragment search · Enter to search contents" : string.Join(" · ", modes);
        }

        private IReadOnlyList<string> BuildSearchCandidates(IReadOnlyList<string> patterns)
        {
            var candidates = new HashSet<string>(_fileList, StringComparer.OrdinalIgnoreCase);
            foreach (string root in _filesViewModel.RootPaths)
            {
                try
                {
                    foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    {
                        string extension = Path.GetExtension(path);
                        if (patterns.Any(pattern => string.Equals(extension, pattern, StringComparison.OrdinalIgnoreCase)))
                            candidates.Add(path);
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }
            }
            return candidates.ToArray();
        }

        private async Task SearchContentAsync()
        {
            string query = textBoxFileSearch.Text.Trim();
            if (query.Length == 0)
            {
                await AddDirs(_filesViewModel.RootPaths.ToList(), true);
                return;
            }

            filesTreeView.Enabled = false;
            _buttonSearchOptions.Enabled = false;
            _buttonClearSearch.Enabled = false;
            _searchStatus.Text = "Searching…";
            try
            {
                _filesViewModel.SearchQuery = query;
                _filesViewModel.ExtensionPatterns = _textBoxExtensions.Text;
                _filesViewModel.MatchWholeWord = _checkWholeWord.Checked;
                _filesViewModel.MatchCase = _checkMatchCase.Checked;
                _filesViewModel.UseRegex = _checkUseRegex.Checked;
                _filesViewModel.SearchTimeout = TimeSpan.FromMilliseconds(Math.Max(1, _applicationSettingsContext.Config.FileSearchTimeout));
                await _filesViewModel.SearchAsync(_searchFileCancellation?.Token ?? CancellationToken.None);
                var outcome = _filesViewModel.LastSearch;
                RenderContentResults(new AppBase.Services.Utilities.FileSearchOutcome(
                    outcome.Files.Select(file => new AppBase.Services.Utilities.FileSearchFileResult(
                        file.Path,
                        file.Matches.Select(match => new AppBase.Services.Utilities.FileSearchMatch(
                            match.LineNumber, match.LineText, match.MatchIndex, match.MatchLength)).ToArray(),
                        file.IsTruncated)).ToArray(),
                    outcome.WasCancelled,
                    outcome.WasTruncated,
                    outcome.MatchCount));
            }
            catch (ArgumentException ex)
            {
                filesTreeView.Nodes.Clear();
                _searchStatus.Text = $"Invalid search pattern: {ex.Message}";
            }
            catch (RegexMatchTimeoutException)
            {
                filesTreeView.Nodes.Clear();
                _searchStatus.Text = "Search pattern exceeded the time limit.";
            }
            catch (Exception ex)
            {
                _searchStatus.Text = ex.Message;
            }
            finally
            {
                filesTreeView.Enabled = true;
                _buttonSearchOptions.Enabled = true;
                _buttonClearSearch.Enabled = true;
            }
        }

        private void RenderContentResults(FileSearchOutcome outcome)
        {
            filesTreeView.BeginUpdate();
            filesTreeView.Nodes.Clear();
            _serachMode = true;
            foreach (var file in outcome.Files)
            {
                string fileName = Path.GetFileName(file.Path);
                string directory = Path.GetDirectoryName(file.Path) ?? string.Empty;
                var fileNode = filesTreeView.Nodes.Add(file.Path, $"{fileName}  ·  {file.Matches.Count} match(es)");
                fileNode.Name = file.Path;
                fileNode.ImageIndex = 1;
                fileNode.SelectedImageIndex = 1;
                fileNode.ToolTipText = directory;
                foreach (var match in file.Matches)
                {
                    var lineNode = fileNode.Nodes.Add($"line:{match.LineNumber}", $"{match.LineNumber}: {CreateSnippet(match.LineText, textBoxFileSearch.Text)}");
                    lineNode.Tag = match.LineNumber;
                }
            }
            if (outcome.Files.Count == 0)
                filesTreeView.Nodes.Add("no results", "No results");
            filesTreeView.EndUpdate();
            _searchStatus.Text = outcome.Files.Count == 0
                ? "No results"
                : $"{outcome.Files.Count} file(s) · {outcome.MatchCount} match(es)" + (outcome.WasCancelled || outcome.WasTruncated ? " · results limited" : string.Empty);
        }

        private System.Windows.Forms.Timer _typingSearchTimer = null;

        private void textBoxFileSearch_TextChanged(object sender, EventArgs e)
        {
            // Don't process changes when placeholder is active
            if (_isPlaceholderActive)
                return;

            if (_typingSearchTimer == null)//inicjacja zegara
            {
                _typingSearchTimer = new System.Windows.Forms.Timer();
                _typingSearchTimer.Interval = DELAY_TIME;
                _typingSearchTimer.Tick += TypingSearchTimer_Tick;
            }
            _typingSearchTimer.Stop(); // Resets the timer
            _typingSearchTimer.Tag = (sender as TextBox).Text; // This should be done with EventArgs
            _typingSearchTimer.Start();
        }

        private bool _serachMode = false;
        private void TypingSearchTimer_Tick(object sender, EventArgs e)
        {
            _typingSearchTimer.Stop();

            // Don't process when placeholder is active
            if (_isPlaceholderActive)
                return;

            if (textBoxFileSearch.Text.Length < 3)
            {
                try
                {
                    ShowRootFolders(czyKasowac: true);
                    _serachMode = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Refresh folders error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (textBoxFileSearch.Text.Length >= 3)
            {
                FileSearchAction();
            }
        }

        /// <summary>
        /// Refreshes the visual styling of the control. Call this when the theme changes.
        /// </summary>
        public void RefreshStyling()
        {
            ApplyModernStyling();
            filesTreeView.ContextMenuStrip.Renderer = _colorTheme.GetRenderer();
            this.Invalidate(true);
        }

        private void ShowRootFolders(bool czyKasowac = false)
        {
            filesTreeView.Invoke(() =>
            {
                filesTreeView.BeginUpdate();
                if (czyKasowac)
                {
                    filesTreeView.Nodes.Clear();
                }
                foreach (var item in _fileStructure.Keys)
                {
                    if (_fileStructure[item].level == 0)
                    {
                        TreeNode n = filesTreeView.Nodes.Add(item, item);
                        n.Nodes.Add("fool", "fool");
                        n.ImageIndex = 0;
                        n.SelectedImageIndex = 0;
                    }
                }
                filesTreeView.EndUpdate();
            });
        }
    }
}
