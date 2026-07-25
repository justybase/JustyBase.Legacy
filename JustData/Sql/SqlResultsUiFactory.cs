using AppBase.Common;
using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using JustyBase.NetezzaSqlParser.Linter;
using JustData.Application.Sql;
using JustData.Application.Editor;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.Forms;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.UI.Models;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace JustyBaseLegacy.UI.Sql;

/// <summary>
/// WinForms capabilities used by the results UI factory. This preserves the
/// DockSuite-specific rendering boundary without coupling the factory to the
/// shell form that hosts it.
/// </summary>
internal interface ISqlResultsUiView
{
    int SqlResultsDeviceDpi { get; }
    Font SqlResultsUiFont { get; }
    FastColoredTextBox SqlResultsCurrentEditor { get; }
    IUiHelperService SqlResultsUiHelper { get; }
    IColorTheme SqlResultsColorTheme { get; }
    IApplicationSettingsContext SqlResultsWindowSettings { get; }
    ContextMenuStrip SqlResultsContextMenu { get; }
    Image SqlResultsNormalCloseImage { get; }
    Image SqlResultsActivePinImage { get; }
    TabControlDrawingHandler SqlResultsTabDrawingHandler { get; }
    Dictionary<EditorDocumentId, (FastColoredTextBox Editor, DataGridView Grid)> SqlResultsLintDiagnosticsTargets { get; }
    void OnSqlResultsTabKeyDown(object sender, KeyEventArgs e);
    void OnSqlResultSelectionChanged(TabPage? selectedTab);
    void DisableSqlLintRule(string ruleId);
    void EnableSqlLintRule(string ruleId);
    bool IsLintEditorHighlightShown { get; }
    void ToggleSqlLintEditorHighlight();
    void DisableSqlLintRuleHighlight(string ruleId);
    void EnableSqlLintRuleHighlight(string ruleId);
    SplitContainer? GetSqlResultsSplitContainerForTab(TabPage tabPage);
    TabPage? FindSqlResultsTabForDocument(EditorDocumentId documentId);
    DockContent? EnsureSqlResultsToolWindow();
    TabControl? GetSqlResultsTabControl(TabPage tabPage);
    TabPage? FindSqlResultsTabForSplitContainer(SplitContainer splitter);
    void AttachTabPage(TabPage page, TabControl tabControl);
    void RemoveTabData(TabControl tabControl, int index = -1);
}

internal sealed class SqlResultsUiFactory
{
    private readonly ISqlResultsUiView _window;
    private readonly Dictionary<DataGridView, IReadOnlyList<SqlDiagnostic>> _issuesByGrid = new();
    private readonly Dictionary<DataGridView, (ComboBox Filter, TextBox Search)> _diagnosticsControls = new();
    private readonly Dictionary<DataGridView, Button> _highlightButtons = new();

    public SqlResultsUiFactory(ISqlResultsUiView window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public void LayoutResultsToolbar(Control parent, Button btAbort, ProgressBar progressBarSQL, Control logView = null)
    {
        ResultsToolbarMetrics.Layout(parent, btAbort, progressBarSQL, logView, parent.DeviceDpi);
    }

    public TabControl EnsureResultsTabControl(SplitContainer containerX)
    {
        _window.EnsureSqlResultsToolWindow();

        var tabPage = _window.FindSqlResultsTabForSplitContainer(containerX);
        if (tabPage is null)
            throw new InvalidOperationException("The editor tab must be registered before its results are initialized.");

        var perTabTc = _window.GetSqlResultsTabControl(tabPage);
        containerX.Tag ??= new ResultData { TabControlSQLResults = perTabTc };

        if (containerX.Tag is ResultData rd && rd.DiagnosticsGrid is null)
        {
            EnsureDiagnosticsTab(perTabTc, rd);
        }

        // Configure context menu, custom draw, and drag-reorder once.
        if (perTabTc.ContextMenuStrip is null)
        {
            perTabTc.ContextMenuStrip = _window.SqlResultsContextMenu;
            perTabTc.DrawMode = TabDrawMode.OwnerDrawFixed;
            perTabTc.DrawItem += _window.SqlResultsTabDrawingHandler.TabControlResults_DrawItem;
            perTabTc.MouseMove += _window.SqlResultsTabDrawingHandler.TabControlResults_MouseMove;
            perTabTc.MouseLeave += _window.SqlResultsTabDrawingHandler.Tc_MouseLeave;
            perTabTc.KeyDown += _window.OnSqlResultsTabKeyDown;
            perTabTc.SelectedIndexChanged += (_, _) =>
                _window.OnSqlResultSelectionChanged(perTabTc.SelectedTab);
            perTabTc.DpiChangedAfterParent += ResultsTabControl_DpiChangedAfterParent;
            ApplyResultsTabMetrics(perTabTc);
            EnableTabReorder(perTabTc);
        }

        // Collapse the per-tab Panel2 — results live in the shared dock window.
        containerX.Panel2Collapsed = true;
        return perTabTc;
    }

    private void ResultsTabControl_DpiChangedAfterParent(object sender, EventArgs e)
    {
        if (sender is TabControl tabControl)
        {
            ApplyResultsTabMetrics(tabControl);
        }
    }

    private void ApplyResultsTabMetrics(TabControl tabControl)
    {
        int dpi = tabControl.DeviceDpi;
        Font font = tabControl.Font ?? _window.SqlResultsUiFont;
        tabControl.Padding = TabIconLayout.ResultsTabPadding(dpi);
        tabControl.ItemSize = new Size(0, TabIconLayout.ResultsTabHeight(font, dpi));
        tabControl.Invalidate();
    }

    // ── Tab reorder via drag-and-drop ────────────────────────

    private void EnableTabReorder(TabControl tc)
    {
        tc.AllowDrop = true;
        tc.MouseDown += TabReorder_MouseDown;
        tc.DragOver += TabReorder_DragOver;
        tc.DragDrop += TabReorder_DragDrop;
    }

    private TabPage? _dragTab;

    private void TabReorder_MouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is not TabControl tc || e.Button != MouseButtons.Left)
            return;

        _dragTab = null;
        int dpi = tc.DeviceDpi;
        for (int i = 0; i < tc.TabCount; i++)
        {
            if (tc.GetTabRect(i).Contains(e.Location))
            {
                _dragTab = tc.TabPages[i];
                break;
            }
        }

        if (_dragTab is null)
            return;

        if (_dragTab.Tag is TabPageResultsTag { IsPermanentDiagnostics: true })
        {
            _dragTab = null;
            return;
        }

        int idx = tc.TabPages.IndexOf(_dragTab);
        if (idx < 0)
            return;

        Rectangle tabRect = tc.GetTabRect(idx);
        Rectangle closeHit = TabIconLayout.HitRect(TabIconLayout.CloseIconRect(tabRect, dpi), dpi);
        Rectangle pinHit = TabIconLayout.HitRect(TabIconLayout.PinIconRect(tabRect, dpi), dpi);
        Point p = e.Location;

        if (closeHit.Contains(p))
        {
            _dragTab = null;
            _window.RemoveTabData(tc, idx);
            return;
        }

        if (pinHit.Contains(p))
        {
            _window.AttachTabPage(_dragTab, tc);
            _dragTab = null;
            return;
        }

        tc.DoDragDrop(_dragTab, DragDropEffects.Move);
    }

    private void TabReorder_DragOver(object? sender, DragEventArgs e)
    {
        if (sender is not TabControl tc || _dragTab is null)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        if (!e.Data.GetDataPresent(typeof(TabPage)))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        e.Effect = DragDropEffects.Move;

        Point pt = tc.PointToClient(new Point(e.X, e.Y));
        for (int i = 0; i < tc.TabCount; i++)
        {
            if (tc.GetTabRect(i).Contains(pt))
            {
                int dragIdx = tc.TabPages.IndexOf(_dragTab);
                if (dragIdx >= 0 && dragIdx != i)
                {
                    SuspendLayoutReorder(tc, dragIdx, i);
                }
                break;
            }
        }
    }

    private void TabReorder_DragDrop(object? sender, DragEventArgs e)
    {
        _dragTab = null;
    }

    private static void SuspendLayoutReorder(TabControl tc, int fromIdx, int toIdx)
    {
        TabPage selected = tc.SelectedTab;
        tc.SuspendLayout();
        try
        {
            var pages = tc.TabPages.Cast<TabPage>().ToList();
            TabPage drag = pages[fromIdx];
            pages.RemoveAt(fromIdx);
            pages.Insert(toIdx > fromIdx ? toIdx - 1 : toIdx, drag);

            for (int i = 0; i < pages.Count; i++)
            {
                if (tc.TabPages[i] != pages[i])
                    tc.TabPages[i] = pages[i];
            }

            if (selected is not null && tc.TabPages.Contains(selected))
                tc.SelectedTab = selected;
        }
        finally
        {
            tc.ResumeLayout(true);
            tc.Invalidate();
        }
    }

    public void EnsureDiagnosticsTab(TabControl tc, ResultData? resultData)
    {
        if (resultData?.DiagnosticsGrid is not null && !resultData.DiagnosticsGrid.IsDisposed)
        {
            return;
        }

        foreach (TabPage page in tc.TabPages)
        {
            if (page.Tag is TabPageResultsTag tag && tag.IsPermanentDiagnostics)
            {
                if (resultData is not null)
                    resultData.DiagnosticsGrid = page.Controls.OfType<DataGridView>().FirstOrDefault();
                return;
            }
        }

        var diagnosticsTab = new TabPagePicture
        {
            Text = "Diagnostics",
            CloseImage = _window.SqlResultsNormalCloseImage,
            PinImage = _window.SqlResultsActivePinImage
        };
        diagnosticsTab.Tag = new TabPageResultsTag
        {
            Docked = true,
            IsPermanentDiagnostics = true,
            ParentControl = tc,
            HasDiagnostics = true
        };

        var diagnosticsGrid = PrepareDiagnosticsGrid(diagnosticsTab);
        var toolbar = CreateDiagnosticsToolbar(diagnosticsGrid);
        var container = new Panel { Dock = DockStyle.Fill };
        container.Controls.Add(toolbar);
        container.Controls.Add(diagnosticsGrid);
        diagnosticsTab.Controls.Add(container);
        tc.TabPages.Insert(0, diagnosticsTab);
        resultData.DiagnosticsGrid = diagnosticsGrid;
    }

    private Panel CreateDiagnosticsToolbar(DataGridView grid)
    {
        int dpi = _window.SqlResultsDeviceDpi;
        int comboWidth = DpiScale.Scale(90, dpi);
        int searchWidth = DpiScale.Scale(200, dpi);
        int controlHeight = DpiScale.Scale(24, dpi);

        bool isDark = _window.SqlResultsWindowSettings.Config.UseSpecialColoring
            && _window.SqlResultsColorTheme.IsDark(_window.SqlResultsColorTheme.GridViewDefaultCellStyleBackColor);

        Color backColor = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
        Color foreColor = isDark ? Color.FromArgb(241, 241, 241) : SystemColors.ControlText;
        Color controlBack = isDark ? Color.FromArgb(45, 45, 45) : SystemColors.Window;
        Color controlFore = isDark ? Color.FromArgb(220, 220, 220) : SystemColors.WindowText;

        var filterCombo = new ComboBox
        {
            Name = "diagnosticsSeverityFilter",
            DropDownStyle = ComboBoxStyle.DropDownList,
            Items = { "All", "Error", "Warning", "Info", "Hint" },
            SelectedIndex = 0,
            Width = comboWidth,
            Height = controlHeight,
            BackColor = controlBack,
            ForeColor = controlFore
        };

        var searchBox = new TextBox
        {
            Name = "diagnosticsSearchBox",
            Text = "",
            Width = searchWidth,
            Height = controlHeight,
            BackColor = controlBack,
            ForeColor = controlFore
        };

        var highlightBtn = new Button
        {
            Name = "diagnosticsHighlightBtn",
            Text = _window.IsLintEditorHighlightShown ? "Highlight: ON" : "Highlight: OFF",
            Width = DpiScale.Scale(100, dpi),
            Height = controlHeight,
            BackColor = controlBack,
            ForeColor = controlFore,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderColor = Color.FromArgb(100, 100, 100) },
            Margin = new Padding(DpiScale.Scale(8, dpi), 0, 0, 0)
        };
        highlightBtn.Click += (_, _) =>
        {
            _window.ToggleSqlLintEditorHighlight();
            highlightBtn.Text = _window.IsLintEditorHighlightShown ? "Highlight: ON" : "Highlight: OFF";
        };

        _diagnosticsControls[grid] = (filterCombo, searchBox);
        _highlightButtons[grid] = highlightBtn;
        filterCombo.SelectedIndexChanged += (_, _) => ApplyGridFilter(grid);
        searchBox.TextChanged += (_, _) => ApplyGridFilter(grid);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = controlHeight + DpiScale.Scale(4, dpi),
            Padding = new Padding(DpiScale.Scale(4, dpi), 0, 0, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            BackColor = backColor,
            ForeColor = foreColor
        };
        toolbar.Controls.Add(new Label
        {
            Text = "Severity:",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = controlHeight,
            ForeColor = foreColor,
            BackColor = backColor
        });
        toolbar.Controls.Add(filterCombo);
        toolbar.Controls.Add(new Label
        {
            Text = "Search:",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = controlHeight,
            ForeColor = foreColor,
            BackColor = backColor,
            Margin = new Padding(DpiScale.Scale(8, dpi), 0, 0, 0)
        });
        toolbar.Controls.Add(searchBox);
        toolbar.Controls.Add(highlightBtn);

        return toolbar;
    }

    private void ApplyGridFilter(DataGridView grid)
    {
        if (!_issuesByGrid.TryGetValue(grid, out IReadOnlyList<SqlDiagnostic>? issues))
            issues = Array.Empty<SqlDiagnostic>();

        (ComboBox Filter, TextBox Search) controls = _diagnosticsControls.TryGetValue(grid, out var value)
            ? value
            : (null!, null!);
        string severityFilter = controls.Filter?.SelectedItem as string ?? "All";
        string searchText = controls.Search?.Text?.Trim() ?? "";

        var filtered = issues.AsEnumerable();

        if (!string.Equals(severityFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            var targetSeverity = severityFilter switch
            {
                "Error" => SqlDiagnosticSeverity.Error,
                "Warning" => SqlDiagnosticSeverity.Warning,
                "Info" => SqlDiagnosticSeverity.Information,
                "Hint" => SqlDiagnosticSeverity.Hint,
                _ => (SqlDiagnosticSeverity)(-1)
            };
            filtered = filtered.Where(i => i.Severity == targetSeverity);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(i =>
                i.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (i.Code ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        var editor = grid.Tag as FastColoredTextBox;
        PopulateGrid(grid, filtered.ToList(), editor);
    }

    public DataGridView PrepareDiagnosticsGrid(TabPage diagnosticsTab)
    {
        int dpi = _window.SqlResultsDeviceDpi;
        int[] columnWidths = { 70, 80, 420, 60, 60 };
        var grid = new ThemedDataGridView
        {
            Name = "diagnosticsGrid",
            Dock = DockStyle.Fill,
            RowHeadersVisible = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ShowCellToolTips = true,
            EnableHeadersVisualStyles = false
        };

        var disableBtnCol = new DataGridViewButtonColumn
        {
            Name = "disableBtn",
            HeaderText = "Off",
            Width = DpiScale.Scale(50, dpi),
            FlatStyle = FlatStyle.Flat,
            UseColumnTextForButtonValue = false
        };
        grid.Columns.Insert(0, disableBtnCol);
        grid.CellClick += DiagnosticsGrid_CellClick;

        grid.Columns.Add("severity", "Severity");
        grid.Columns[1].Width = DpiScale.Scale(columnWidths[0], dpi);
        grid.Columns.Add("ruleId", "Rule");
        grid.Columns[2].Width = DpiScale.Scale(columnWidths[1], dpi);
        grid.Columns.Add("message", "Message");
        grid.Columns[3].Width = DpiScale.Scale(columnWidths[2], dpi);
        grid.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        grid.Columns.Add("line", "Line");
        grid.Columns[4].Width = DpiScale.Scale(columnWidths[3], dpi);
        grid.Columns.Add("column", "Col");
        grid.Columns[5].Width = DpiScale.Scale(columnWidths[4], dpi);

        _window.SqlResultsUiHelper.DoubleBufDateGridView(grid);
        _window.SqlResultsColorTheme.ColorDataGridView(grid);
        grid.AlternatingRowsDefaultCellStyle.BackColor = grid.DefaultCellStyle.BackColor;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = grid.DefaultCellStyle.ForeColor;
        GridDpiMetrics.Apply(grid, dpi, paddingLogical: 10);
        GridThemingHelper.EnableDarkScrollbars(grid, _window.SqlResultsWindowSettings.Config.UseSpecialColoring);
        grid.CellDoubleClick += DiagnosticsGrid_CellDoubleClick;
        grid.Disposed += (_, _) =>
        {
            _issuesByGrid.Remove(grid);
            _diagnosticsControls.Remove(grid);
        };
        ConfigureDiagnosticsContextMenu(grid);
        return grid;
    }

    private void ConfigureDiagnosticsContextMenu(DataGridView grid)
    {
        var menu = new ContextMenuStrip();
        var disableRule = new ToolStripMenuItem();
        var disableHighlight = new ToolStripMenuItem();
        var separator1 = new ToolStripSeparator();
        var enableRules = new ToolStripMenuItem("Re-enable disabled rule");
        var enableHighlightRules = new ToolStripMenuItem("Re-enable highlighting for rule");
        var separator2 = new ToolStripSeparator();
        SqlDiagnostic? contextIssue = null;
        menu.Items.Add(disableRule);
        menu.Items.Add(disableHighlight);
        menu.Items.Add(separator1);
        menu.Items.Add(enableRules);
        menu.Items.Add(enableHighlightRules);
        menu.Items.Add(separator2);

        grid.ContextMenuStrip = menu;
        grid.Disposed += (_, _) => menu.Dispose();
        menu.Closed += (_, _) => contextIssue = null;
        grid.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var hit = grid.HitTest(e.X, e.Y);
            contextIssue = hit.RowIndex >= 0 ? grid.Rows[hit.RowIndex].Tag as SqlDiagnostic : null;
            if (contextIssue is not null)
            {
                grid.ClearSelection();
                grid.Rows[hit.RowIndex].Selected = true;
                grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
            }
        };

        menu.Opening += (_, e) =>
        {
            if (contextIssue is null && grid.CurrentRow?.Tag is SqlDiagnostic selectedIssue)
                contextIssue = selectedIssue;

            bool canDisable = !string.IsNullOrWhiteSpace(contextIssue?.Code);
            disableRule.Visible = canDisable;
            if (canDisable)
            {
                disableRule.Tag = contextIssue!.Code;
                disableRule.Text = $"Disable rule '{contextIssue.Code}'";
            }

            bool canHighlight = canDisable;
            var disabledHighlight = _window.SqlResultsWindowSettings.Config.DisabledHighlightRules;
            if (canHighlight)
            {
                bool alreadyHidden = disabledHighlight?.Contains(contextIssue!.Code!, StringComparer.OrdinalIgnoreCase) == true;
                disableHighlight.Tag = contextIssue!.Code;
                disableHighlight.Text = alreadyHidden
                    ? $"Enable highlighting for rule '{contextIssue.Code}'"
                    : $"Disable highlighting for rule '{contextIssue.Code}'";
            }
            disableHighlight.Visible = canHighlight;

            enableRules.DropDownItems.Clear();
            foreach (string ruleId in _window.SqlResultsWindowSettings.Config.DisabledLintRules ?? [])
            {
                var enableRule = new ToolStripMenuItem(ruleId);
                enableRule.Click += (_, _) => _window.EnableSqlLintRule(ruleId);
                enableRules.DropDownItems.Add(enableRule);
            }
            enableRules.Visible = enableRules.DropDownItems.Count > 0;

            enableHighlightRules.DropDownItems.Clear();
            foreach (string ruleId in disabledHighlight ?? [])
            {
                var enableHighlight = new ToolStripMenuItem(ruleId);
                enableHighlight.Click += (_, _) => _window.EnableSqlLintRuleHighlight(ruleId);
                enableHighlightRules.DropDownItems.Add(enableHighlight);
            }
            enableHighlightRules.Visible = enableHighlightRules.DropDownItems.Count > 0;

            separator1.Visible = disableRule.Visible || disableHighlight.Visible;
            separator2.Visible = enableRules.Visible || enableHighlightRules.Visible;
            e.Cancel = !disableRule.Visible && !disableHighlight.Visible && !enableRules.Visible && !enableHighlightRules.Visible;
        };

        disableRule.Click += (_, _) =>
        {
            if (disableRule.Tag is string ruleId)
            {
                _window.DisableSqlLintRule(ruleId);
                MessageBox.Show(
                    "Full effect requires application restart.\r\nPełny efekt wymaga restartu aplikacji.",
                    "Rule Disabled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        };

        disableHighlight.Click += (_, _) =>
        {
            if (disableHighlight.Tag is string ruleId)
            {
                var disabledHighlightRules = _window.SqlResultsWindowSettings.Config.DisabledHighlightRules;
                bool alreadyHidden = disabledHighlightRules?.Contains(ruleId, StringComparer.OrdinalIgnoreCase) == true;
                if (alreadyHidden)
                    _window.EnableSqlLintRuleHighlight(ruleId);
                else
                    _window.DisableSqlLintRuleHighlight(ruleId);
            }
        };
    }

    public ISqlExecutionLog PrepareSqlLog(TabPagePicture currentResultsTab, Button btAbort)
    {
        var log = new SqlExecutionLogControl
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };
        log.ApplyTheme(_window.SqlResultsColorTheme);
        log.SetErrorBackColor(MyColors.LogErrorStdColor);
        LayoutResultsToolbar(currentResultsTab, btAbort, null, log.View);
        return log;
    }

    public void RegisterDiagnosticsTarget(EditorDocumentId documentId, FastColoredTextBox editor)
    {
        if (editor is null)
        {
            return;
        }

        if (_window.FindSqlResultsTabForDocument(documentId) is not TabPage selectedTab)
        {
            return;
        }

        var splitContainer = _window.GetSqlResultsSplitContainerForTab(selectedTab);
        if (splitContainer is null)
        {
            return;
        }

        EnsureResultsTabControl(splitContainer);
        if (splitContainer.Tag is not ResultData resultData || resultData.DiagnosticsGrid is null)
        {
            return;
        }

        _window.SqlResultsLintDiagnosticsTargets[documentId] = (editor, resultData.DiagnosticsGrid);
    }

    public void OnDocumentDiagnosticsChanged(
        EditorDocumentId documentId,
        IReadOnlyList<SqlDiagnostic> diagnostics)
    {
        if (!_window.SqlResultsLintDiagnosticsTargets.TryGetValue(documentId, out var target))
            return;
        if (target.Grid.IsDisposed || target.Editor.IsDisposed)
        {
            _window.SqlResultsLintDiagnosticsTargets.Remove(documentId);
            return;
        }

        UpdateDiagnosticsGrid(target.Grid, target.Editor, diagnostics);
    }

    public void UpdateDiagnosticsGrid(DataGridView grid, FastColoredTextBox editor, IReadOnlyList<SqlDiagnostic> issues)
    {
        if (grid.InvokeRequired)
        {
            grid.BeginInvoke(() => UpdateDiagnosticsGrid(grid, editor, issues));
            return;
        }

        _issuesByGrid[grid] = issues;
        grid.Tag = editor;
        ApplyGridFilter(grid);
    }

    private void PopulateGrid(DataGridView grid, IReadOnlyList<SqlDiagnostic> issues, FastColoredTextBox? editor = null)
    {
        grid.Rows.Clear();
        bool isDark = IsDarkThemeActive;
        var disabledRules = _window.SqlResultsWindowSettings.Config.DisabledLintRules;

        foreach (var issue in issues.OrderBy(i => (int)i.Severity).ThenBy(i => i.StartOffset))
        {
            string message = issue.Message;
            if (!string.IsNullOrWhiteSpace(issue.Code)
                && message.StartsWith(issue.Code + ": ", StringComparison.OrdinalIgnoreCase))
            {
                message = message[(issue.Code.Length + 2)..];
            }

            bool isDisabled = issue.Code is not null
                && disabledRules?.Contains(issue.Code, StringComparer.OrdinalIgnoreCase) == true;

            int rowIndex = grid.Rows.Add(
                isDisabled ? "Enable" : "Off",
                SeverityLabel(issue.Severity),
                issue.Code,
                message,
                GetLine(editor, issue.StartOffset),
                GetColumn(editor, issue.StartOffset));
            grid.Rows[rowIndex].Tag = issue;
            ApplyDiagnosticRowStyle(grid.Rows[rowIndex], issue.Severity, isDark);
        }

        if (editor is not null)
            grid.Tag = editor;
    }

    public void RefreshAllDiagnosticsGrids()
    {
        bool isDark = IsDarkThemeActive;
        bool dark = _window.SqlResultsWindowSettings.Config.UseSpecialColoring;
        foreach (DataGridView grid in _window.SqlResultsLintDiagnosticsTargets.Values
            .Select(target => target.Grid)
            .Where(grid => !grid.IsDisposed)
            .Distinct()
            .ToArray())
        {
            _window.SqlResultsColorTheme.ColorDataGridView(grid);
            grid.AlternatingRowsDefaultCellStyle.BackColor = grid.DefaultCellStyle.BackColor;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = grid.DefaultCellStyle.ForeColor;
            GridThemingHelper.ApplyScrollbarTheme(grid, dark);

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Tag is SqlDiagnostic issue)
                    ApplyDiagnosticRowStyle(row, issue.Severity, isDark);
            }

            grid.Invalidate();
        }
    }

    private bool IsDarkThemeActive =>
        _window.SqlResultsWindowSettings.Config.UseSpecialColoring
        && _window.SqlResultsColorTheme.IsDark(_window.SqlResultsColorTheme.GridViewDefaultCellStyleBackColor);

    private static string SeverityLabel(SqlDiagnosticSeverity severity) => severity switch
    {
        SqlDiagnosticSeverity.Error => "Error",
        SqlDiagnosticSeverity.Warning => "Warning",
        SqlDiagnosticSeverity.Information => "Info",
        SqlDiagnosticSeverity.Hint => "Hint",
        _ => severity.ToString()
    };

    private static void ApplyDiagnosticRowStyle(DataGridViewRow row, SqlDiagnosticSeverity severity, bool isDarkTheme)
    {
        DiagnosticRowColors.Apply(row, severity switch
        {
            SqlDiagnosticSeverity.Error => LintSeverity.Error,
            SqlDiagnosticSeverity.Warning => LintSeverity.Warning,
            SqlDiagnosticSeverity.Information => LintSeverity.Information,
            _ => LintSeverity.Hint
        }, isDarkTheme);
    }

    private void DiagnosticsGrid_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != 0 || sender is not DataGridView grid)
            return;

        if (grid.Rows[e.RowIndex].Tag is not SqlDiagnostic issue || string.IsNullOrWhiteSpace(issue.Code))
            return;

        var disabledRules = _window.SqlResultsWindowSettings.Config.DisabledLintRules;
        bool isDisabled = disabledRules?.Contains(issue.Code, StringComparer.OrdinalIgnoreCase) == true;

        if (isDisabled)
        {
            _window.EnableSqlLintRule(issue.Code);
        }
        else
        {
            _window.DisableSqlLintRule(issue.Code);
            MessageBox.Show(
                "Full effect requires application restart.",
                "Rule Disabled",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void DiagnosticsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || sender is not DataGridView grid)
        {
            return;
        }

        if (grid.Rows[e.RowIndex].Tag is not SqlDiagnostic issue)
        {
            return;
        }

        var editor = grid.Tag as FastColoredTextBox ?? _window.SqlResultsCurrentEditor;
        if (editor is null || editor.IsDisposed)
        {
            return;
        }

        int start = Math.Clamp(issue.StartOffset, 0, Math.Max(0, editor.TextLength - 1));
        int length = editor.TextLength == 0
            ? 0
            : Math.Max(1, Math.Min(issue.Length, editor.TextLength - start));
        editor.SelectionStart = start;
        editor.SelectionLength = length;
        editor.DoCaretVisible();
        editor.Focus();
    }

    private static string GetLine(FastColoredTextBox? editor, int offset)
    {
        if (editor is null || offset < 0 || offset > editor.TextLength)
            return string.Empty;
        return (editor.PositionToPlace(offset).iLine + 1).ToString();
    }

    private static string GetColumn(FastColoredTextBox? editor, int offset)
    {
        if (editor is null || offset < 0 || offset > editor.TextLength)
            return string.Empty;
        return (editor.PositionToPlace(offset).iChar + 1).ToString();
    }

    public void UpdateHighlightButtonState()
    {
        foreach (var kvp in _highlightButtons)
        {
            var btn = kvp.Value;
            if (!btn.IsDisposed)
                btn.Text = _window.IsLintEditorHighlightShown ? "Highlight: ON" : "Highlight: OFF";
        }
    }
}
