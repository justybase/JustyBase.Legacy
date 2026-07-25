// BaseWindow SQL results UI partial — delegates to SqlResultsUiFactory.
using AppBase.Common;
using AppBase.Common.Interfaces;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.UI.Forms;
using WeifenLuo.WinFormsUI.Docking;
using JustyBaseLegacy.UI.Models;
using JustyBaseLegacy.UI.Sql;
using JustData.ViewModels.Editor;
using JustData.Application.Editor;
using JustData.Application.Startup;
using JustyBaseLegacy.Services;
using System.Diagnostics;
using System.Data;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        bool IWinFormsSqlResultView.CanPresentSqlResult(EditorDocumentId documentId) =>
            CanPresentSqlResult(documentId);

        void IWinFormsSqlResultView.BeginInvoke(Action action) =>
            BeginInvoke(action);

        TabPagePicture IWinFormsSqlResultView.CreatePresentedResultTab(
            EditorDocumentId documentId,
            JustData.Application.Sql.ResultSetDescriptor descriptor) =>
            CreatePresentedResultTab(documentId, descriptor);

        CustomDataGridView IWinFormsSqlResultView.CreatePresentedResultGrid(
            EditorDocumentId documentId,
            TabPagePicture tab,
            JustData.Application.Sql.ResultSetDescriptor descriptor,
            List<object[]> rows) =>
            CreatePresentedResultGrid(documentId, tab, descriptor, rows);

        void IWinFormsSqlResultView.RegisterPresentedResultGrid(TabPage tab, CustomDataGridView grid) =>
            RegisterPresentedResultGrid(tab, grid);

        int ISqlResultsUiView.SqlResultsDeviceDpi => SqlResultsDeviceDpi;
        Font ISqlResultsUiView.SqlResultsUiFont => SqlResultsUiFont;
        FastColoredTextBox ISqlResultsUiView.SqlResultsCurrentEditor => SqlResultsCurrentEditor;
        IUiHelperService ISqlResultsUiView.SqlResultsUiHelper => SqlResultsUiHelper;
        IColorTheme ISqlResultsUiView.SqlResultsColorTheme => SqlResultsColorTheme;
        IApplicationSettingsContext ISqlResultsUiView.SqlResultsWindowSettings => SqlResultsWindowSettings;
        ContextMenuStrip ISqlResultsUiView.SqlResultsContextMenu => SqlResultsContextMenu;
        Image ISqlResultsUiView.SqlResultsNormalCloseImage => SqlResultsNormalCloseImage;
        Image ISqlResultsUiView.SqlResultsActivePinImage => SqlResultsActivePinImage;
        TabControlDrawingHandler ISqlResultsUiView.SqlResultsTabDrawingHandler => SqlResultsTabDrawingHandler;
        Dictionary<EditorDocumentId, (FastColoredTextBox Editor, DataGridView Grid)>
            ISqlResultsUiView.SqlResultsLintDiagnosticsTargets => SqlResultsLintDiagnosticsTargets;
        void ISqlResultsUiView.OnSqlResultsTabKeyDown(object sender, KeyEventArgs e) =>
            OnSqlResultsTabKeyDown(sender, e);
        void ISqlResultsUiView.OnSqlResultSelectionChanged(TabPage? selectedTab) =>
            OnSqlResultSelectionChanged(selectedTab);
        void ISqlResultsUiView.DisableSqlLintRule(string ruleId) => DisableSqlLintRule(ruleId);
        void ISqlResultsUiView.EnableSqlLintRule(string ruleId) => EnableSqlLintRule(ruleId);
        bool ISqlResultsUiView.IsLintEditorHighlightShown => _applicationSettingsContext.Config.LintEditorHighlightShown;
        void ISqlResultsUiView.ToggleSqlLintEditorHighlight() => ToggleLintEditorHighlight();
        void ISqlResultsUiView.DisableSqlLintRuleHighlight(string ruleId) => DisableLintRuleHighlight(ruleId);
        void ISqlResultsUiView.EnableSqlLintRuleHighlight(string ruleId) => EnableLintRuleHighlight(ruleId);
        SplitContainer? ISqlResultsUiView.GetSqlResultsSplitContainerForTab(TabPage tabPage) =>
            GetSqlResultsSplitContainerForTab(tabPage);
        TabPage? ISqlResultsUiView.FindSqlResultsTabForDocument(EditorDocumentId documentId) =>
            FindSqlResultsTabForDocument(documentId);
        DockContent? ISqlResultsUiView.EnsureSqlResultsToolWindow() => EnsureSqlResultsToolWindow();
        TabControl? ISqlResultsUiView.GetSqlResultsTabControl(TabPage tabPage) => GetSqlResultsTabControl(tabPage);
        TabPage? ISqlResultsUiView.FindSqlResultsTabForSplitContainer(SplitContainer splitter) =>
            FindSqlResultsTabForSplitContainer(splitter);
        void ISqlResultsUiView.AttachTabPage(TabPage page, TabControl tabControl) => AttachTabPage(page, tabControl);
        void ISqlResultsUiView.RemoveTabData(TabControl tabControl, int index) => RemoveTabData(tabControl, index);

        internal int SqlResultsDeviceDpi => DeviceDpi;
        internal Font SqlResultsUiFont => Font;
        internal FastColoredTextBox SqlResultsCurrentEditor => CurrentTB;
        internal IUiHelperService SqlResultsUiHelper => _uiHelperService;
        internal IColorTheme SqlResultsColorTheme => _colorTheme;
        internal IApplicationSettingsContext SqlResultsWindowSettings => _applicationSettingsContext;
        internal ContextMenuStrip SqlResultsContextMenu => cmResults;
        internal Image SqlResultsNormalCloseImage => _normalXimage;
        internal Image SqlResultsActivePinImage => _activePinImage;
        internal TabControlDrawingHandler SqlResultsTabDrawingHandler => _tabControlDrawingHandler;
        internal Dictionary<EditorDocumentId, (FastColoredTextBox Editor, DataGridView Grid)> SqlResultsLintDiagnosticsTargets => _lintDiagnosticsTargets;
        internal void OnSqlResultsTabKeyDown(object sender, KeyEventArgs e) => Tc_KeyDown(sender, e);
        internal void OnSqlResultSelectionChanged(TabPage? selectedTab)
        {
            if (selectedTab?.Tag is not TabPageResultsTag { DocumentId: { } documentId } tag)
                return;

            _editorWorkspaceViewModel.Documents
                .FirstOrDefault(document => document.Id == documentId)
                ?.SqlExecution.SelectResult(tag.ResultSetId);
        }
        internal void DisableSqlLintRule(string ruleId) => DisableLintRule(ruleId);
        internal void EnableSqlLintRule(string ruleId) => EnableLintRule(ruleId);
        internal SplitContainer? GetSqlResultsSplitContainerForTab(TabPage tabPage) =>
            _tabManager.GetSplitContainerForTab(tabPage);
        internal TabPage? FindSqlResultsTabForDocument(EditorDocumentId documentId) =>
            _documentIdsByTab.FirstOrDefault(item => item.Value == documentId).Key;
        internal DockContent EnsureSqlResultsToolWindow() =>
            _tabManager is DockSuiteTabManager dsm ? dsm.EnsureResultsToolWindow() : null;
        internal TabControl? GetSqlResultsTabControl(TabPage tabPage) =>
            _tabManager is DockSuiteTabManager dsm ? dsm.GetOrCreateResultsTabControl(tabPage) : null;
        internal TabPage? FindSqlResultsTabForSplitContainer(SplitContainer splitter) =>
            _tabManager is DockSuiteTabManager dsm ? dsm.FindTabForSplitContainer(splitter) : null;
        internal bool CanPresentSqlResult(EditorDocumentId documentId) =>
            _editorWorkspaceViewModel.Documents.FirstOrDefault(document => document.Id == documentId) is { } document
            && !string.Equals(_generalDbService.DriverName(document.ConnectionName), "NetezzaSQL", StringComparison.OrdinalIgnoreCase);

        internal TabPagePicture CreatePresentedResultTab(
            EditorDocumentId documentId,
            JustData.Application.Sql.ResultSetDescriptor descriptor)
        {
            TabPagePicture tab = PrepareTab();
            if (tab.Tag is TabPageResultsTag tag)
            {
                tag.DocumentId = documentId;
                tag.ResultSetId = descriptor.ResultSetId;
            }
            return tab;
        }

        internal CustomDataGridView CreatePresentedResultGrid(
            EditorDocumentId documentId,
            TabPagePicture tab,
            JustData.Application.Sql.ResultSetDescriptor descriptor,
            List<object[]> rows)
        {
            var dataTable = new DataTable();
            foreach (JustData.Application.Sql.ResultColumnDescriptor column in descriptor.Columns)
                dataTable.Columns.Add(column.Name, typeof(object));

            FastColoredTextBox editor = _documentIdsByEditor.FirstOrDefault(pair => pair.Value == documentId).Key
                ?? CurrentTB
                ?? throw new InvalidOperationException("The SQL editor is no longer available.");
            var grid = new CustomDataGridView(_colorTheme, _importExportTasks, _uiHelperService, editor, dataTable, rows)
            {
                Name = $"resultGrid_{tab.Text}",
                ResultGridAccessibilityName = $"resultGrid_{Guid.NewGuid():N}",
                Dock = DockStyle.Fill,
                DoMessageAction = DoMessage,
                AttachedSQL = string.Empty,
                DateTimeFormat = _applicationSettingsContext.Config.DateTimeFormat,
                DecimalFormat = _applicationSettingsContext.Config.DecimalFormat,
                IntegerFormat = _applicationSettingsContext.Config.IntegerFormat,
                ForceDecimalFormat = _applicationSettingsContext.Config.ForceDecimalFormat,
                AutoSizeColumnsMode = _applicationSettingsContext.Config.AutoSizeColumnsMode
            };
            grid.NewSqlTabRequested += (_, _) => OpenNewSqlDocument();
            _colorTheme.ColorMyDataGridView(grid);
            DataGridDpiHelper.Apply(grid);
            tab.Controls.Add(grid);
            ConfigureResultDataGrid(grid);
            PrepareDocumentationShowcaseAfterFirstResult();
            return grid;
        }

        private bool _documentationShowcasePrepared;

        /// <summary>
        /// After the first result grid exists: dock Results taller (once) and expand explorer to DIMDATE.
        /// Order matches documentation screenshots — no resize timer / no startup tree fighting the grid.
        /// </summary>
        private void PrepareDocumentationShowcaseAfterFirstResult()
        {
            if (_documentationShowcasePrepared)
            {
                return;
            }

            string[] args = Environment.GetCommandLineArgs();
            bool showcaseLayout = StartupArguments.IsDocumentationShowcaseLayout(args);
            if (!showcaseLayout)
            {
                return;
            }

            _documentationShowcasePrepared = true;

            if (_tabManager is DockSuiteTabManager dockSuite)
            {
                // Keep results tall enough that the data grid is clearly visible in docs shots.
                dockSuite.ForceResultsBelowSqlDocuments(0.56);
            }

            if (StartupArguments.IsDocumentationNavigateDimDate(Environment.GetCommandLineArgs()))
            {
                StartDocumentationDimDateNavigateWhenSignaled();
            }
        }

        private void StartDocumentationDimDateNavigateWhenSignaled()
        {
            string signalPath = Path.Combine(Path.GetTempPath(), "justybase-doc-navigate-dimdate");
            try
            {
                File.Delete(signalPath);
            }
            catch (IOException)
            {
                // Best-effort cleanup of a leftover signal from a previous run.
            }

            var timer = new System.Windows.Forms.Timer { Interval = 400 };
            timer.Tick += (_, _) =>
            {
                if (!File.Exists(signalPath))
                {
                    return;
                }

                timer.Stop();
                timer.Dispose();
                try
                {
                    File.Delete(signalPath);
                }
                catch (IOException)
                {
                    // Ignore — navigation still proceeds.
                }

                _ = RunUiEventAsync(
                    nameof(NavigateDocumentationDimDateExplorerAsync),
                    NavigateDocumentationDimDateExplorerAsync);
            };
            timer.Start();
        }

        internal void RegisterPresentedResultGrid(TabPage tab, CustomDataGridView grid)
        {
            RegisterLegacyResultGrid(tab, grid);
            if (tab.Tag is TabPageResultsTag { DocumentId: { } documentId, ResultSetId: { Length: > 0 } resultSetId })
                _resultGridRegistry.Register(documentId, resultSetId, grid);
        }

        private void LayoutResultsToolbar(Control parent, Button btAbort, ProgressBar progressBarSQL, Control logView = null) =>
            _sqlResultsUiFactory.LayoutResultsToolbar(parent, btAbort, progressBarSQL, logView);

        private TabControl EnsureResultsTabControl(SplitContainer containerX) =>
            _sqlResultsUiFactory.EnsureResultsTabControl(containerX);

        private void EnsureDiagnosticsTab(TabControl tc, ResultData? resultData) =>
            _sqlResultsUiFactory.EnsureDiagnosticsTab(tc, resultData);

        private DataGridView PrepareDiagnosticsGrid(TabPage diagnosticsTab) =>
            _sqlResultsUiFactory.PrepareDiagnosticsGrid(diagnosticsTab);

        private ISqlExecutionLog PrepareSqlLog(TabPagePicture currentResultsTab, Button btAbort) =>
            _sqlResultsUiFactory.PrepareSqlLog(currentResultsTab, btAbort);

        private void RegisterDiagnosticsTarget(EditorDocumentId documentId, FastColoredTextBox editor) =>
            _sqlResultsUiFactory.RegisterDiagnosticsTarget(documentId, editor);

        private void OnDocumentDiagnosticsChanged(
            EditorDocumentViewModel document,
            IReadOnlyList<JustData.Application.Sql.SqlDiagnostic> diagnostics)
        {
            _sqlResultsUiFactory.OnDocumentDiagnosticsChanged(document.Id, diagnostics);
            EditorDocumentId documentId = document.Id;
            if (!_lintDiagnosticsTargets.TryGetValue(documentId, out var target)
                || target.Editor.IsDisposed)
            {
                _cachedDiagnostics.Remove(documentId);
                return;
            }

            void Apply()
            {
                if (target.Editor.IsDisposed)
                    return;

                CacheDiagnostics(documentId, diagnostics);
                IReadOnlyList<LintIssue> issues = diagnostics
                    .Select(diagnostic => MapLintIssue(target.Editor, diagnostic))
                    .ToArray();
                CacheLintIssues(documentId, issues);

                if (_applicationSettingsContext.Config.LintEditorHighlightShown)
                    ApplyDocumentLintMarkers(target.Editor, diagnostics);
                else
                    ClearLintMarkers(target.Editor);

                _lightbulbManager.RefreshLightbulbs(target.Editor);
            }

            if (target.Editor.InvokeRequired)
                target.Editor.BeginInvoke(Apply);
            else
                Apply();
        }

        private static LintIssue MapLintIssue(
            FastColoredTextBox editor,
            JustData.Application.Sql.SqlDiagnostic diagnostic)
        {
            int start = Math.Clamp(diagnostic.StartOffset, 0, editor.TextLength);
            int end = Math.Clamp(start + Math.Max(1, diagnostic.Length), start, editor.TextLength);
            Place startPlace = editor.PositionToPlace(start);
            Place endPlace = editor.PositionToPlace(end);
            return new LintIssue(
                diagnostic.Code ?? string.Empty,
                diagnostic.Message,
                diagnostic.Severity switch
                {
                    JustData.Application.Sql.SqlDiagnosticSeverity.Error => LintSeverity.Error,
                    JustData.Application.Sql.SqlDiagnosticSeverity.Warning => LintSeverity.Warning,
                    JustData.Application.Sql.SqlDiagnosticSeverity.Information => LintSeverity.Information,
                    _ => LintSeverity.Hint
                },
                start,
                end,
                startPlace.iLine + 1,
                startPlace.iChar + 1,
                endPlace.iLine + 1,
                endPlace.iChar + 1);
        }

        private void ApplyDocumentLintMarkers(
            FastColoredTextBox editor,
            IReadOnlyList<JustData.Application.Sql.SqlDiagnostic> diagnostics)
        {
            var colors = _colorTheme.CurrentFctbColors;
            var disabledHighlight = _applicationSettingsContext.Config.DisabledHighlightRules;
            if (disabledHighlight?.Count > 0)
            {
                diagnostics = diagnostics
                    .Where(d => d.Code is null || !disabledHighlight.Contains(d.Code, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            editor.Range.ClearStyle(colors.ErrorStyle, colors.WarningStyle, colors.LintInfoStyle);
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.StartOffset < 0 || diagnostic.StartOffset >= editor.TextLength)
                    continue;

                int length = Math.Max(1, Math.Min(
                    diagnostic.Length,
                    editor.TextLength - diagnostic.StartOffset));
                var range = new FastColoredTextBoxNS.Range(editor)
                {
                    Start = editor.PositionToPlace(diagnostic.StartOffset),
                    End = editor.PositionToPlace(diagnostic.StartOffset + length)
                };
                range.SetStyle(diagnostic.Severity switch
                {
                    JustData.Application.Sql.SqlDiagnosticSeverity.Error => colors.ErrorStyle,
                    JustData.Application.Sql.SqlDiagnosticSeverity.Warning => colors.WarningStyle,
                    _ => colors.LintInfoStyle
                });
            }
            editor.Invalidate();
        }

        private void RefreshAllDiagnosticsGrids() =>
            _sqlResultsUiFactory.RefreshAllDiagnosticsGrids();

        private void DisableLintRule(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                return;
            }

            var disabledRules = _applicationSettingsContext.Config.DisabledLintRules ??= [];
            if (!disabledRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
            {
                disabledRules.Add(ruleId);
                _settingsPersistence.SaveConfig();
            }

            foreach (EditorDocumentViewModel document in _editorWorkspaceViewModel.Documents)
                document.SqlAuthoring.DisableRule(ruleId);
            ScheduleLintForAllEditors();
        }

        private void EnableLintRule(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                return;
            }

            _applicationSettingsContext.Config.DisabledLintRules?.RemoveAll(
                disabledRule => string.Equals(disabledRule, ruleId, StringComparison.OrdinalIgnoreCase));
            _settingsPersistence.SaveConfig();
            foreach (EditorDocumentViewModel document in _editorWorkspaceViewModel.Documents)
                document.SqlAuthoring.EnableRule(ruleId);
            ScheduleLintForAllEditors();
        }

        private void ScheduleLintForAllEditors()
        {
            foreach (EditorDocumentViewModel document in _editorWorkspaceViewModel.Documents.ToArray())
                _ = RelintDocumentSafelyAsync(document);
        }

        private static async Task RelintDocumentSafelyAsync(EditorDocumentViewModel document)
        {
            try
            {
                await document.SqlAuthoring.LintNowAsync(document.Text, document.ConnectionName);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"SQL lint refresh failed: {exception.GetType().Name}");
            }
        }

        private void ToggleLintEditorHighlight()
        {
            _applicationSettingsContext.Config.LintEditorHighlightShown =
                !_applicationSettingsContext.Config.LintEditorHighlightShown;
            _settingsPersistence.SaveConfig();
            RefreshLintMarkers();
        }

        private void DisableLintRuleHighlight(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                return;

            var disabledHighlight = _applicationSettingsContext.Config.DisabledHighlightRules ??= [];
            if (!disabledHighlight.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
            {
                disabledHighlight.Add(ruleId);
                _settingsPersistence.SaveConfig();
            }
            RefreshLintMarkers();
        }

        private void EnableLintRuleHighlight(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                return;

            _applicationSettingsContext.Config.DisabledHighlightRules?.RemoveAll(
                r => string.Equals(r, ruleId, StringComparison.OrdinalIgnoreCase));
            _settingsPersistence.SaveConfig();
            RefreshLintMarkers();
        }

        private void RefreshLintMarkers()
        {
            foreach (var kvp in _lintDiagnosticsTargets)
            {
                EditorDocumentId documentId = kvp.Key;
                var (editor, _) = kvp.Value;
                if (editor.IsDisposed)
                    continue;

                if (!_cachedDiagnostics.TryGetValue(documentId, out var diagnostics))
                    continue;

                if (_applicationSettingsContext.Config.LintEditorHighlightShown)
                    ApplyDocumentLintMarkers(editor, diagnostics);
                else
                    ClearLintMarkers(editor);
            }
        }

        private void ClearLintMarkers(FastColoredTextBox editor)
        {
            var colors = _colorTheme.CurrentFctbColors;
            editor.Range.ClearStyle(colors.ErrorStyle, colors.WarningStyle, colors.LintInfoStyle);
            editor.Invalidate();
        }

        internal void SyncHighlightButtonState()
        {
            _sqlResultsUiFactory.UpdateHighlightButtonState();
        }
    }
}
