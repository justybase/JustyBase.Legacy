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
using JustyBase.NetezzaSqlParser.Linter;
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
        private readonly NetezzaSqlErrorHighlighter _nzErrorHighlighter = new();

        /// <summary>
        /// Set when the legacy Netezza executor hits a database error so the VM
        /// bridge can report <see cref="SqlExecutionOutcome.Failed"/> instead of Success.
        /// </summary>
        private string? _legacySqlFailureMessage;

        private void HandleNzErrors(string msg, FastColoredTextBox fctb, int selectionStart, int selectionLength, bool fromOleDB = false)
        {
            _nzErrorHighlighter.Highlight(msg, fctb, _colorTheme.CurrentFctbColors.ErrorStyle, selectionStart, selectionLength, fromOleDB);
        }

        private void RecordLegacySqlFailure(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = "database returned an empty message";
            _legacySqlFailureMessage = message;
        }

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
                    _sessionVariableRuntimeContext.GlobalVariables[globalKey] = val;
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
            foreach (var kvp in result.UpdatedSessionVariables)
            {
                if (!_sessionVariableRuntimeContext.SessionVariables.TryGetValue(tabName, out var tabVars))
                {
                    tabVars = new Dictionary<string, string>();
                    _sessionVariableRuntimeContext.SessionVariables[tabName] = tabVars;
                }
                tabVars[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in result.UpdatedGlobalVariables)
                _sessionVariableRuntimeContext.GlobalVariables[kvp.Key] = kvp.Value;

            // Sync known parameters
            foreach (var kvp in result.UpdatedKnownParameters)
                _knownParams[kvp.Key] = kvp.Value;

            // Handle export directive
            string? fPath = null;
            ExportOptions eo = ExportOptions.noInfo;
            if (result.ExportOptionDirective is not null)
            {
                fPath = result.ExportFilePath;
                eo = ExportOptions.xlsx;
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
            if (_sessionVariableRuntimeContext.SessionVariables.TryGetValue(tabName, out var tab))
            {
                foreach (var item in tab.OrderByDescending(o => o.Key.Length))
                {
                    if (query.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return query;
        }

        private static readonly DataTable _tableToCompute = new System.Data.DataTable();
        public static object Evaluate(string expression)
        {
            object result = expression;
            try
            {
                result = _tableToCompute.Compute(expression, "");
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
                    _sessionVariableRuntimeContext.SessionVariables[tabName][name] = val2?.ToString();
                    AddVariable(tabName, name, val2?.ToString());
                }
                else if (m2.Success)
                {
                    _sessionVariableRuntimeContext.GlobalVariables[name] = val2?.ToString();
                    AddVariable(tabName, null, null);
                }
                m = m.NextMatch();
                query = "";
            }
            else
            {
                if (_sessionVariableRuntimeContext.SessionVariables[tabName].Count > 0)
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
                DocumentId = _executingDocumentId.Value ?? CurrentEditorDocumentId
            };

            tc.TabPages.Add(actualTab);
            tc.SelectedTab = actualTab;

            return actualTab;
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
            if (result.WasHandled)
                return result.ReplacementSql ?? query;

            return query;
        }

        private static readonly Regex _rxSleep = RegexSleep();
        private static readonly Regex _rxMaxRows = RegexMaxRows();
        private static readonly Regex _rxEcho = RegexEcho();
        private static readonly Regex _rxEchoFile = RegexEchoFile();

        private async Task<bool> DoSpecialTask(FastColoredTextBox fctb, string cmd, ISqlExecutionLog log, Stopwatch st, string connectionName = null)
        {
            string NzConnString = "";


            if (TabConnectionCache.Default.TryGet(fctb, out var connectionData))
            {
                if (IGeneralDbService.GeneralDic.TryGetValue(connectionData.ConnectionName, out var generalDb) && generalDb is INetezza)
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
                    if (IGeneralDbService.GeneralDic.TryGetValue(SelectedConnectionName, out var gdbForExport))
                        gdbForExport.DoCsvOrXlsxExport(cmd, log, st);
                });
                return true;
            }
            else if (_rxSleep.IsMatch(cmd))
            {
                log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), SelectedConnectionName, SelectedDatabase, "sleep", cmd);

                var m = _rxSleep.Match(cmd);
                await Task.Delay(int.Parse(m.Groups["nums"].Value));
                return true;
            }
            else if (_rxMaxRows.IsMatch(cmd))
            {
                var m = _rxMaxRows.Match(cmd);
                log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), SelectedConnectionName, SelectedDatabase, "max rows", cmd);
                _applicationSettingsContext.Config.ResultRowsLimit = int.Parse(m.Groups["nums"].Value);
                return true;
            }
            else if (_rxEcho.IsMatch(cmd))
            {
                var m = _rxEcho.Match(cmd);
                log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), SelectedConnectionName, SelectedDatabase, m.Groups["msg"].Value, cmd);
                return true;
            }
            else if (_rxEchoFile.IsMatch(cmd))
            {
                var m = _rxEchoFile.Match(cmd);
                string message = m.Groups["msg"].Value;
                string filePath = m.Groups["filePath"].Value;

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    using var sw = File.AppendText(filePath);
                    sw.WriteLine(message);
                }

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

        private static DataTable _dtDoEksportu;
        private static List<object[]> _dtDoEksportuRows;

        private static DataGridView _currentDataGrid;
        private static ICustomDataGridView _currentMyGrid;
        private void DataGrid_MouseDown_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is DataGridView && (sender as DataGridView).Parent is CustomDataGridView)
            {
                _dtDoEksportu = ((sender as DataGridView).Parent as CustomDataGridView).CurrentDataTable;
                _dtDoEksportuRows = ((sender as DataGridView).Parent as CustomDataGridView).RowsList;
                _currentMyGrid = ((sender as DataGridView).Parent as CustomDataGridView);
            }
            else
            {
                _dtDoEksportu = null;
                _dtDoEksportuRows = null;
                _currentMyGrid = null;
            }

            _currentDataGrid = (sender as DataGridView);
            if (e.Button == MouseButtons.Right)
            {
                var hti = _currentDataGrid.HitTest(e.X, e.Y);
            }
        }
        static void ClearCurrentHelpReferences()
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

                if (IGeneralDbService.GeneralDic.TryGetValue(SelectedConnectionName, out var database))
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
                document?.UpdateEditorSelection(editor.SelectionStart, editor.SelectionLength, editor.SelectionStart);

                if (document is not null && !_executionRoutedThroughViewModel.Value)
                {
                    // Repeated keyboard input while a document is running is a
                    // no-op at the view boundary. The VM still rejects races
                    // between programmatic callers, but an async WinForms key
                    // handler must never surface that rejection as an
                    // unhandled application-closing exception.
                    if (document.SqlExecution.IsBusy)
                        return;

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

                    await document.SqlExecution.RunAsync(
                        document.BuildExecutionRequest(executionMode, outputMode, filePath) with
                        {
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
                    await RunNzSQLCore(CurrentUpper.KeepConnectionOpen, mode, exportOption, explain, filePath);
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

        public async Task RunNzSQL(bool keepConnectionOpen, int mode = 0, ExportOptions opcjaEksportu = ExportOptions.grid, bool explain = false, string filePath = null) =>
            await RunNzSQLCore(keepConnectionOpen, mode, opcjaEksportu, explain, filePath);

        async IAsyncEnumerable<JustData.Application.Sql.SqlExecutionEvent> ExecuteSqlForDocumentAsync(
            JustData.Application.Sql.SqlExecutionRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_documentIdsByEditor.FirstOrDefault(item => item.Value == request.DocumentId).Key is { } editor
                && !editor.IsDisposed)
            {
                if (_documentIdsByTab.FirstOrDefault(item => item.Value == request.DocumentId).Key is { } tab)
                    _tabManager.SelectTab(tab);

                if (!string.Equals(editor.Text, request.SqlText, StringComparison.Ordinal))
                    editor.Text = request.SqlText;
                editor.SelectionStart = Math.Clamp(request.SelectionStart, 0, editor.TextLength);
                editor.SelectionLength = Math.Clamp(request.SelectionLength, 0, editor.TextLength - editor.SelectionStart);
            }

            int mode = request.Mode switch
            {
                JustData.Application.Sql.SqlExecutionMode.RunToCursor => 4,
                JustData.Application.Sql.SqlExecutionMode.SingleBatch => 1,
                _ => 0
            };
            ExportOptions output = request.OutputMode switch
            {
                JustData.Application.Sql.SqlOutputMode.Csv => ExportOptions.csv,
                JustData.Application.Sql.SqlOutputMode.Xlsx => ExportOptions.xlsx,
                JustData.Application.Sql.SqlOutputMode.Xlsb => ExportOptions.xlsb,
                JustData.Application.Sql.SqlOutputMode.LogOnly => ExportOptions.onlyLog,
                _ => ExportOptions.grid
            };
            bool previousRouteValue = _executionRoutedThroughViewModel.Value;
            EditorDocumentId? previousExecutionDocumentId = _executingDocumentId.Value;
            HashSet<string> existingResultSetIds = _resultGridRegistry.GetDocument(request.DocumentId)
                .Select(item => item.ResultSetId)
                .ToHashSet(StringComparer.Ordinal);
            using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(
                () => CancelActiveDocumentExecution(request.DocumentId, request.ConnectionName));
            FastColoredTextBox? scriptEditor = null;
            int originalSelectionStart = 0;
            int originalSelectionLength = 0;
            if (request.Mode == JustData.Application.Sql.SqlExecutionMode.Script
                && _documentIdsByEditor.FirstOrDefault(item => item.Value == request.DocumentId).Key is { } candidateEditor
                && !candidateEditor.IsDisposed)
            {
                scriptEditor = candidateEditor;
                originalSelectionStart = candidateEditor.SelectionStart;
                originalSelectionLength = candidateEditor.SelectionLength;
                candidateEditor.SelectionStart = 0;
                candidateEditor.SelectionLength = candidateEditor.TextLength;
            }
            _executionRoutedThroughViewModel.Value = true;
            _executingDocumentId.Value = request.DocumentId;
            try
            {
                await RunSQL(mode, output, request.Explain, request.OutputPath).WaitAsync(cancellationToken);
                if (_sqlExecutionSessionRegistry.TryConsumeCancellation(request.DocumentId))
                {
                    _legacySqlFailureMessage = null;
                    yield return JustData.Application.Sql.SqlExecutionEvent.Completed(
                        request.DocumentId,
                        JustData.Application.Sql.SqlExecutionOutcome.Cancelled,
                        "SQL execution was cancelled.");
                    yield break;
                }

                string? legacyFailure = _legacySqlFailureMessage;
                _legacySqlFailureMessage = null;

                foreach (var resultEntry in _resultGridRegistry.GetDocument(request.DocumentId)
                    .Where(item => !existingResultSetIds.Contains(item.ResultSetId))
                    .Select((item, index) => (item.ResultSetId, item.Grid, index)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string resultSetId = resultEntry.ResultSetId;
                    CustomDataGridView grid = resultEntry.Grid;
                    if (grid.IsDisposed || grid.CurrentDataTable is null)
                        continue;

                    TabPage? tab = grid.Parent as TabPage;
                    TabPageResultsTag? tag = tab?.Tag as TabPageResultsTag;
                    JustData.Application.Sql.ResultSetDescriptor descriptor = new(
                        resultSetId,
                        tab?.Text ?? resultSetId,
                        grid.CurrentDataTable.Columns
                            .Cast<DataColumn>()
                            .Select((column, ordinal) => new JustData.Application.Sql.ResultColumnDescriptor(
                                ordinal,
                                column.ColumnName,
                                column.DataType.Name,
                                column.AllowDBNull))
                        .ToArray(),
                        StatementIndex: resultEntry.index,
                        IsPinned: tag?.Docked == true);
                    yield return JustData.Application.Sql.SqlExecutionEvent.Result(request.DocumentId, descriptor);

                    // The legacy grid already owns the only full row buffer.
                    // Report its count without cloning rows back through the VM.
                    yield return JustData.Application.Sql.SqlExecutionEvent.RowsObserved(
                        request.DocumentId,
                        grid.RowsList.Count,
                        resultSetId: resultSetId);
                }

                if (!string.IsNullOrWhiteSpace(legacyFailure))
                {
                    yield return JustData.Application.Sql.SqlExecutionEvent.Completed(
                        request.DocumentId,
                        JustData.Application.Sql.SqlExecutionOutcome.Failed,
                        legacyFailure);
                }
                else
                {
                    yield return JustData.Application.Sql.SqlExecutionEvent.Completed(
                        request.DocumentId,
                        JustData.Application.Sql.SqlExecutionOutcome.Success);
                }
            }
            finally
            {
                if (scriptEditor is not null && !scriptEditor.IsDisposed)
                {
                    scriptEditor.SelectionStart = Math.Clamp(originalSelectionStart, 0, scriptEditor.TextLength);
                    scriptEditor.SelectionLength = Math.Clamp(
                        originalSelectionLength,
                        0,
                        scriptEditor.TextLength - scriptEditor.SelectionStart);
                }
                _executionRoutedThroughViewModel.Value = previousRouteValue;
                _executingDocumentId.Value = previousExecutionDocumentId;
            }
        }

        private void CancelActiveDocumentExecution(
            JustData.Application.Editor.EditorDocumentId documentId,
            string connectionName)
        {
            _ = _sqlExecutionSessionRegistry.CancelAsync(documentId);

            if (!string.IsNullOrWhiteSpace(connectionName)
                && IGeneralDbService.GeneralDic.TryGetValue(connectionName, out var generalDb))
            {
                _ = generalDb.AbortAsync("x");
            }
        }

        private void SynchronizeSelectedResult(EditorDocumentId documentId)
        {
            if (_documentIdsByTab.FirstOrDefault(item => item.Value == documentId).Key is not { } tab)
                return;

            TabControl? results = (_tabManager.GetSplitContainerForTab(tab)?.Tag as ResultData)
                ?.TabControlSQLResults;
            string? resultSetId = (results?.SelectedTab?.Tag as TabPageResultsTag)?.ResultSetId;
            _editorWorkspaceViewModel.Documents
                .FirstOrDefault(document => document.Id == documentId)
                ?.SqlExecution.SelectResult(resultSetId);
        }

        private void RegisterLegacyResultGrid(TabPage tab, CustomDataGridView grid)
        {
            if (tab?.Tag is not TabPageResultsTag tag || string.IsNullOrWhiteSpace(tag.ResultSetId))
                return;

            if (tag.DocumentId is not { } documentId)
                return;

            string resultSetId = tag.ResultSetId;
            if (_resultGridRegistry.TryGet(documentId, resultSetId, out _))
                resultSetId = Guid.NewGuid().ToString("N");
            tag.ResultSetId = resultSetId;
            _resultGridRegistry.Register(documentId, resultSetId, grid);
            PrepareDocumentationShowcaseAfterFirstResult();
        }

    }
}
