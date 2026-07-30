// BaseWindow SQL execution partial.
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Common.JsonContext;
using AppBase.Common.Models;
using AppBase.Common.WindowManagement;
using AppBase.Data;
using AppBase.Data.Completion;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Services;
using AppBase.Services.Helpers;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustyBase.NetezzaDriver;
using JustyBase.NetezzaSqlParser.Authoring;
using System.Drawing;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.DbForms;
using JustyBaseLegacy.UI.Extensions;
using JustyBaseLegacy.UI.Models;
using SpreadSheetTasks;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using AppBase.Services.Sql;
using JustData.Application.Editor;
using JustData.Application.Sql;
using JustData.ViewModels.Editor;
using JustyBaseLegacy.UI.Sql;

namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        readonly Dictionary<string, string> _knownParams = new Dictionary<string, string>();
        private static readonly Regex _containsLetter = ContainsAZRegex();

        private async Task<(string sql, string filepath, ExportOptions exportOptions)> PrepareSQLAsync(string originalQuery)
        {
            bool cancelled = false;

            var request = new PreprocessRequest(
                SqlText: originalQuery,
                ConnectionName: SelectedConnectionName,
                DatabaseName: SelectedDatabase,
                DocumentKey: ActiveEditorTabPage?.Text ?? string.Empty,
                KnownParameters: new Dictionary<string, string>(_knownParams, StringComparer.OrdinalIgnoreCase),
                AllowPrompts: true);

            // Adapter that shows the WinForms Prompt dialog, applies quote-wrapping,
            // and updates UI state. Returns null if user cancels.
            IVariablePromptService promptService = new PromptAdapter(async (unresolved, ct) =>
            {
                var strParams = new Dictionary<string, string>();
                foreach (var entry in unresolved)
                    strParams[entry.Value] = _knownParams.TryGetValue(entry.Key, out var known) ? known : "";

                var r = Prompt.ShowDialog(strParams, "Please enter value", out var values);
                if (r != DialogResult.OK)
                {
                    cancelled = true;
                    return null;
                }

                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int i = 0;
                foreach (var entry in unresolved)
                {
                    string val = values[i];
                    if (_containsLetter.IsMatch(val) && !val.StartsWith('\''))
                        val = "'" + val + "'";
                    result[entry.Key] = val;
                    _knownParams[entry.Key] = val;
                    string globalKey = '&' + entry.Value[1..];
                    _sessionVariableRuntimeContext.SetGlobalVariable(globalKey, val);
                    i++;
                }
                VariablesRefresh();
                return result;
            });

            var result = await _sqlPreprocessingService.PreprocessAsync(request, promptService);

            if (cancelled)
                return (string.Empty, null, ExportOptions.noInfo);

            // Sync session/global variables from the result
            string tabName = ActiveEditorTabPage?.Text ?? string.Empty;
            _sessionVariableRuntimeContext.SetSessionVariables(tabName, result.UpdatedSessionVariables);
            foreach (var kvp in result.UpdatedGlobalVariables)
                _sessionVariableRuntimeContext.SetGlobalVariable(kvp.Key, kvp.Value);

            // Sync known parameters
            foreach (var kvp in result.UpdatedKnownParameters)
                _knownParams[kvp.Key] = kvp.Value;

            // Handle export directive
            string? fPath = null;
            ExportOptions eo = ExportOptions.noInfo;
            if (result.ExportOptionDirective is not null)
            {
                fPath = result.ExportFilePath;
                eo = result.ExportOptionDirective.Equals("csv", StringComparison.OrdinalIgnoreCase)
                    ? ExportOptions.csv
                    : ExportOptions.xlsx;
            }

            return (result.ProcessedSql, fPath, eo);
        }

        /// <summary>
        /// IVariablePromptService adapter wrapping a delegate.
        /// </summary>
        private sealed class PromptAdapter : IVariablePromptService
        {
            private readonly Func<IReadOnlyDictionary<string, string>, CancellationToken, Task<IReadOnlyDictionary<string, string>?>> _handler;
            public PromptAdapter(Func<IReadOnlyDictionary<string, string>, CancellationToken, Task<IReadOnlyDictionary<string, string>?>> handler) => _handler = handler;
            public Task<IReadOnlyDictionary<string, string>?> PromptAsync(IReadOnlyDictionary<string, string> unresolved, CancellationToken ct) => _handler(unresolved, ct);
        }

        private TabPagePicture PrepareTab(SplitContainer container = null, bool enableDisable = false, bool isLogTab = false)
        {
            SplitContainer containerX = container ?? CurrentSplitContainer;

            TabControl tc = EnsureResultsTabControl(containerX);

            // Reuse existing log tab — logs should be one tab, appended.
            if (isLogTab)
            {
                foreach (TabPage page in tc.TabPages)
                {
                    if (page.Tag is TabPageResultsTag tag && tag.IsLog)
                    {
                        // Log is a mandatory first-class execution view. Keep
                        // it directly after permanent Diagnostics even when a
                        // result grid was created before the first log event.
                        MoveLogTabAfterDiagnostics(tc, page);
                        // Do not steal focus from a result tab when a later
                        // execution event appends another log entry.
                        bool hasResultTab = tc.TabPages.Cast<TabPage>()
                            .Any(item => item.Tag is TabPageResultsTag resultTag
                                && !resultTag.IsLog
                                && !resultTag.IsPermanentDiagnostics);
                        if (!hasResultTab)
                            tc.SelectedTab = page;
                        return (TabPagePicture)page;
                    }
                }
            }

            TabPagePicture actualTab = new TabPagePicture();
            actualTab.Text = isLogTab
                ? "Log"
                : ResultsTabNaming.NextResultTitle(tc);
            actualTab.CloseImage = _normalXimage;
            bool pinned = isLogTab || _applicationSettingsContext.Config.PinDataByDefault;
            actualTab.PinImage = pinned ? _activePinImage : _normalPinImage;

            actualTab.Tag = new TabPageResultsTag()
            {
                Docked = pinned,
                ParentControl = tc,
                IsLog = isLogTab,
                HasLog = isLogTab,
                DocumentId = CurrentEditorDocumentId
            };

            if (isLogTab)
                tc.TabPages.Insert(GetLogTabIndex(tc), actualTab);
            else
                tc.TabPages.Add(actualTab);
            tc.SelectedTab = actualTab;

            return actualTab;
        }

        private static int GetLogTabIndex(TabControl tabControl)
        {
            for (int index = 0; index < tabControl.TabPages.Count; index++)
            {
                if (tabControl.TabPages[index].Tag is TabPageResultsTag { IsPermanentDiagnostics: true })
                    return index + 1;
            }
            return 0;
        }

        private static void MoveLogTabAfterDiagnostics(TabControl tabControl, TabPage logTab)
        {
            int expectedIndex = GetLogTabIndex(tabControl);
            if (tabControl.TabPages.IndexOf(logTab) == expectedIndex)
                return;

            tabControl.TabPages.Remove(logTab);
            tabControl.TabPages.Insert(expectedIndex, logTab);
        }

        private void Tc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode is Keys.N or Keys.T)
            {
                OpenNewSqlDocument();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.F2)
            {
                RenameResultTabEventHandler(sender, e);
                _preventRenameMainTab = true;
            }
        }

        sealed class ConnectionData
        {
            public DbCommand Cmd { get; set; }
            public DbConnection Conn { get; set; }
            public int Ssid { get; set; }
            public int ProcessID { get; set; }
            public EditorDocumentId? DocumentId { get; set; }
        }

        void AddHistoryEntry(string sql, string database, string connectioName)
        {
            var currentDateTime = DateTime.Now;

            using (BinaryWriter br = new BinaryWriter(new FileStream(HistoryDatFile, FileMode.Append, FileAccess.Write), Encoding.UTF8))
            {
                br.Write(currentDateTime.ToBinary());
                br.Write(sql);
                br.Write(database);
                br.Write(connectioName);
                br.Flush();
                //(br.BaseStream as FileStream).Flush(true);
            }
        }
        private string HistoryDatFile => $"{_applicationSettingsContext.ConfigDirectory}\\history.dat";

        private async void FormatSQL_Click(object sender, EventArgs e)
        {
            try
            {
                var fctb = CurrentTB;
                bool textSelected = !fctb.Selection.IsEmpty;
                string text;
                if (textSelected)
                {
                    text = fctb.SelectedText;
                }
                else
                {
                    text = fctb.TextFast;
                }

                string res = text;

                int timeout = 5000;
                var task = Task.Run(() => res = _formatter.Format(text));

                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                {
                    fctb?.Invoke(() =>
                        {
                            if (textSelected)
                            {
                                fctb.InsertText(res, true);
                            }
                            else
                            {
                                fctb.Text = res;
                            }
                        });
                }
                else
                {
                    _loggerLoud.MessageBox_Show(this, "SQL formatter timed out.", "Formatter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }

        private readonly Lock _sync = new Lock();

        private bool RiskySqlCommand(string query, string? driverName = null)
        {
            return _sqlRiskGate.AllowExecution(
                query,
                driverName,
                _applicationSettingsContext.Config.DoNotWarnFullUpdateDelete,
                new WinFormsSqlRiskConfirmation(this, _loggerLoud));
        }

        private void RestyleCurrentTb()
        {
            MiscellaneousHelper.UpdateAdditionStyles(CurrentTB.Range, _colorTheme.CurrentFctbColors, _applicationSettingsContext.Config.BracketFolding);
            GetTextCommentRanges(CurrentTB);
        }

        private void BtNo_Click(object sender, EventArgs e)
        {
            Close();
        }

        private DataTable? _dtDoEksportu;
        private List<object[]>? _dtDoEksportuRows;

        private DataGridView? _currentDataGrid;
        private ICustomDataGridView? _currentMyGrid;
        private void DataGrid_MouseDown_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is DataGridView dataGrid && dataGrid.Parent is CustomDataGridView customGrid)
            {
                _dtDoEksportu = customGrid.CurrentDataTable;
                _dtDoEksportuRows = customGrid.RowsList;
                _currentMyGrid = customGrid;
            }
            else
            {
                _dtDoEksportu = null;
                _dtDoEksportuRows = null;
                _currentMyGrid = null;
            }

            _currentDataGrid = sender as DataGridView;
        }
        private void ClearCurrentHelpReferences()
        {
            _dtDoEksportu = null;
            _dtDoEksportuRows = null;
            _currentDataGrid = null;
            _currentMyGrid = null;
        }

        private async void BtAbort_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            ConnectionData connectionData = button?.Tag as ConnectionData;
            if (button is null || connectionData is null)
            {
                return;
            }

            button.Enabled = false;
            button.Invalidate();
            try
            {
                if (connectionData.DocumentId is { } documentId)
                    await _sqlExecutionSessionRegistry.CancelAsync(documentId);
                else
                    await Task.Run(() => connectionData.Cmd.Cancel());
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, ex.Message, "Abort error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async void Stop_Click(object sender, EventArgs e)
        {
            try
            {
                if (CurrentTB is { } activeEditor
                    && _documentIdsByEditor.TryGetValue(activeEditor, out var activeDocumentId)
                    && _editorWorkspaceViewModel.Documents.FirstOrDefault(item => item.Id == activeDocumentId) is { } activeDocument)
                {
                    await activeDocument.SqlExecution.StopAsync();
                    await _sqlExecutionSessionRegistry.CancelAsync(activeDocumentId);
                    return;
                }

                if (_connectionSessions.TryGetValue(SelectedConnectionName, out var database))
                    await database.AbortAsync("x");
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }

        private EditorDocumentViewModel? EnsureWorkspaceDocument(FastColoredTextBox editor)
        {
            return EditorWorkspaceDocumentEnsure.GetOrCreateByEditorKey(
                _editorWorkspaceViewModel,
                _documentIdsByEditor,
                editor,
                () => CreateAndAttachWorkspaceDocument(editor));
        }

        /// <summary>
        /// Creates a workspace document and mirrors its id onto panel + DockSuite
        /// projections. Workspace + <c>_documentIdsByEditor</c> remain the SSOT.
        /// </summary>
        private EditorDocumentViewModel? CreateAndAttachWorkspaceDocument(FastColoredTextBox editor)
        {
            if (!TryGetTabAndPanelForEditor(editor, out TabPage? ownerTab, out SQLUpperPanel? panel)
                || ownerTab is null
                || panel is null)
            {
                return null;
            }

            var editorDocument = _editorWorkspaceViewModel.AddDocumentFromView(
                ownerTab.Text,
                editor.Text,
                (ownerTab.Tag as TabPageMainTag)?.Filename,
                panel.SelectedConnectionName,
                panel.SelectedDatabase,
                panel.KeepConnectionOpen,
                panel.ContinueOnError);
            editorDocument.DiagnosticsChanged += OnDocumentDiagnosticsChanged;
            editorDocument.SqlExecution.EventReceived += _sqlResultPresenter.Handle;
            editorDocument.SqlExecution.EventReceived += PresentProviderExecutionLog;
            _sqlResultPresenter.Attach(editorDocument.SqlExecution);

            _documentIdsByTab[ownerTab] = editorDocument.Id;
            _documentIdsByEditor[editor] = editorDocument.Id;
            panel.SetDocumentId(editorDocument.Id);
            if (_tabManager is DockSuiteTabManager dockSuiteTabManager)
                dockSuiteTabManager.SetDocumentId(ownerTab, editorDocument.Id);
            if (_tabManager.GetSplitContainerForTab(ownerTab) is { } splitContainer)
                EnsureResultsTabControl(splitContainer);
            RegisterDiagnosticsTarget(editorDocument.Id, editor);
            return editorDocument;
        }

        /// <summary>Resolves the tab/panel that owns <paramref name="editor"/> (not merely the active tab).</summary>
        private bool TryGetTabAndPanelForEditor(
            FastColoredTextBox editor,
            out TabPage? ownerTab,
            out SQLUpperPanel? panel)
        {
            ownerTab = null;
            panel = null;
            if (editor is null)
                return false;

            foreach (TabPage candidate in EditorTabPages)
            {
                if (!ReferenceEquals(_tabManager.GetEditor(candidate), editor))
                    continue;
                if (_tabManager.GetEditorPanel(candidate) is not SQLUpperPanel ownerPanel)
                    continue;

                ownerTab = candidate;
                panel = ownerPanel;
                return true;
            }

            return false;
        }

        public async Task RunSQL(int mode = 0, ExportOptions exportOption = ExportOptions.grid, bool explain = false, string filePath = null)
        {
            FastColoredTextBox? editor = CurrentTB;
            if (editor is null)
                return;

            EditorDocumentViewModel? document = EnsureWorkspaceDocument(editor);
            if (document is null)
            {
                _loggerLoud.MessageBox_Show(
                    this,
                    "SQL execution requires an editor document.",
                    "SQL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string editorSql = SqlPerformancePolicy.IsLargeScriptDocument(editor.LinesCount, editor.TextLength)
                ? editor.TextFast
                : editor.Text;
            document.UpdateTextFromView(editorSql, editor.LinesCount);

            // Repeated keyboard input while a document is running is a
            // no-op at the view boundary. The VM still rejects races
            // between programmatic callers, but an async WinForms key
            // handler must never surface that rejection as an
            // unhandled application-closing exception.
            if (document.SqlExecution.IsBusy)
                return;

            // The panel is an adapter. Capture its current execution
            // options before building the immutable request so toggles
            // such as Keep connection open select the intended route.
            if (_tabManager.CurrentEditorPanel is { } panel)
            {
                document.ConnectionName = panel.SelectedConnectionName;
                document.DatabaseName = panel.SelectedDatabase;
                document.KeepConnectionOpen = panel.KeepConnectionOpen;
                document.ContinueOnError = panel.ContinueOnError;
            }

            JustData.Application.Sql.SqlExecutionMode executionMode = mode switch
            {
                4 => JustData.Application.Sql.SqlExecutionMode.RunToCursor,
                1 => JustData.Application.Sql.SqlExecutionMode.SingleBatch,
                _ => JustData.Application.Sql.SqlExecutionMode.Selection
            };
            JustData.Application.Sql.SqlOutputMode outputMode = exportOption switch
            {
                ExportOptions.csv => JustData.Application.Sql.SqlOutputMode.Csv,
                ExportOptions.xlsx => JustData.Application.Sql.SqlOutputMode.Xlsx,
                ExportOptions.xlsb => JustData.Application.Sql.SqlOutputMode.Xlsb,
                ExportOptions.onlyLog => JustData.Application.Sql.SqlOutputMode.LogOnly,
                _ => JustData.Application.Sql.SqlOutputMode.Grid
            };
            if (outputMode is JustData.Application.Sql.SqlOutputMode.Csv
                or JustData.Application.Sql.SqlOutputMode.Xlsx
                or JustData.Application.Sql.SqlOutputMode.Xlsb)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    SaveFileDialog dialog = outputMode == JustData.Application.Sql.SqlOutputMode.Csv
                        ? saveFileCSV
                        : saveFileXlsx;
                    if (outputMode == JustData.Application.Sql.SqlOutputMode.Xlsb)
                        dialog.Filter = dialog.Filter.Replace("xlsx", "xlsb", StringComparison.OrdinalIgnoreCase);
                    if (dialog.ShowDialog() != DialogResult.OK)
                        return;
                    filePath = dialog.FileName;
                }
            }

            string sqlText = SelectSqlTextForExecution(editor, mode);
            (string preparedSql, string preparedFilePath, ExportOptions directiveOutput) =
                await PrepareSQLAsync(sqlText);
            if (string.IsNullOrWhiteSpace(preparedSql))
                return;

            sqlText = preparedSql;
            if (directiveOutput != ExportOptions.noInfo)
            {
                outputMode = directiveOutput switch
                {
                    ExportOptions.csv => JustData.Application.Sql.SqlOutputMode.Csv,
                    ExportOptions.xlsx => JustData.Application.Sql.SqlOutputMode.Xlsx,
                    ExportOptions.xlsb => JustData.Application.Sql.SqlOutputMode.Xlsb,
                    ExportOptions.onlyLog => JustData.Application.Sql.SqlOutputMode.LogOnly,
                    _ => outputMode
                };
                if (!string.IsNullOrWhiteSpace(preparedFilePath))
                    filePath = preparedFilePath;
            }

            string executionDriver = _generalDbService.DriverName(document.ConnectionName);
            if (!RiskySqlCommand(sqlText, executionDriver))
                return;

            document.UpdateEditorSelection(editor.SelectionStart, editor.SelectionLength, editor.SelectionStart);
            await document.SqlExecution.RunAsync(
                document.BuildExecutionRequest(executionMode, outputMode, filePath) with
                {
                    SqlText = sqlText,
                    Explain = explain,
                    CommandTimeoutSeconds = _applicationSettingsContext.Config.CommandTimeout,
                    RowLimit = _applicationSettingsContext.Config.ResultRowsLimit
                });
            SynchronizeSelectedResult(document.Id);
            if (_completionContext.SelectedConnectionName != SelectedConnectionName)
            {
                _completionRuntimeContext.SelectedConnectionName = SelectedConnectionName;
            }
        }

        /// <summary>
        /// Preserves the legacy Run behavior at the WinForms boundary: an
        /// explicit selection wins; otherwise execute the statement containing
        /// the caret. Run-to-cursor intentionally uses all text before the
        /// active selection end.
        /// </summary>
        private static string SelectSqlTextForExecution(FastColoredTextBox editor, int mode)
        {
            if (mode == 4)
            {
                int selectionEnd = editor.SelectionStart + editor.SelectionLength;
                editor.SelectionStart = 0;
                editor.SelectionLength = Math.Clamp(selectionEnd, 0, editor.TextLength);
            }

            if (editor.Selection.TextLength < 2)
                editor.SelectBetweenSemicolons();

            return editor.Selection.Text;
        }



        public void RefreshTabKeepConnectionProperty()
        {
            if (CurrentUpper?.CurrentTb is not null && TabConnectionCache.Default.TryGet(CurrentUpper.CurrentTb, out var connectionData))
            {
                connectionData.CloseConnectionByDefault = _applicationSettingsContext.Config.CloseConnectionByDefault;
                if (connectionData.Connection?.State == ConnectionState.Open)
                {
                    connectionData.Connection.Close();
                }
            }
        }

        private void SynchronizeSelectedResult(EditorDocumentId documentId)
        {
            if (_documentIdsByTab.FirstOrDefault(item => item.Value == documentId).Key is not { } tab)
                return;

            TabControl? results = (_tabManager.GetSplitContainerForTab(tab)?.Tag as ResultData)
                ?.TabControlSQLResults;
            ResultSetKey? key = (results?.SelectedTab?.Tag as TabPageResultsTag)?.Key;
            _editorWorkspaceViewModel.Documents
                .FirstOrDefault(document => document.Id == documentId)
                ?.SqlExecution.SelectResultKey(key);
        }

    }
}
