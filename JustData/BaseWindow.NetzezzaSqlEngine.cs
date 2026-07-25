// Netezza SQL execution core extracted from BaseWindow.SqlExecution.cs.
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Common.Models;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Services;
using AppBase.Services.Helpers;
using AppBase.Services.Sql;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDriver;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.UI.Models;
using JustyBaseLegacy.UI.Sql;
using JustyBaseLegacy.UI.ImportExport;
using JustData.Application.Editor;
using SpreadSheetTasks;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI;

public partial class BaseWindow
{
    private void InvokeOnMainWindow(Action action)
    {
        if (IsDisposed || Disposing)
            return;

        if (InvokeRequired)
        {
            if (IsHandleCreated)
                Invoke(action);
            return;
        }

        action();
    }

        private async Task RunNzSQLCore(bool keepConnectionOpen, int mode = 0, ExportOptions opcjaEksportu = ExportOptions.grid, bool explain = false, string filePath = null)
        {
            SqlFirstRenderProbeRun? firstRenderProbeRun = SqlFirstRenderProbeRun.StartSqlExecution();
            _legacySqlFailureMessage = null;

            if (CurrentTB is null)
            {
                return;
            }
            RestyleCurrentTb();


            var currentMainTab = ActiveEditorTabPage;
            var fctbFromStart = CurrentTB;
            bool continueOnError = CurrentUpper.ContinueOnError;

            string tabName = ActiveEditorTabPage?.Text ?? string.Empty;
            lock (_sync)
            {
                fctbFromStart.Focus();
            }

            string connectionName = SelectedConnectionName;
            string selectedDb = SelectedDatabase;

            string queryOrg = default;

            if (mode == 4)
            {
                int selStart = fctbFromStart.SelectionStart + fctbFromStart.SelectionLength;
                fctbFromStart.SelectionStart = 0;
                fctbFromStart.SelectionLength = selStart;
            }

            if (fctbFromStart.Selection.TextLength >= 2)//jak zaznaczone uruchom tylko zaznaczone
            {
                queryOrg = fctbFromStart.Selection.Text;
            }
            else
            {
                fctbFromStart.SelectBetweenSemicolons();
                queryOrg = fctbFromStart.Selection.Text;
            }

            int selectionStart = fctbFromStart.SelectionStart;
            int selectionLength = fctbFromStart.SelectionLength;
            int goodSelectionLength = 0;

            if (explain)
            {
                queryOrg = "explain verbose " + queryOrg; // ewentualnie do poprawki na znalezienie ostatniego with/select
            }

            if (queryOrg.Length < 200 && _specialActions.TryGetValue(queryOrg, out var action))
            {
                action?.Invoke();
                return;
            }

            var (sql, filepath, exportOption) = await PrepareSQLAsync(queryOrg);

            string queryClean = sql;
            if (string.IsNullOrEmpty(queryClean))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(queryClean))
            {
                //this.Invoke(() => _loggerLoud.MessageBox_Show("nothing to execute", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information));
                _loggerLoud.MessageBox_Show(this, "Nothing to execute.", "SQL", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            EditorDocumentId executionDocumentId = _documentIdsByEditor.TryGetValue(fctbFromStart, out var mappedDocumentId)
                ? mappedDocumentId
                : CurrentEditorDocumentId ?? throw new InvalidOperationException("No active editor document is available.");
            if (!_sqlExecutionSessionRegistry.TryStart(executionDocumentId, SelectedConnectionName, out ISqlExecutionSession executionSession))
            {
                _loggerLoud.MessageBox_Show(this,
                    "A SQL command is already running for this document.",
                    "Concurrent commands",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            NzConnection noticeNzConnection = null;
            NzConnection.NzNoticeEventHandler noticeHandler = null;

            try
            {
            filePath = filepath;
            if (exportOption != ExportOptions.noInfo)
            {
                opcjaEksportu = exportOption;
            }


            if (!TabConnectionCache.Default.TryGet(fctbFromStart, out var tabConnectionData))
            {
                if (!_connectionSessions.TryGetValue(connectionName, out IGeneralDb db1))
                {
                    db1 = new Netezza(_databaseRuntimeContext, _loggerLoud, _importExportTasks, _generalDbService)
                    {
                        ConnectionString = _generalDbService.ConnectionStringForNz(_applicationSettingsContext.Config.ConnectionTimeout, connectionName),
                        ConnectionName = connectionName,
                        Username = _generalDbService.UserName(connectionName),
                        LogErrorStdColor = MyColors.LogErrorStdColor
                    };
                    _connectionSessions.Set(connectionName, db1);
                }

                var conn1 = db1.GetConnection(SelectedDatabase, usePool: false);
                tabConnectionData = new TabConnectionData()
                {
                    Connection = conn1,
                    CloseConnectionByDefault = !keepConnectionOpen,
                    ConnectionName = connectionName,
                    DatabaseName = SelectedDatabase
                };
                TabConnectionCache.Default.Set(fctbFromStart, tabConnectionData);
            }
            else
            {
                if (tabConnectionData.ConnectionName != connectionName)
                {
                    tabConnectionData.ConnectionName = connectionName;
                    tabConnectionData.DatabaseName = SelectedDatabase;
                }
                else if (tabConnectionData.DatabaseName != SelectedDatabase)
                {
                    tabConnectionData.DatabaseName = SelectedDatabase;
                }
            }


            bool czyZaknacPoWykonaniu = !keepConnectionOpen;
            TabConnectionData dp = tabConnectionData;

            DbConnection connection;

            if (!_connectionSessions.TryGetValue(connectionName, out var generalDbForConnection))
            {
                _sqlExecutionSessionRegistry.Complete(executionDocumentId);
                _loggerLoud.MessageBox_Show(this, "Not connected.", "Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var conn = generalDbForConnection.GetConnection(selectedDb, usePool: false);
            if (!czyZaknacPoWykonaniu && dp.Connection?.ConnectionString == conn.ConnectionString)
            {
                connection = dp.Connection;
                //connection.ChangeDatabase(selectedDb);
                int howMany = dp.Commands.Count;
                if (howMany > 0)
                {
                    //this.Invoke(() => _loggerLoud.MessageBox_Show($"{howMany} commands still running - this mode dont support concurrent commands - wait/drop session or change tab"));
                    _loggerLoud.MessageBox_Show(this, $"{howMany} commands are still running. This mode does not support concurrent commands — wait, drop the session, or switch tabs.", "Concurrent commands", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _sqlExecutionSessionRegistry.Complete(executionDocumentId);
                    return;
                }
            }
            else
            {
                connection = conn;
                TabConnectionCache.Default.GetOrCreate(fctbFromStart).Connection = connection;
            }

            DeleteUndockedTabs();

            TabPagePicture currentResultsTab = null;


            if (this.InvokeRequired)
            {
                currentResultsTab = PrepareTab(isLogTab: true);
            }
            else
            {
                InvokeOnMainWindow(() =>
                {
                    currentResultsTab = PrepareTab(isLogTab: true);
                });
            }



            //await Task.Delay(5000);

            var firstFCTB = CurrentTB;


            currentResultsTab.IsRunning = true;
            currentResultsTab.IsSuccess = true;

            if (!fctbFromStart.Focused && CurrentTB != null && fctbFromStart == CurrentTB)
            {
                fctbFromStart.Focus();
            }

            Button btAbort = new Button();
            btAbort.Text = "Abort !";
            btAbort.Click += BtAbort_Click;
            btAbort.Enabled = false;
            btAbort.ForeColor = Color.Red;
            btAbort.Font = new Font(btAbort.Font, FontStyle.Bold);
            ProgressBar progressBarSQL = new CustomProgressBar();
            progressBarSQL.Step = 1;

            var sqlLog = PrepareSqlLog(currentResultsTab, btAbort);

            currentResultsTab.Controls.Add(progressBarSQL);
            currentResultsTab.Controls.Add(btAbort);
            currentResultsTab.Controls.Add(sqlLog.View);
            LayoutResultsToolbar(currentResultsTab, btAbort, progressBarSQL, sqlLog.View);
            currentResultsTab.Resize += (_, _) => LayoutResultsToolbar(currentResultsTab, btAbort, progressBarSQL, sqlLog.View);


            string[] sqlsArray = default;

            if ((mode == 0 || mode == 3 || mode == 4) && opcjaEksportu != ExportOptions.xlsx)
            {
                sqlsArray = queryClean.SqlSplitAdvanced(';').Select(arg => arg.Trim()).Where(arg => arg.Length >= 2).ToArray();
            }
            else if (mode == 1 || opcjaEksportu == ExportOptions.xlsx)
            {
                if (queryClean.Length >= 3)
                {
                    sqlsArray = new string[] { queryClean };
                }
                else
                {
                    sqlsArray = Array.Empty<string>();
                }
            }
            else if (mode == 2) // procedure
            {
                if (queryClean.Length >= 3)
                {
                    sqlsArray = new string[] { $"CALL SP_ANWB_EXEC_IMMEDIATELY('{queryOrg.Replace("'", "''")}');" };
                    // SP_ANWB_EXEC_IMMEDIATELY = execute immediate 'sql'
                }
                else
                {
                    sqlsArray = Array.Empty<string>();
                }
            }

            if (opcjaEksportu == ExportOptions.csv)
            {
                if (saveFileCSV.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileCSV.FileName;
                }
            }
            else if (opcjaEksportu == ExportOptions.xlsx)
            {
                if (_applicationSettingsContext.Config.UseXlsb)
                {
                    saveFileXlsx.Filter = saveFileXlsx.Filter.Replace("xlsx", "xlsb");
                }
                if (saveFileXlsx.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileXlsx.FileName;
                }
                else
                {
                    sqlLog.AppendEntry(DateTime.Now, -1, connectionName, selectedDb, $"ExecuteReader - finished");
                    currentResultsTab.IsRunning = false;
                    currentResultsTab.IsSuccess = false;
                    // currentResultsTab.Invalidate();
                    return;
                }
            }

            progressBarSQL.Step = 1;
            Stopwatch st = new Stopwatch();
            st.Start();
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    InvokeOnMainWindow(() =>
                    {
                        sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, "connecting");
                    });

                    await Task.Run(() => connection.Open());

                    if (InvokeRequired && IsHandleCreated)
                    {
                        Invoke(() =>
                        {
                            sqlLog?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, "connected");
                        });
                    }
                    else
                    {
                        sqlLog?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, "connected");
                    }

                }
            }
            catch (Exception ex)
            {
                _loggerLoud.MessageBox_Show(this, $"{DateTime.Now:u} — connection.Open() failed: {ex.Message}", "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DbConnection connectionForCost = null;

            int ssid = -1;
            int processID = -1;
            if (connection is NzConnection nETConnection1)
            {
                processID = nETConnection1.Pid;
                noticeNzConnection = nETConnection1;
                noticeHandler = (_, noticeArgs) =>
                {
                    string noticeMessage = noticeArgs.Message;
                    if (string.IsNullOrWhiteSpace(noticeMessage))
                    {
                        return;
                    }

                    sqlLog.AppendEmphasisEntry(
                        DateTime.Now,
                        st.Elapsed.TotalSeconds.ToString("F1"),
                        connectionName,
                        selectedDb,
                        $"ℹ NOTICE: {noticeMessage.TrimEnd()}");
                };
                noticeNzConnection.NoticeReceived += noticeHandler;
            }
            else
            {
                try
                {
                    await Task.Run(() =>
                    {
                        object c = null;
                        try
                        {
                            using (DbCommand csid = connection.CreateCommand())
                            {
                                csid.CommandTimeout = 5;
                                csid.CommandText = NetezzaHelpers.SESSION;
                                c = csid.ExecuteScalar();
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        if (c != null)
                        {
                            ssid = (int)c;
                        }
                    });
                }
                catch (Exception ex)
                {
                    ssid = -1;
                    _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }


            if (sqlsArray.Length == 0)
            {
                InvokeOnMainWindow(() =>
                {
                    sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, "nothing to execute");
                });
            }

            int queryNum = 0;
            int csvQueryNum = 0;
            if (sqlsArray.Length > 0)
            {
                using (var cmdCatalog = connection.CreateCommand())
                {
                    try
                    {
                        cmdCatalog.CommandText = "SELECT CURRENT_CATALOG";

                        var res = cmdCatalog.ExecuteScalar() as string;
                        if (res is not null && res != selectedDb)
                        {
                            var mb = MessageBox.Show(this, $"Current database ({res}) differs from the selected database ({selectedDb}). Continue?", "Different database", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (mb != DialogResult.Yes)
                            {
                                sqlsArray = Array.Empty<string>();
                            }
                            else
                            {
                                SelectedDatabase = res;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine($"Netezza command cleanup failed: {exception.GetType().Name}");
                    }
                }
            }

            Stopwatch stDelayTab = new Stopwatch();
            bool doBreak = false;
            Memory<char> charsStr = new char[1024];

            foreach (string queryBase in sqlsArray)
            {
                doBreak = executionSession.IsCancelling;
                if (doBreak)
                {
                    lock (_sync)
                    {
                        this?.Invoke(() =>
                        {
                            sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"Aborted");
                            progressBarSQL.Maximum++;
                            progressBarSQL.Value = progressBarSQL.Maximum;
                            progressBarSQL.Maximum--;
                            progressBarSQL.Enabled = false;
                            if (progressBarSQL is CustomProgressBar customProgress)
                            {
                                customProgress.SetState(2);
                            }
                        });
                    }
                    break;
                }

                if (queryBase.IsAllComments())
                {
                    lock (_sync)
                    {
                        this.Invoke(() =>
                        {
                            progressBarSQL.Value = (int)(100.0 * (++queryNum) / sqlsArray.Length);
                        });
                    }
                    continue;
                }

                string query = await SpecialCommandsAsync(queryBase);
                query = await ReplaceAndSetSessionVariables(query, tabName, connection);

                if (string.IsNullOrWhiteSpace(query))
                {
                    lock (_sync)
                    {
                        this.Invoke(() =>
                        {
                            progressBarSQL.Value = (int)(100.0 * (++queryNum) / sqlsArray.Length);
                        });
                    }
                    continue;
                }
                AddHistoryEntry(query, SelectedDatabase, connectionName);
                List<DbCommand> commandsList = dp.Commands;
                try
                {
                    if (query.StartsWith("BLOB" + Environment.NewLine))
                    {
                        query = query.Substring("BLOB".Length + Environment.NewLine.Length);
                        int par = sql.IndexOf($"{Environment.NewLine}PATHS{Environment.NewLine}");
                        if (par == -1)
                        {
                            throw new Exception("line with 'PATHS' is required");
                        }
                        var paths = query.Substring(par + Environment.NewLine.Length + 1).Split(Environment.NewLine);
                        int parCnt = sql.Length - sql.Replace("?", "").Length;
                        if (paths.Length != parCnt)
                        {
                            throw new Exception("number of paths and '?' mark must equals");
                        }
                        query = query.Substring(0, par - 6);

                        int num = 0;
                        query = Regex.Replace(query, @"\?", (match) =>
                        {
                            var path = paths[num++];
                            char[] chars = null;
                            using (var fs = File.OpenRead(path))
                            {
                                if (fs.Length > 64000)
                                {
                                    //this.Invoke(() => _loggerLoud.MessageBox_Show($"File {path} is to big", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                                    _loggerLoud.MessageBox_Show(this, $"File {path} is too large.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return "?";
                                }
                                chars = new char[(int)fs.Length * 2 + 3];
                                chars[0] = 'x';
                                chars[1] = '\'';
                                chars[(int)fs.Length * 2 + 2] = '\'';

                                byte[] buffer = new byte[4096];
                                int readed = 1;
                                int writted = 2;
                                while (readed > 0)
                                {
                                    readed = fs.Read(buffer, 0, buffer.Length);
                                    for (int l = 0; l < readed; l++)
                                    {
                                        buffer[l].TryFormat(chars.AsSpan().Slice(writted), out _, "x2");
                                        writted += 2;
                                    }
                                }
                            }

                            return new string(chars);
                        });
                    }

                }
                catch (Exception)
                {
                    query = "SELECT 'BLOB ERROR';";
                }

                var tt1 = await DoSpecialTask(fctbFromStart, query, sqlLog, st);
                if (tt1)
                {
                    lock (_sync)
                    {
                        this.Invoke(() =>
                        {
                            progressBarSQL.Value = (int)(100.0 * (++queryNum) / sqlsArray.Length);
                        });
                    }

                    continue;
                }


                if (!RiskySqlCommand(query, true))
                {
                    break;
                }



                DbCommand command = null;
                InvokeOnMainWindow(() =>
                {
                    //command = new OdbcCommand(query, connection as OdbcConnection) { CommandTimeout = config.CommandTimeout };
                    //command = generalDic[connectionName].GetCommand(query, connection);
                    command = connection.CreateCommand();
                    command.CommandTimeout = _applicationSettingsContext.Config.CommandTimeout;
                    command.CommandText = query;
                });
                executionSession.SetConnection(connection, ownsConnection: false);
                executionSession.SetCommand(command, ownsCommand: false);
                executionSession.SetProviderAbort(() => generalDbForConnection.AbortAsync("x"));

                this.Invoke(() =>
                {
                    btAbort.Enabled = true;
                    btAbort.Tag = new ConnectionData() { Cmd = command, Conn = connection, Ssid = ssid, ProcessID = processID, DocumentId = executionDocumentId };
                    commandsList.Add(command);
                });

                InvokeOnMainWindow(() =>
                {
                    sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"SQL started", query);
                });


                DbDataReader rdr = null;
                DataTable? shemaTable = null;
                try
                {
                    Task mainQueryTask = Task.Run(() =>
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            if (command != null)
                            {
                                try
                                {
                                    rdr = command.ExecuteReader();
                                }
                                catch (NullReferenceException ex)
                                {
                                    _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }

                                if (rdr.FieldCount != -1)
                                {
                                    try
                                    {
                                        shemaTable = rdr.GetSchemaTable();
                                    }
                                    catch (ArgumentException ex)
                                    {
                                        if (ex.Message == "Unknown SQL type - 110.")
                                        {
                                            // interval = ok
                                            shemaTable = null;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        throw;
                                    }
                                }
                            }
                        }
                    });

                    int l = 0;
                    while (mainQueryTask.Status == TaskStatus.Running || mainQueryTask.Status == TaskStatus.WaitingToRun)
                    {
                        await Task.Delay(100);
                        l++;
                        if (l % _applicationSettingsContext.Config.LongQueryWarning == 0)
                        {
                            //this.Invoke(() => _loggerLoud.MessageBox_Show($"query in session session id: {ssid}/ process id: {processID} is still running.."));
                            _loggerLoud.MessageBox_Show(this, $"Query in session {ssid} (process {processID}) is still running.", "Query running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        if (l % _applicationSettingsContext.Config.EstimatedWarningInterval == 0 || l == 100) // from time to time show cost
                        {
                            long costInt = default;
                            string inf = "";
                            try
                            {
                                object constObj = null;
                                await Task.Run(() =>
                                {
                                    DbCommand cost = null;
                                    if (connectionForCost is null
                                        && _connectionSessions.TryGetValue(connectionName, out var generalDbForCost))
                                    {
                                        connectionForCost = generalDbForCost.GetConnection(selectedDb);
                                        connectionForCost.Open();
                                        cost = connectionForCost.CreateCommand();
                                        cost.CommandText = NetezzaSystemSql.GetEstimatedQueryCost(processID, ssid);
                                    }
                                    try
                                    {
                                        constObj = cost?.ExecuteScalar();
                                    }
                                    catch (Exception exception)
                                    {
                                        Trace.WriteLine($"Netezza result processing failed: {exception.GetType().Name}");
                                    }

                                });

                                if (constObj == null || constObj == DBNull.Value)
                                {
                                    inf = "no cost avaliable";
                                }
                                else
                                {
                                    costInt = (Int64)constObj;
                                }
                            }
                            catch (Exception ex)
                            {
                                costInt = -1;
                                inf = ex.Message;
                            }

                            try
                            {
                                if (costInt > _applicationSettingsContext.Config.EstimatedWarning)
                                {
                                    _loggerLoud.MessageBox_Show(this, $"Session {ssid}: approximately {(costInt / 1000).ToString("N0")} seconds remaining.", "Query progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }

                                InvokeOnMainWindow(() =>
                                    {
                                        sqlLog.AppendEmphasisEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"{(costInt / 1000).ToString("N0")} estimated seconds {inf}");
                                    });
                            }
                            catch (Exception ex)
                            {
                                _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    await mainQueryTask;
                }
                catch (Exception e)
                {
                    RecordLegacySqlFailure(e.Message);
                    InvokeOnMainWindow(() =>
                    {
                        btAbort.Enabled = false;
                        //tekst.Text += $"\r\n{DateTime.Now} - {e.Message}";
                        string msgX = e.Message.Length > 300 ? (e.Message[0..100] + "[...]" + e.Message[^200..]) : e.Message;
                        msgX = Regex.Replace(msgX, @"\s{5,}", " ");

                        if (string.IsNullOrWhiteSpace(msgX))
                        {
                            msgX = "database returned an empty message";
                        }

                        sqlLog.AppendErrorEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, msgX);
                        if (progressBarSQL is CustomProgressBar customProgress)
                        {
                            //customProgress.Alarm = true;
                            customProgress.SetState(2);
                        }

                        // Always mark failure so FinalizeSqlRun keeps the Log tab selected.
                        currentResultsTab.IsSuccess = false;
                        currentResultsTab.Parent?.Invalidate();
                    });
                    if (e is DbException)
                    {
                        InvokeOnMainWindow(() =>
                        {
                            if (e.Message == "ERROR [HY000] ERROR:  Transaction rolled back by client\n" || !continueOnError)
                            {
                                progressBarSQL.Maximum++;
                                progressBarSQL.Value = progressBarSQL.Maximum;
                                progressBarSQL.Maximum--;
                                progressBarSQL.Enabled = false;
                                currentResultsTab.IsRunning = false;
                                currentResultsTab.IsSuccess = false;
                                commandsList.Remove(command);
                                currentResultsTab?.Parent?.Invalidate();
                                try
                                {
                                    string msg = e.Message;
                                    if (e is NetezzaException)
                                    {
                                        HandleNzErrors(msg, fctbFromStart, selectionStart + goodSelectionLength, selectionLength - goodSelectionLength);
                                    }
                                    else if (msg.Length < 1000 || msg.Contains(" ^ found \"") || msg.Contains("at char "))
                                    {
                                        HandleNzErrors(msg, fctbFromStart, selectionStart + goodSelectionLength, selectionLength - goodSelectionLength);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }

                        });
                        if (e.Message == "ERROR [HY000] ERROR:  Transaction rolled back by client\n" || !continueOnError)
                        {
                            if (czyZaknacPoWykonaniu)
                            {
                                await Task.Run(() => connection.Close());
                                //connection.Close();
                            }
                            else
                            {
                                InvokeOnMainWindow(() =>
                                {
                                    commandsList.Remove(command);
                                });
                            }

                            if (connectionForCost is not null && connectionForCost.State == ConnectionState.Open)
                            {
                                await Task.Run(() => connectionForCost.Close());
                                //connectionForCost.Close();
                            }
                            // Keep Log selected and surface the error the same way a normal finish does.
                            lock (_sync)
                            {
                                FinalizeSqlRun(currentMainTab, fctbFromStart, currentResultsTab);
                            }
                            return;
                        }
                    }
                }

                try
                {
                    if (currentResultsTab.InvokeRequired)
                    {
                        InvokeOnMainWindow(() =>
                        {
                            commandsList.Remove(command);
                        });
                    }
                    else
                    {
                        commandsList.Remove(command);
                    }
                }
                catch (Exception ex)
                {
                    //this.Invoke(() => _loggerLoud.MessageBox_Show($"RunSQL - {ex.Message}"));
                    _loggerLoud.MessageBox_Show(this, ex.Message, "SQL execution error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


                InvokeOnMainWindow(() =>
                {
                    sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"ExecuteReader - finished");
                });



                if (rdr != null)
                {
                    InvokeOnMainWindow(() =>
                    {
                        //xxx1
                        //btAbort.Enabled = false;
                        sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"RecordsAffected: {rdr.RecordsAffected} (driver info)");
                        progressBarSQL.Enabled = false;
                    });
                }
                //xxx?
                doBreak = executionSession.IsCancelling;


                try
                {


                    if (!doBreak && rdr != null && !rdr.IsClosed && (rdr.HasRows || rdr.FieldCount > 0)) // Reader returned data.
                    {
                        InvokeOnMainWindow(() =>
                        {
                            sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"ExecuteReader - receiving data");
                        });

                        var fetchSession = new LegacyNetezzaResultFetchSession();
                        try
                        {
                            do
                            {
                                fetchSession.BeginResultSet();
                                DataTable dtWynikiForGrid = DataFuncs.Default.GetDataTable(rdr, onErrorMessage: msg => InvokeOnMainWindow(() => _loggerLoud.MessageBox_Show(this, msg, "Column", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                                List<object[]> dataForGrid = new List<object[]>();


                                int romNumber = 0;


                                string extraInfo = "";
                                if (opcjaEksportu == ExportOptions.grid)
                                {

                                    long elaps = 0;
                                    if (!stDelayTab.IsRunning)
                                    {
                                        stDelayTab.Start();
                                    }
                                    elaps = stDelayTab.ElapsedMilliseconds;
                                    if (elaps < 100)
                                    {
                                        await Task.Delay((int)(100 - elaps));
                                    }
                                    stDelayTab.Restart();

                                    CustomDataGridView myDataGridView = null;
                                    TabControl activeTabControl = (currentResultsTab.Tag as TabPageResultsTag)?.ParentControl
                                        ?? ((DockSuiteTabManager)_tabManager).GetResultsToolWindow()?.TabControl;
                                    if (activeTabControl is null) continue;

                                    TabPagePicture tabAktualnaZakladkaX = new TabPagePicture();
                                    tabAktualnaZakladkaX.CloseImage = _normalXimage;

                                    if (_applicationSettingsContext.Config.PinDataByDefault)
                                    {
                                        tabAktualnaZakladkaX.Tag = new TabPageResultsTag()
                                        {
                                            Docked = true,
                                            ParentControl = activeTabControl,
                                            DocumentId = (currentResultsTab.Tag as TabPageResultsTag)?.DocumentId
                                                ?? _executingDocumentId.Value
                                                ?? CurrentEditorDocumentId
                                        };
                                        tabAktualnaZakladkaX.Text = ResultsTabNaming.NextResultTitle(activeTabControl);
                                        tabAktualnaZakladkaX.PinImage = _activePinImage;
                                    }
                                    else
                                    {
                                        tabAktualnaZakladkaX.Tag = new TabPageResultsTag()
                                        {
                                            Docked = false,
                                            ParentControl = activeTabControl,
                                            DocumentId = (currentResultsTab.Tag as TabPageResultsTag)?.DocumentId
                                                ?? _executingDocumentId.Value
                                                ?? CurrentEditorDocumentId
                                        };
                                        tabAktualnaZakladkaX.Text = ResultsTabNaming.NextResultTitle(activeTabControl);
                                        tabAktualnaZakladkaX.PinImage = _normalPinImage;
                                    }

                                    void EnsureResultTabAttached()
                                    {
                                        if (fetchSession.TabAttached || activeTabControl is null)
                                            return;
                                        lock (_sync)
                                        {
                                            if (fetchSession.TabAttached)
                                                return;
                                            activeTabControl.TabPages.Add(tabAktualnaZakladkaX);
                                            activeTabControl.SelectedTab = tabAktualnaZakladkaX;
                                            fetchSession.MarkTabAttached();

                                            if (CurrentTB != null && fctbFromStart == CurrentTB && !fctbFromStart.Focused)
                                            {
                                                CurrentTB.Focus();
                                            }
                                        }
                                    }

                                    myDataGridView = new CustomDataGridView(_colorTheme, _importExportTasks, _uiHelperService, fctbFromStart, dtWynikiForGrid, dataForGrid, firstRenderProbeRun)
                                    {
                                        Name = $"resultGrid_{tabAktualnaZakladkaX.Text}",
                                        ResultGridAccessibilityName = $"resultGrid_{Guid.NewGuid():N}",
                                        Dock = DockStyle.Fill,
                                        DoMessageAction = DoMessage
                                    };

                                    myDataGridView.NewSqlTabRequested += (_, _) => OpenNewSqlDocument();

                                    myDataGridView.AttachedSQL = queryOrg;

                                    _colorTheme.ColorMyDataGridView(myDataGridView);
                                    DataGridDpiHelper.Apply(myDataGridView);

                                    myDataGridView.DateTimeFormat = _applicationSettingsContext.Config.DateTimeFormat;
                                    myDataGridView.DecimalFormat = _applicationSettingsContext.Config.DecimalFormat;
                                    myDataGridView.IntegerFormat = _applicationSettingsContext.Config.IntegerFormat;
                                    myDataGridView.ForceDecimalFormat = _applicationSettingsContext.Config.ForceDecimalFormat;
                                    myDataGridView.AutoSizeColumnsMode = _applicationSettingsContext.Config.AutoSizeColumnsMode;

                                    tabAktualnaZakladkaX.Controls.Add(myDataGridView);
                                    myDataGridView.ShemaDataTable = shemaTable;

                                    if (doBreak)
                                        break;

                                    void MarkLegacyFetchFailed()
                                    {
                                        if (currentResultsTab.IsDisposed)
                                            return;
                                        InvokeOnMainWindow(() =>
                                        {
                                            currentResultsTab.IsSuccess = false;
                                            if (progressBarSQL is CustomProgressBar customProgress)
                                                customProgress.SetState(2);
                                            currentResultsTab.Parent?.Invalidate();
                                        });
                                    }
                                    await Task.Run(() =>
                                    {
                                        try
                                        {
                                            Stopwatch st = new Stopwatch();
                                            st.Start();

                                            object[] rowX = new object[rdr.FieldCount];

                                            int[] isDateType = new int[rdr.FieldCount];
                                            for (int i = 0; i < rdr.FieldCount; i++)
                                            {
                                                if (rdr.GetDataTypeName(i) == "interval")
                                                {
                                                    isDateType[i] = 2;
                                                }
                                                else if (rdr.GetFieldType(i) == typeof(DateTime))
                                                {
                                                    isDateType[i] = 1;
                                                }
                                                else
                                                {
                                                    isDateType[i] = 0;
                                                }
                                            }
                                            if (isDateType.Any(a => (a == 2)))
                                            {
                                                InvokeOnMainWindow(() =>
                                                {
                                                    sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"at least on of column is interval, due of odbc driver limitations receiving data will be much longer, cast to varchar can fix this");
                                                });
                                            }
                                            while (rdr.Read())
                                            {
                                                if (romNumber == _applicationSettingsContext.Config.ResultRowsLimitWarning)
                                                {
                                                    DialogResult r = DialogResult.None;

                                                    r = _loggerLoud.MessageBox_Show(this, $"{_applicationSettingsContext.Config.ResultRowsLimitWarning.ToString("N0")} rows received - continue? ", "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                                                    if (r == DialogResult.No)
                                                    {
                                                        command.Cancel();
                                                        try
                                                        {
                                                            rdr.Close();
                                                        }
                                                        catch (NetezzaException ex)
                                                        {
                                                            if (ex.Message != "ERROR: Query was cancelled.")
                                                            {
                                                                _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                            }
                                                        }

                                                        break;
                                                    }
                                                }

                                                if (!(++romNumber > _applicationSettingsContext.Config.ResultRowsLimit))
                                                {
                                                    if (fetchSession.ShouldAttachTabForRow(romNumber))
                                                    {
                                                        // Attach only — register once on the success completion path
                                                        // to avoid duplicate ResultSetId entries in the VM registry.
                                                        InvokeOnMainWindow(EnsureResultTabAttached);
                                                    }
                                                    for (int i = 0; i < rdr.FieldCount; i++)
                                                    {
                                                        if (isDateType[i] == 2) // interval
                                                        {
                                                            object ob = null;
                                                            try
                                                            {
                                                                ob = rdr.GetString(i);
                                                            }
                                                            catch (Exception)
                                                            {
                                                                ob = rdr.GetValue(i);
                                                            }
                                                            rowX[i] = ob;
                                                        }
                                                        else if (mode == 3 && isDateType[i] == 1) // infinity mode
                                                        {
                                                            object ob = null;
                                                            try
                                                            {
                                                                ob = rdr.GetValue(i);
                                                                rowX[i] = ob;
                                                            }
                                                            catch (Exception)
                                                            {
                                                                ob = rdr.GetString(i);
                                                                if (((string)ob.ToString()) == "infinity")
                                                                {
                                                                    rowX[i] = DateTime.MaxValue;
                                                                }
                                                                else if (((string)ob.ToString()) == "-infinity")
                                                                {
                                                                    rowX[i] = DateTime.MinValue;
                                                                }
                                                            }
                                                        }
                                                        else if (rdr.GetFieldType(i) == typeof(string) || rdr.GetFieldType(i) == typeof(Memory<byte>))
                                                        {
                                                            var val1 = rdr.GetValue(i);
                                                            if (val1 is null || val1 == DBNull.Value)
                                                            {
                                                                rowX[i] = val1;
                                                            }
                                                            else
                                                            {
                                                                if (val1 is Memory<byte> mem)
                                                                {
                                                                    if (mem.Length > 256)
                                                                    {
                                                                        rowX[i] = Encoding.UTF8.GetString(mem.Span);
                                                                    }
                                                                    else
                                                                    {
                                                                        //256 limit
                                                                        //if (mem.Length > charsStr.Length)
                                                                        //{
                                                                        //    charsStr = new char[mem.Length];
                                                                        //}
                                                                        int len = Encoding.UTF8.GetChars(mem.Span, charsStr.Span);
                                                                        rowX[i] = new string(charsStr.Span[..len]);
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    rowX[i] = val1.ToString();
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            rowX[i] = rdr.GetValue(i);
                                                        }
                                                    }

                                                    var rowY = new object[rowX.Length + DatabaseDataGridView.WinForms.CustomDataGridView.TechColsNum];
                                                    rowX.CopyTo(rowY, 0);
                                                    dataForGrid.Add(rowY);
                                                }
                                                else
                                                {
                                                    command.Cancel();
                                                    rdr.Close();
                                                    break;
                                                }

                                                if (romNumber == 500)
                                                {
                                                    myDataGridView.InitGrid(true);
                                                }
                                                else if (romNumber % 50_000 == 0)
                                                {
                                                    if (!currentResultsTab.IsDisposed)
                                                    {
                                                        InvokeOnMainWindow(() =>
                                                        {
                                                            statusTextBox.Text = extraInfo;
                                                            sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"transfered {romNumber.ToString("N0")} rows");
                                                        });
                                                    }
                                                }
                                            }

                                            myDataGridView.EnsureColumnList();
                                        }
                                        catch (DbException dbExc)
                                        {
                                            // NetezzaException : DbException : SystemException — must be caught
                                            // before SystemException or type/SQL errors during Read() are swallowed,
                                            // leaving an empty Result tab and only a hard-to-see Log row.
                                            fetchSession.OnFetchFault(dbExc);
                                            RecordLegacySqlFailure(dbExc.Message);
                                            // Do not Cancel/Close here — on a broken NZ socket this blocks ~5–30s
                                            // and a later Close then pops a transport MessageBox. stopResultSets
                                            // skips NextResult; connection cleanup closes the socket later.

                                            if (!currentResultsTab.IsDisposed)
                                            {
                                                InvokeOnMainWindow(() =>
                                                {
                                                    statusTextBox.Text = extraInfo;
                                                    string msgX = dbExc.Message.Length > 300
                                                        ? (dbExc.Message[0..100] + "[...]" + dbExc.Message[^200..])
                                                        : dbExc.Message;
                                                    msgX = Regex.Replace(msgX, @"\s{5,}", " ");
                                                    if (string.IsNullOrWhiteSpace(msgX))
                                                        msgX = "database returned an empty message";

                                                    sqlLog.AppendErrorEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, msgX);
                                                    if (progressBarSQL is CustomProgressBar customProgress)
                                                    {
                                                        customProgress.SetState(2);
                                                    }
                                                    currentResultsTab.IsSuccess = false;
                                                    currentResultsTab.Parent?.Invalidate();

                                                    try
                                                    {
                                                        HandleNzErrors(
                                                            dbExc.Message,
                                                            fctbFromStart,
                                                            selectionStart + goodSelectionLength,
                                                            selectionLength - goodSelectionLength);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    }
                                                });
                                            }
                                        }
                                        catch (SystemException sExc)
                                        {
                                            // Transport / IO failures after a SQL error must stop the NextResult loop;
                                            // otherwise empty Result tabs keep spawning every few seconds.
                                            fetchSession.OnFetchFault(sExc);
                                            if (fetchSession.SoftBreak)
                                                doBreak = true;
                                            else
                                                RecordLegacySqlFailure(sExc.Message);

                                            if (!currentResultsTab.IsDisposed)
                                            {
                                                InvokeOnMainWindow(() =>
                                                {
                                                    statusTextBox.Text = extraInfo;
                                                    sqlLog.AppendErrorEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, sExc.Message);
                                                    if (progressBarSQL is CustomProgressBar customProgress)
                                                    {
                                                        customProgress.SetState(2);
                                                    }
                                                    currentResultsTab.IsSuccess = false;
                                                    currentResultsTab.Parent?.Invalidate();
                                                });
                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.Message == "ERROR [22007] Invalid datetime format")
                                            {
                                                //this.Invoke(() => _loggerLoud.MessageBox_Show("if you expect infinity datetime value please use Ctrl + F9 \r\n(due of odbc driver limitations receiving data will be much longer, cast to varchar can fix this)"));
                                                _loggerLoud.MessageBox_Show(this, "If you expect an infinity datetime value, use Ctrl+F9.\r\nDue to ODBC driver limitations, receiving data will take much longer; casting to VARCHAR can fix this.", "ODBC datetime", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            }
                                            else if (ex.Message == "Year, Month, and Day parameters describe an un-representable DateTime.")
                                            {
                                                //this.Invoke(() => _loggerLoud.MessageBox_Show("Odbc driver can not handle some dates please cast to varchar"));
                                                _loggerLoud.MessageBox_Show(this, "The ODBC driver cannot handle some dates. Please cast to VARCHAR.", "ODBC datetime", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            }
                                            else if (ex.Message == "ERROR [08S02] Unexpected protocol character/message")
                                            {
                                                fetchSession.OnFetchFault(LegacyNetezzaFetchExceptionKind.GenericFault, ex.Message);
                                                MarkLegacyFetchFailed();
                                                command.Cancel();
                                                rdr.Close();
                                            }
                                            else if (ex.Message == "ERROR [HY000] ERROR:  Query was cancelled.")
                                            {
                                                fetchSession.OnFetchFault(LegacyNetezzaFetchExceptionKind.GenericFault, ex.Message);
                                                MarkLegacyFetchFailed();
                                                command.Cancel();
                                                rdr.Close();
                                            }
                                            else
                                            {
                                                fetchSession.OnFetchFault(ex);
                                                RecordLegacySqlFailure(ex.Message);
                                                MarkLegacyFetchFailed();
                                                try
                                                {
                                                    if (rdr is not null && !rdr.IsClosed)
                                                        rdr.Close();
                                                }
                                                catch (Exception)
                                                {
                                                }
                                                _loggerLoud.MessageBox_Show(this, ex.Message, "SQL execution error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        }

                                        if (extraInfo == "")
                                            extraInfo = $"Received {romNumber.ToString("N0")}  rows";
                                        if (romNumber >= _applicationSettingsContext.Config.ResultRowsLimit)
                                            extraInfo = $"Warning - rows limit({_applicationSettingsContext.Config.ResultRowsLimit.ToString("N0")}) was exceeded";
                                        if (romNumber >= _applicationSettingsContext.Config.ResultRowsLimit)
                                            _loggerLoud.MessageBox_Show(this, extraInfo, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    });

                                    bool weirdCancelEmptySchema = dtWynikiForGrid.Columns.Count == 1
                                        && dtWynikiForGrid.Columns[0].ColumnName == "\"\"";
                                    switch (fetchSession.DecideTabDisposition(
                                        executionSession.IsCancelling,
                                        weirdCancelEmptySchema,
                                        dataForGrid.Count))
                                    {
                                        case LegacyNetezzaResultTabDisposition.DiscardAttached:
                                            this.Invoke(() =>
                                            {
                                                var tp = myDataGridView.Parent as TabPage;
                                                var par = tp?.Parent as TabControl;
                                                if (tp is null || par is null)
                                                    return;
                                                ForgetLegacyResultCommand(tp);
                                                par.TabPages.Remove(tp);
                                            });
                                            break;

                                        case LegacyNetezzaResultTabDisposition.DisposeUnattached:
                                            this.Invoke(() =>
                                            {
                                                try { myDataGridView?.Dispose(); } catch { }
                                                try { tabAktualnaZakladkaX?.Dispose(); } catch { }
                                            });
                                            break;

                                        default:
                                            // Successful empty result sets still need a visible tab.
                                            InvokeOnMainWindow(() =>
                                            {
                                                EnsureResultTabAttached();
                                                if (fetchSession.ShouldRegisterGridOnSuccess())
                                                    RegisterLegacyResultGrid(tabAktualnaZakladkaX, myDataGridView);
                                            });
                                            InvokeOnMainWindow(() =>
                                            {
                                                sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, "Loading data finished");
                                                statusTextBox.Text = extraInfo;
                                                myDataGridView.InitGrid(false);
                                                ConfigureResultDataGrid(myDataGridView);

                                                //if (dtWynikiForGrid.Rows.Count == 0)
                                                if (dataForGrid.Count == 0)
                                                {
                                                    myDataGridView.IsEmpty = true;
                                                    //myDataGridView.dataGridView.AllowUserToAddRows = true;
                                                }
                                                myDataGridView.Invalidate();
                                            });
                                            break;
                                    }
                                }
                                else if (opcjaEksportu == ExportOptions.onlyLog)
                                {

                                }
                                else if (opcjaEksportu == ExportOptions.csv)
                                {
                                    DialogResult r = default;
                                    bool autoMode = false;
                                    if (filePath == null)
                                    {
                                        Thread t = new Thread((ThreadStart)(() =>
                                        {
                                            r = saveFileCSV.ShowDialog();
                                        }));

                                        // Run your code from a thread that joins the STA Thread
                                        t.SetApartmentState(ApartmentState.STA);
                                        t.Start();
                                        t.Join();
                                        filePath = saveFileCSV.FileName;
                                    }
                                    else
                                    {
                                        autoMode = true;
                                    }

                                    if (r == DialogResult.OK || autoMode)
                                    {
                                        if (csvQueryNum > 0 && filePath.Contains('.'))
                                        {
                                            int num = filePath.LastIndexOf('.');
                                            string part1 = filePath[0..num];
                                            filePath = part1 + csvQueryNum + filePath[num..];
                                        }
                                        else if (csvQueryNum > 0)
                                        {
                                            filePath += csvQueryNum;
                                        }

                                        long writedRows = await Task.Run(() => _importExportTasks.ExportCSVReader(CsvExportSettings.ResolveEncoding(_applicationSettingsContext.Config.EncondingName), rdr, filePath, _applicationSettingsContext.Config.SepInExportedCsv[0].ToString(), false, CsvExportSettings.ResolveNewLine(_applicationSettingsContext.Config.SepRowsInExportedCsv),
                                            (arg) =>
                                            {
                                                InvokeOnMainWindow(() =>
                                                {
                                                    sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"{arg.ToString("N0")} rows ");
                                                });
                                            }
                                            )
                                        );
                                        csvQueryNum++;

                                        InvokeOnMainWindow(() =>
                                        {
                                            //tekst.Text += $"\r\n{DateTime.Now} - results -> {filePath}";
                                            sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"results ({writedRows.ToString("N0")} rows) -> {filePath}");
                                        });
                                    }
                                }
                                else if (opcjaEksportu == ExportOptions.xlsx)
                                {
                                    DialogResult r = default;
                                    bool autoMode = false;
                                    if (filePath == null)
                                    {
                                        Thread t = new Thread((ThreadStart)(() =>
                                        {
                                            if (_applicationSettingsContext.Config.UseXlsb)
                                            {
                                                saveFileXlsx.Filter = saveFileXlsx.Filter.Replace("xlsx", "xlsb");
                                            }
                                            r = saveFileXlsx.ShowDialog();
                                        }));

                                        // Run your code from a thread that joins the STA Thread
                                        t.SetApartmentState(ApartmentState.STA);
                                        t.Start();
                                        t.Join();
                                        filePath = saveFileXlsx.FileName;
                                    }
                                    else
                                    {
                                        autoMode = true;
                                    }

                                    if (r == DialogResult.OK || autoMode)
                                    {
                                        int rowsWritted = 0;
                                        await Task.Run(() =>
                                        {
                                            ExcelWriter excelFile;
                                            if (_applicationSettingsContext.Config.UseXlsb)
                                            {
                                                excelFile = new XlsbWriter(filePath) { SuppressYear1000Dates = true };
                                            }
                                            else
                                            {
                                                excelFile = new XlsxWriter(filePath) { SuppressYear1000Dates = true };
                                            }
                                            try
                                            {
                                                int i = 1;
                                                do
                                                {
                                                    excelFile.AddSheet("Sheet" + "_" + i);
                                                    excelFile.WriteSheet(rdr, doAutofilter: true);
                                                    rowsWritted += excelFile.RowsCount;
                                                    excelFile.AddSheet($"SQL_{i}", hidden: true);
                                                    excelFile.WriteSheet(StringExtension.Sqlparts(queryClean));
                                                    i++;
                                                } while (rdr.NextResult());
                                            }
                                            finally
                                            {
                                                excelFile.Dispose();
                                            }
                                        });

                                        InvokeOnMainWindow(() =>
                                        {
                                            sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"results ({rowsWritted.ToString("N0")} rows)-> {filePath}");
                                        });
                                    }
                                }
                            } while (fetchSession.ShouldContinueNextResult(
                                         executionSession.IsCancelling,
                                         rdr is null || rdr.IsClosed)
                                && rdr.NextResult());
                        }
                        catch (Exception ex)
                        {
                            //this.Invoke(() => _loggerLoud.MessageBox_Show($"{ex.Message}"));
                            _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        if (fetchSession.ShouldCloseReaderAfterLoop() && rdr is not null && !rdr.IsClosed)
                        {
                            try
                            {
                                rdr.Close();
                            }
                            catch (Exception ex)
                            {
                                _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _loggerLoud.MessageBox_Show(this, "Something went wrong.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                InvokeOnMainWindow(() =>
                {
                    bool test;
                    if (fctbFromStart != null)
                    {
                        test = fctbFromStart.Focused;
                    }
                    else
                    {
                        test = false;
                    }
                    sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"Finished");
                    progressBarSQL.Value = (int)(100.0 * (++queryNum) / sqlsArray.Length);
                    lock (_sync)
                    {
                        if (test && CurrentTB != null && fctbFromStart == CurrentTB)
                        {
                            fctbFromStart.Focus();
                        }
                    }
                });

                goodSelectionLength += queryBase.Length;

            }

            btAbort.Enabled = false;
            lock (_sync)
            {
                FinalizeSqlRun(currentMainTab, fctbFromStart, currentResultsTab);
            }
            if (connectionForCost is not null && connectionForCost.State == ConnectionState.Open)
            {
                //connectionForCost.Close();
                await Task.Run(() => connectionForCost.Close());
            }

            if (!keepConnectionOpen && connection.State == ConnectionState.Open)
            {
                try
                {
                    await Task.Run(() => connection.Close());
                    //connection.Close();
                }
                catch (Exception ex)
                {
                    if (ex is not PlatformNotSupportedException && ex.Message != "Operation is not supported on this platform")
                    {
                        _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            InvokeOnMainWindow(() =>
            {
                try
                {
                    sqlLog.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds.ToString("F1"), connectionName, selectedDb, $"Connection closed");
                    currentResultsTab.IsRunning = false;

                    if (doBreak)
                    {
                        if (progressBarSQL is CustomProgressBar customProgress)
                        {
                            customProgress.SetState(2);
                        }
                        currentResultsTab.IsSuccess = false;
                    }

                    currentResultsTab.Parent?.Invalidate();

                }
                catch (Exception ex)
                {
                    _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            }
            finally
            {
                if (noticeNzConnection is not null && noticeHandler is not null)
                {
                    noticeNzConnection.NoticeReceived -= noticeHandler;
                }

                _sqlExecutionSessionRegistry.Complete(executionDocumentId);
            }
        }
}
