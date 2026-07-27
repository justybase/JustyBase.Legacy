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

        private string ReplaceSessionVariables(string tabName, string query)
        {
            foreach (var item in _sessionVariableRuntimeContext.GetSessionVariables(tabName)
                .OrderByDescending(o => o.Key.Length))
            {
                if (query.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
                }
            }

            return query;
        }

        private object Evaluate(string expression)
        {
            object result = expression;
            try
            {
                result = new DataTable().Compute(expression, "");
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Expression evaluation failed: {exception.GetType().Name}");
            }

            return result;
        }

        private async ValueTask<string> ReplaceAndSetSessionVariables(string queryOrg, string tabName, DbConnection conn = null)
        {

            string query = queryOrg;

            var m1 = _rxSessionVariableDefine.Match(query);
            var m2 = _rxGlobalVariableDefine.Match(query);

            // to do evaluate
            if (m1.Success || m2.Success)
            {
                Match m = null;
                if (m1.Success)
                {
                    m = m1;
                }
                else
                {
                    m = m2;
                }

                string variableValue = m.Groups["sessionValue"].Value;
                string val = _sessionVariableRuntimeContext.ReplaceGlobalVariables(ReplaceSessionVariables(tabName, variableValue));
                object val2 = val;
                try
                {
                    if (!val.StartsWith("SQL_"))
                    {
                        val2 = Evaluate(val);
                    }
                    else if (conn is not null)
                    {
                        if (val.StartsWith("SQL_RESULT["))
                        {
                            string sql = val["SQL_RESULT[".Length..^1];
                            using (DbCommand cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                val2 = await Task.Run(() => cmd.ExecuteScalar());
                            }
                        }
                        else if (val.StartsWith("SQL_RECORDS_AFFECTED["))
                        {
                            string sql = val["SQL_RECORDS_AFFECTED[".Length..^1];
                            using (DbCommand cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                val2 = await Task.Run(() => cmd.ExecuteNonQuery());
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }

                string name = m.Groups["sessionVar"].Value;

                if (m1.Success)
                {
                    _sessionVariableRuntimeContext.SetSessionVariable(tabName, name, val2?.ToString());
                    AddVariable(tabName, name, val2?.ToString());
                }
                else if (m2.Success)
                {
                    _sessionVariableRuntimeContext.SetGlobalVariable(name, val2?.ToString());
                    AddVariable(tabName, null, null);
                }
                m = m.NextMatch();
                query = "";
            }
            else
            {
                if (_sessionVariableRuntimeContext.GetSessionVariableCount(tabName) > 0)
                {
                    query = ReplaceSessionVariables(tabName, query);
                }
                query = _sessionVariableRuntimeContext.ReplaceGlobalVariables(query);
            }

            return query;
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

        private async Task<string> SpecialCommandsAsync(string query)
        {
            if (query.Length >= 120)
                return query;

            var result = await _specialCommandService.TryHandleAsync(query);
            if (!result.WasHandled)
                return query;

            if (result.SleepMilliseconds is int sleepMs)
            {
                await Task.Delay(sleepMs);
                return string.Empty;
            }

            if (result.MaxRows is int maxRows)
            {
                _applicationSettingsContext.Config.ResultRowsLimit = maxRows;
                return string.Empty;
            }

            return result.ReplacementSql ?? query;
        }

        private async Task<bool> DoSpecialTask(FastColoredTextBox fctb, string cmd, ISqlExecutionLog log, Stopwatch st, string connectionName = null)
        {
            // Shared special-command path (sleep / max_rows / echo / directories helpers).
            if (cmd.Length < 120)
            {
                var special = await _specialCommandService.TryHandleAsync(cmd);
                if (special.WasHandled)
                {
                    if (special.SleepMilliseconds is int sleepMs)
                    {
                        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), SelectedConnectionName, SelectedDatabase, "sleep", cmd);
                        await Task.Delay(sleepMs);
                        return true;
                    }

                    if (special.MaxRows is int maxRows)
                    {
                        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), SelectedConnectionName, SelectedDatabase, "max rows", cmd);
                        _applicationSettingsContext.Config.ResultRowsLimit = maxRows;
                        return true;
                    }

                    if (special.ReplacementSql is not null
                        && special.ReplacementSql.StartsWith("SELECT '", StringComparison.OrdinalIgnoreCase))
                    {
                        // Echo / directory helpers already applied side effects; log and stop.
                        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), SelectedConnectionName, SelectedDatabase, "special", cmd);
                        return true;
                    }
                }
            }

            string NzConnString = "";

            if (TabConnectionCache.Default.TryGet(fctb, out var connectionData))
            {
                if (_connectionSessions.TryGetValue(connectionData.ConnectionName, out var generalDb) && generalDb is INetezza)
                {
                    NzConnString = _generalDbService.ConnectionStringForNz(_applicationSettingsContext.Config.ConnectionTimeout, connectionData.ConnectionName, SelectedDatabase);
                }
            }
            else
            {
                NzConnString = "";
            }

            if (ImportExportTasks.rxImportXlsxTxt.IsMatch(cmd))
            {
                if (connectionName != null)
                {
                    OtherUtils.OnlyNzMesage(this);

                    return true;
                }
                await Task.Run(() => _importExportTasks.DoXlsxTxtImportFromCodeAsync(_applicationSettingsContext, NzConnString, cmd, _applicationSettingsContext.ConfigDirectory, _applicationSettingsContext.Config, log, st));
                return true;
            }
            else if (InlineCommandPattern.Regex().IsMatch(cmd))
            {
                if (connectionName != null && !cmd.StartsWith("___run"))
                {
                    OtherUtils.OnlyNzMesage(this);
                    return true;
                }
                await _inlineCommandRunner.DoInlineCommandAsync(NzConnString, cmd, log, st);
                return true;
            }
            else if (_databaseRuntimeContext.RxExportCsvXlsx.IsMatch(cmd))
            {
                await Task.Run(() =>
                {
                    if (_connectionSessions.TryGetValue(SelectedConnectionName, out var gdbForExport))
                        gdbForExport.DoCsvOrXlsxExport(cmd, log, st);
                });
                return true;
            }
            else if (cmd.Trim() == "___window iconify")
            {
                _windowManagementService.FlashWindowEx(this);
                return true;
            }
            else if (cmd.Trim() == "___window restore")
            {
                WindowState = FormWindowState.Normal;
                return true;
            }
            return false;
        }

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

        private bool RiskySqlCommand(string query, bool nz = false)
        {
            if (!_applicationSettingsContext.Config.DoNotWarnFullUpdateDelete)
            {
                string? driver = nz ? "NetezzaSQL" : null;
                var risks = _sqlRiskAnalysisService.Analyze(query, driver);
                foreach (var risk in risks)
                {
                    string caption = risk.Kind switch
                    {
                        SqlRiskKind.UnsafeUpdateDelete => "Update/delete warning",
                        SqlRiskKind.MissingDistribute => "Create table warning",
                        SqlRiskKind.SelectInto => "SELECT INTO warning",
                        _ => "SQL risk warning"
                    };
                    var r = _loggerLoud.MessageBox_Show(this, risk.Message, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (r == DialogResult.Cancel)
                        return false;
                }
            }
            return true;
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

        private void FinalizeSqlRun(TabPage currentMainTab, FastColoredTextBox fctbFromStart, TabPagePicture currentResultsTab)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => FinalizeSqlRun(currentMainTab, fctbFromStart, currentResultsTab));
                return;
            }

            if (ActiveEditorTabPage != currentMainTab)
            {
                (currentMainTab as TabPagePicture).FinishedInBackground = true;
                _tabControlMain.Invalidate();
                System.Media.SystemSounds.Hand.Play();
            }

            var tab = currentResultsTab.Parent as TabControl;
            if (tab is not null)
            {
                // On failure keep the Log tab selected so the error row stays visible.
                // Selecting the last tab used to jump to an empty Result created before Read() failed.
                if (LegacyNetezzaResultFetchSession.PreferLogTab(currentResultsTab.IsSuccess)
                    && !currentResultsTab.IsDisposed)
                    tab.SelectedTab = currentResultsTab;
                else
                    tab.SelectedIndex = tab.TabCount - 1;
                UnPin(currentResultsTab, tab);
            }

            if (WindowState == FormWindowState.Minimized)
            {
                System.Media.SystemSounds.Hand.Play();
            }

            _windowManagementService.FlashWindowEx(this);

            // Results are displayed in a separate dock window. Keep the SQL
            // editor active so the user can correct and run another statement
            // immediately. Do not steal focus from a different active document
            // when this query was completed in the background.
            if (fctbFromStart is not null && !fctbFromStart.IsDisposed
                && _tabManager.CurrentEditor == fctbFromStart)
            {
                fctbFromStart.Focus();
            }
        }

        public async Task RunSQL(int mode = 0, ExportOptions exportOption = ExportOptions.grid, bool explain = false, string filePath = null)
        {
            FastColoredTextBox? editor = CurrentTB;
            if (editor is null)
                return;

            if (_documentIdsByEditor.TryGetValue(editor, out var documentId))
            {
                var document = _editorWorkspaceViewModel.Documents
                    .FirstOrDefault(item => item.Id == documentId);
                document?.UpdateTextFromView(editor.Text);

                if (document is not null)
                {
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
                    if (!RiskySqlCommand(sqlText,
                        string.Equals(executionDriver, "NetezzaSQL", StringComparison.OrdinalIgnoreCase)))
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
                    return;
                }
            }

            string driver = _generalDbService.DriverName(SelectedConnectionName);
            if (_completionContext.SelectedConnectionName != SelectedConnectionName)
            {
                _completionRuntimeContext.SelectedConnectionName = SelectedConnectionName;
            }

            if (!editor.Name.StartsWith(driver))
                editor.Name = $"{driver}_addedFastColored";

            switch (driver)
            {
                case "NetezzaSQL":
                    _loggerLoud.MessageBox_Show(this,
                        "SQL execution requires an editor document.",
                        "SQL",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;
                case "DB2":
                case "Microsoft.ACE.OLEDB.12.0":
                case "Oracle":
                case "Postgres":
                case "MySql":
                case "SQLite":
                case "MsSqlStd":
                case "MsSqlTrusted":
                    if (exportOption != ExportOptions.grid && exportOption != ExportOptions.xlsx
                        && exportOption != ExportOptions.csv
                        && exportOption != ExportOptions.onlyLog
                        || explain != false || filePath != null)
                    {
                        _loggerLoud.MessageBox_Show(this, "Not implemented for this database.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        _loggerLoud.MessageBox_Show(this,
                            "SQL execution requires an editor document.",
                            "SQL",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    break;
                default:
                    _loggerLoud.MessageBox_Show(this, "Run SQL is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
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

        private void RegisterLegacyResultGrid(TabPage tab, CustomDataGridView grid)
        {
            if (tab?.Tag is not TabPageResultsTag tag || string.IsNullOrWhiteSpace(tag.ResultSetId))
                return;

            if (tag.DocumentId is not { } documentId)
                return;

            string resultSetId = tag.ResultSetId;
            if (_resultGridRegistry.TryGet(new ResultSetKey(documentId, resultSetId), out _))
                resultSetId = Guid.NewGuid().ToString("N");
            tag.ResultSetId = resultSetId;
            _resultGridRegistry.Register(new ResultSetKey(documentId, resultSetId), grid);
            PrepareDocumentationShowcaseAfterFirstResult();
        }

    }
}
