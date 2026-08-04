// BaseWindow schema refresh and database explorer tree orchestration partial.
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
using AppBase.Services.Sql;
using JustyBaseLegacy.UI.Sql;
using DatabaseDataGridView.WinForms;
using DatabaseDataGridView.WinForms.Coloring;
using FastColoredTextBoxNS;
using FastColoredTextBoxNS.Helpers;
using JustDataAdditionalForms;
using JustyBase.NetezzaDriver;
using JustyBase.NetezzaCatalogSql;
using System.Drawing;
using JustyBase.NetezzaSqlParser.Linter;
using JustyBaseLegacy.UI.Helpers;
using JustyBaseLegacy.Services;
using JustyBaseLegacy.UI.Controls;
using JustyBaseLegacy.UI.DbForms;
using JustyBaseLegacy.UI.Models;
using SpreadSheetTasks;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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


namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        private void SetEnabledConnectionsAllDatabases(bool enabled)
        {
            foreach (TabPage tab in EditorTabPages)
            {
                (_tabManager.GetEditorPanel(tab) as SQLUpperPanel)?.SetEnabledConnectionsDatabases(enabled);
            }
        }

        private void ResetAutocompleteCachesForAllEditors()
        {
            foreach (TabPage tab in EditorTabPages)
                (_tabManager.GetEditorPanel(tab) as SQLUpperPanel)?.ResetAutocompleteCache();
        }

        private bool IsEnabledMode = true;

        public void SchemaRefreshOptionEnable(bool enabled)
        {
            IsEnabledMode = enabled;
            SetEnabledConnectionsAllDatabases(enabled);
            tcmChangeSorting.Enabled = enabled;
            addNewConnectionToolStripMenuItem.Enabled = enabled;
                refreshTableListItem.Enabled = enabled;
            refreshTableListToolStripMenuItem.Enabled = enabled;
            if (_mvvmDatabaseExplorerControl is not null)
                _mvvmDatabaseExplorerControl.SetControlsEnabled(enabled);
            importFromClipboard.Enabled = enabled;
            // Loading visuals are handled by the MVVM control itself
        }

        public ComboBox CbSearchDb { get => _mvvmDatabaseExplorerControl?.CbWhatDb; }
        public ComboBox CbWhatDb { get => _mvvmDatabaseExplorerControl?.CbWhatDb; }
        public TextBox TbFastSchemaSearch { get => _mvvmDatabaseExplorerControl?.TbFastSchemaSearch; }
        public DataGridView DgvFastDbBrowser { get => _mvvmDatabaseExplorerControl?.DgvFastDbBrowser; }

        public async Task CbConnectionsSelectedIndexChanged(Action<bool> chageEnableStateOfNotAddedTab)
        {
            //SelectedConnectionName = cbConnections.SelectedItem as string;
            string selConnName = SelectedConnectionName;
            _completionRuntimeContext.SelectedConnectionName = SelectedConnectionName;
            _netezzaSqlCompletionServices.InvalidateSchema();
            ResetAutocompleteCachesForAllEditors();
            FastColoredTextBox fctb = CurrentTB;

            // Track whether the non-Netezza branch already initialized MVVM tree
            // so the shared final InitializeAsync(selConnName) can be skipped.
            bool nonNetezzaMvvmInitialized = false;

            if (_generalDbService.DriverName(selConnName) == "NetezzaSQL")
            {
                bool schemaDownloadSucceeded = true;
                // Check session registry instead of tree — MVVM InitializeAsync may have
                // created the node already, but the connection isn't initialized yet.
                if (!_connectionSessions.ContainsKey(selConnName))
                {
                    SelectedDatabase = _generalDbService.DBname(selConnName);

                    SchemaRefreshOptionEnable(false);
                    chageEnableStateOfNotAddedTab(false);
                    var nz = new Netezza(
                        _databaseRuntimeContext,
                        _loggerLoud,
                        _importExportTasks,
                        _generalDbService,
                        _netezzaHelperService)
                    {
                        ConnectionString = _generalDbService.ConnectionStringForNz(_applicationSettingsContext.Config.ConnectionTimeout, selConnName),
                        ConnectionName = selConnName,
                        Username = _generalDbService.UserName(selConnName),
                        LogErrorStdColor = MyColors.LogErrorStdColor
                    };
                    _connectionSessions.Set(selConnName, nz);
                    nz.InitDb();

                    statusTextBox.Text = $"Schema downloading";



                    if (_completionContext.DatabaseDictionary?.Count == 0 && _applicationSettingsContext.Config.CachedDatabaseDictionary is not null)
                    {
                        _completionRuntimeContext.ReplaceDatabaseDictionary(_applicationSettingsContext.Config.CachedDatabaseDictionary);
                    }

                    _completionRuntimeContext.ClearDatabaseDictionary(); // to avoid keeping dummy data in memory

                    try
                    {
                        await _schemaRefreshCoordinator.RefreshAsync(
                            selConnName,
                            new JustData.Application.Schema.SchemaRefreshRequest(
                                JustData.Application.Schema.SchemaRefreshMode.Partial));
                        schemaDownloadSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        schemaDownloadSucceeded = false;
                        _loggerLoud.LogError("Error while downloading Netezza schema for {ConnectionName}", ex);
                    }
             
                    if (!schemaDownloadSucceeded)
                    {
                        NetezzaSchemaRefreshErrorInfo();
                        // Drop the session so a later edit/save can recreate it with a fresh ConnectionString
                        // (same pattern as the non-Netezza InitDb failure path below).
                        _connectionSessions.Remove(selConnName);
                    }

                    if (schemaDownloadSucceeded)
                    {
                        _mvvmDatabaseExplorerControl.CbWhatDb.Items.Clear();
                        _mvvmDatabaseExplorerControl.CbWhatDb.Items.Add("all");
                        if (_completionContext.DatabaseDictionary.TryGetValue(selConnName, out var dbDict))
                        {
                            _mvvmDatabaseExplorerControl.CbWhatDb.Items.AddRange(dbDict.Values.ToArray().Select(arg => arg.DatabaseName).ToArray());
                        }
                        _mvvmDatabaseExplorerControl.CbWhatDb.SelectedIndex = 0;
                    }
                    SchemaRefreshOptionEnable(true);
                    chageEnableStateOfNotAddedTab(true);

                    if (schemaDownloadSucceeded)
                    {
                        // Partial download seeds the catalog; full refresh populates every
                        // database (legacy RefreshTableListInternalAsync after first connect).
                        await RefreshTableListInternalAsync(selConnName, disableInUi: false);
                        statusTextBox.Text = $"Schema downloaded";
                    }
                   
                }

                if (schemaDownloadSucceeded && _completionContext.DatabaseDictionary.TryGetValue(selConnName, out var dbDict2))
                {
                    _mvvmDatabaseExplorerControl.CbWhatDb.Items.Clear();
                    _mvvmDatabaseExplorerControl.CbWhatDb.Items.Add("all");
                    _mvvmDatabaseExplorerControl.CbWhatDb.Items.AddRange(dbDict2.Values.ToArray().Select(arg => arg.DatabaseName).ToArray());
                    _mvvmDatabaseExplorerControl.CbWhatDb.SelectedIndex = 0;
                }

                if (schemaDownloadSucceeded && _completionContext.DatabaseSchemaLookup is not null && _completionContext.DatabaseSchemaLookup.TryGetValue(selConnName, out var value))
                {
                    CurrentUpper?.ExtendDatabasesList(value.Keys);
                    _netezzaSqlCompletionServices.InvalidateSchema();
                    ResetAutocompleteCachesForAllEditors();
                    _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, selConnName);
                }
            }
            else if (_connectionProfileCatalog.TryGetProfile(selConnName, out _)
                && (_generalDbService.DriverName(selConnName) == "DB2"
                    || _generalDbService.DBname(selConnName).EndsWith("accdb", StringComparison.OrdinalIgnoreCase)
                    || _generalDbService.DriverName(selConnName) == "Oracle"
                    || _generalDbService.DriverName(selConnName) == "MsSqlStd"
                    || _generalDbService.DriverName(selConnName) == "MsSqlTrusted"
                    || _generalDbService.DriverName(selConnName) == "Postgres"
                    || _generalDbService.DriverName(selConnName) == "SQLite"
                    || _generalDbService.DriverName(selConnName) == "MySql"))
            {
                if (!_connectionSessions.ContainsKey(selConnName))
                {
                    statusTextBox.Text = $"{selConnName} schema refreshing";
                    SchemaRefreshOptionEnable(false);
                    chageEnableStateOfNotAddedTab(false);

                    this._mvvmDatabaseExplorerControl.CbWhatDb.Items.Clear();
                    this._mvvmDatabaseExplorerControl.CbWhatDb.Items.Add("all");
                    this._mvvmDatabaseExplorerControl.CbWhatDb.SelectedIndex = 0;

                    IGeneralDb gdb = _generalDbService.GetGeneralDb(_databaseRuntimeContext, _loggerLoud, _importExportTasks, selConnName, out string dbName);
                    gdb.Username = _generalDbService.UserName(selConnName);

                    CurrentUpper?.ExtendDatabasesList(new string[] { _generalDbService.DBname(selConnName) });
                    await Task.Delay(10);
                    SchemaRefreshOptionEnable(false);
                    chageEnableStateOfNotAddedTab(false);
                    _connectionSessions.Set(selConnName, gdb);
                    try
                    {
                        await Task.Run(() => gdb.InitDb());
                        // Schema is populated through the MVVM repository.
                        // Use selConnName overload instead of parameterless to avoid
                        // rendering ALL connections before the final fixup call.
                        if (_mvvmDatabaseExplorerControl is not null)
                        {
                            await _mvvmDatabaseExplorerControl.InitializeAsync(selConnName);
                            nonNetezzaMvvmInitialized = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggerLoud.MessageBox_Show(this, ex.Message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _connectionSessions.Remove(selConnName);

                        foreach (TabPage tab in EditorTabPages)
                        {
                            (_tabManager.GetEditorPanel(tab) as SQLUpperPanel)?.RemoveConnection(selConnName);
                        }
                        SchemaRefreshOptionEnable(true);
                        return;
                    }

                    statusTextBox.Text = $"{dbName} schema refreshed";
                    SchemaRefreshOptionEnable(true);
                    chageEnableStateOfNotAddedTab(true);
                }
                else if (_connectionSessions.TryGetValue(selConnName, out var gdbReset))
                {
                    gdbReset.ResetDynamicCollection();
                }

                if (SelectedConnectionName == selConnName
                    && _connectionSessions.TryGetValue(selConnName, out var gdbExtend))
                {
                    if (gdbExtend.DatabaseList is not null)
                    {
                        CurrentUpper.ExtendDatabasesList(gdbExtend.DatabaseList.ToArray());
                    }
                    else
                    {
                        CurrentUpper.ExtendDatabasesList(new string[] { gdbExtend.DefaultDatabaseName });
                    }
                }

            }
            else if (!_connectionProfileCatalog.TryGetProfile(selConnName, out _))
            {
                _loggerLoud.MessageBox_Show(this, $"{selConnName} was not found.", "Connection not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                _loggerLoud.MessageBox_Show(this, "This feature is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Load MVVM tree roots from existing repository state (no re-download).
            // Skip if the non-Netezza branch already initialized the MVVM tree above.
            if (!nonNetezzaMvvmInitialized && _mvvmDatabaseExplorerControl is not null && !string.IsNullOrWhiteSpace(selConnName))
            {
                await _mvvmDatabaseExplorerControl.InitializeAsync(selConnName);
            }

            if (fctb is not null)
            {
                MiscellaneousHelper.UpdateAdditionStyles(fctb.Range, _colorTheme.CurrentFctbColors, _applicationSettingsContext.Config.BracketFolding);
                GetTextCommentRanges(fctb);

                fctb.Name = _generalDbService.DriverName(selConnName) + "_addedFastColored";
                if (fctb.FindAncestorTabPage() is TabPagePicture pagePicture)
                {
                    pagePicture.DatabaseTypeName = _generalDbService.DriverName(selConnName);
                }
            }
        }
        private void CollapseDatabaseMenuItem_Click(object sender, EventArgs e)
        {
            _mvvmDatabaseExplorerControl?.DatabaseTreeView?.CollapseAll();
            _mvvmDatabaseExplorerControl?.CollapseAllNodes();
        }


        private void NetezzaSchemaRefreshErrorInfo()
        {
            InvokeOnMainWindow(() =>
            {
                if (IsDisposed || Disposing)
                    return;

                _loggerLoud.MessageBox_Show(this, "Problem refreshing the schema.", "Schema refresh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                statusTextBox.Text = $"Problem with db connection";
            });
        }

        private ContextMenuStrip _emptyContextMenuStrip = new ContextMenuStrip();

        private bool _refreshTableListInProgress;

        public bool RefreshTableListInProgress => _refreshTableListInProgress;

        public async Task RefreshTableListInternalAsync(string conName, bool disableInUi = true)
        {
            if (_refreshTableListInProgress)
            {
                return;
            }

            _refreshTableListInProgress = true;
            try
            {
                if (disableInUi)
                {
                    SchemaRefreshOptionEnable(false);
                }

                if (string.IsNullOrEmpty(conName))
                {
                    _loggerLoud.MessageBox_Show(this, "Select the root node to refresh.", "Refresh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_connectionSessions.TryGetValue(conName, out var db) && db is INetezza nz)
                {
                    nz.ResetLists();
                    await RefreshSchemaFullOrNot(conName, NetezzaRefreshMode.full, disableInUi);
                    await nz.LoadSourceTextCache();
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError("Error while refreshing the schema for {ConnectionName}", ex);
            }
            finally
            {
                _refreshTableListInProgress = false;
                if (disableInUi)
                {
                    SchemaRefreshOptionEnable(true);
                }
            }
        }

        /// <summary>
        /// Refreshes the table metadata for every currently displayed Netezza connection.
        /// The UI control delegates the workflow here so concurrency and state restoration
        /// remain owned by the window rather than by a view event handler.
        /// </summary>
        public async Task RefreshAllNetezzaTablesAsync()
        {
            if (_refreshTableListInProgress)
            {
                return;
            }

            _refreshTableListInProgress = true;
            try
            {
                SchemaRefreshOptionEnable(false);

                string[] connectionNames = _mvvmDatabaseExplorerControl?.DatabaseTreeView?.Nodes
                    .Cast<TreeNode>()
                    .Select(node => node.Text)
                    .ToArray() ?? Array.Empty<string>();

                foreach (string connectionName in connectionNames)
                {
                    if (_generalDbService.DriverName(connectionName) == "NetezzaSQL")
                    {
                        await RefreshSchemaFullOrNot(connectionName, NetezzaRefreshMode.partialOnlyTables, disableInUi: false);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError("Error while refreshing Netezza table metadata", ex);
            }
            finally
            {
                _refreshTableListInProgress = false;
                SchemaRefreshOptionEnable(true);
            }
        }

        public async void RefreshTableList(object sender, EventArgs e)
        {
            try
            {
                string conName = GetSelectedConnectionName();
                await RefreshTableListInternalAsync(conName);
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError(ex.Message, ex);
            }
        }

        private string GetSelectedConnectionName()
        {
            string conName = null;
            var treeView = _mvvmDatabaseExplorerControl?.DatabaseTreeView;
            if (treeView?.SelectedNode is not null &&
                treeView.SelectedNode.Level == 0 && _connectionProfileCatalog.TryGetProfile(treeView.SelectedNode.Name, out _))
            {
                conName = treeView.SelectedNode.Name;
            }
            else if (treeView?.SelectedNode is not null &&
                treeView.SelectedNode.Level == 1 && _connectionProfileCatalog.TryGetProfile(treeView.SelectedNode.Parent.Name, out _))
            {
                conName = treeView.SelectedNode.Parent.Name;
            }
            else if (treeView?.SelectedNode is not null &&
            treeView.SelectedNode.Level == 2 && _connectionProfileCatalog.TryGetProfile(treeView.SelectedNode.Parent.Parent.Name, out _))
            {
                conName = treeView.SelectedNode.Parent.Parent.Name;
            }
            else if (treeView?.Nodes.Count == 0)
            {
                conName = SelectedConnectionName;
            }

            return conName;
        }

        public void ExtendDatabasesList(IEnumerable<string> databasesList)
        {
            CurrentUpper.ExtendDatabasesList(databasesList);
        }

        public async Task RefreshSchemaFullOrNot(string conName, NetezzaRefreshMode refreshMode, bool disableInUi)
        {
            if (disableInUi)
            {
                SchemaRefreshOptionEnable(false);
            }
            try
            {
                if (_generalDbService.DriverName(conName) != "NetezzaSQL"
                    && (!_connectionSessions.TryGetValue(conName, out var generalDb) || generalDb is null))
                {
                    IGeneralDb gdb = _generalDbService.GetGeneralDb(_databaseRuntimeContext, _loggerLoud, _importExportTasks, conName, out string dbName);
                    gdb.Username = _generalDbService.UserName(conName);
                    CurrentUpper.ExtendDatabasesList(new string[] { _generalDbService.DBname(conName) });
                    statusTextBox.Text = $"{dbName} schema refreshing";
                    _connectionSessions.Set(conName, gdb);
                }

                JustData.Application.Schema.SchemaRefreshMode mode = refreshMode switch
                {
                    NetezzaRefreshMode.full => JustData.Application.Schema.SchemaRefreshMode.Full,
                    NetezzaRefreshMode.partialOnlyTables => JustData.Application.Schema.SchemaRefreshMode.PartialOnlyTables,
                    _ => JustData.Application.Schema.SchemaRefreshMode.Partial
                };

                List<string> dbsToRefresh = null;
                if (refreshMode == NetezzaRefreshMode.partialOnlyTables
                    && _mvvmDatabaseExplorerControl?.DatabaseTreeView?.Nodes.ContainsKey(conName) == true)
                {
                    dbsToRefresh = new List<string>();
                    foreach (TreeNode node in _mvvmDatabaseExplorerControl.DatabaseTreeView.Nodes[conName].Nodes)
                    {
                        if (node.IsExpanded && node.Tag is DatabaseTag dlaBazy && dlaBazy.KIND_ID == TypeInDatabase.dbase)
                            dbsToRefresh.Add(node.Text);
                    }
                }

                InvokeOnMainWindow(() => statusTextBox.Text = "Schema downloading");
                try
                {
                    await _schemaRefreshCoordinator.RefreshAsync(
                        conName,
                        new JustData.Application.Schema.SchemaRefreshRequest(mode, dbsToRefresh));
                    InvokeOnMainWindow(() => statusTextBox.Text = "Schema downloaded");

                    _netezzaSqlCompletionServices.InvalidateSchema();
                    ResetAutocompleteCachesForAllEditors();
                    _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, conName);

                    if (_mvvmDatabaseExplorerControl is not null)
                        await _mvvmDatabaseExplorerControl.InitializeAsync(conName);
                }
                catch (Exception ex)
                {
                    if (_generalDbService.DriverName(conName) == "NetezzaSQL")
                        NetezzaSchemaRefreshErrorInfo();
                    else
                        _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (disableInUi)
                {
                    SchemaRefreshOptionEnable(true);
                }
            }
        }

        private async void TcmChangeSorting_Click(object sender, EventArgs e)
        {
            SchemaRefreshOptionEnable(false);
            try
            {
                _applicationSettingsContext.Config.SortMethod = (_applicationSettingsContext.Config.SortMethod + 1) % 3;
                statusTextBox.Text = $"schema refreshing";
                // Snapshot to prevent "Collection was modified" during re-entrant tree operations
                TreeNode[] roots = _mvvmDatabaseExplorerControl?.DatabaseTreeView.Nodes
                    .Cast<TreeNode>().ToArray() ?? [];
                foreach (TreeNode item in roots)
                {
                    try
                    {
                        if (_generalDbService.DriverName(item.Text) == "NetezzaSQL")
                        {
                            string connName = item.Text;

                            // Clear dictionaries so InitializeConnectionSchemaData re-sorts from clean state
                            _schemaTables.ClearConnection(connName);
                            _completionRuntimeContext.ClearSchemaLookup(connName);
                            _completionRuntimeContext.ClearDatabaseOwners(connName);

                            string userName = _applicationSession.CurrentLogin?.Profile.UserName ?? string.Empty;
                            NetezzaHelpers.InitializeConnectionSchemaData(_databaseRuntimeContext, _connectionSessions, _schemaTables, userName, connName);
                            await _schemaRefreshCoordinator.NotifyRefreshedAsync(connName);
                            _completionRuntimeContext.SchemaRefreshed = true;
                            _netezzaSqlCompletionServices.InvalidateSchema();
                            ResetAutocompleteCachesForAllEditors();
                            _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, connName);

                            // Re-render MVVM tree with the new sort order — skip legacy auxiliary tree
                            if (_mvvmDatabaseExplorerControl is not null)
                                await _mvvmDatabaseExplorerControl.InitializeAsync(connName);
                        }
                        else
                        {
                            _loggerLoud.MessageBox_Show(this, $"This feature is not implemented yet: {item.Text}", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggerLoud.LogError("Error during schema refresh for {ConnectionName}", ex);
                    }
                }
                statusTextBox.Text = $"schema refreshed";
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError("Error while changing schema sort order", ex);
            }
            finally
            {
                SchemaRefreshOptionEnable(true);
            }
        }

        public async Task AddOneDbToNetezzaSchemaTree(string connectionName, IDatabaseDownloader dbObject, string dbName)
        {
            try
            {
                await _schemaRefreshCoordinator.AttachDatabaseAsync(connectionName, dbName);
                _completionRuntimeContext.SchemaRefreshed = true;
                _netezzaSqlCompletionServices.InvalidateSchema();
                ResetAutocompleteCachesForAllEditors();
                _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, connectionName);
                if (_mvvmDatabaseExplorerControl is not null)
                    await _mvvmDatabaseExplorerControl.InitializeAsync(connectionName);
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError("Error while attaching Netezza database {Database} on {ConnectionName}", ex);
                NetezzaSchemaRefreshErrorInfo();
            }
        }

        /// <summary>Called after autocomplete lazily downloads one database catalog.</summary>
        public async Task OnNetezzaOneDatabaseAttachedAsync(string connectionName, string databaseName)
        {
            try
            {
                // Download already completed in AutocompleteClass; publish + re-init caches/UI.
                _schemaTables.ClearConnection(connectionName);
                _completionRuntimeContext.ClearSchemaLookup(connectionName);
                _completionRuntimeContext.ClearDatabaseOwners(connectionName);
                string userName = _applicationSession.CurrentLogin?.Profile.UserName ?? string.Empty;
                NetezzaHelpers.InitializeConnectionSchemaData(_databaseRuntimeContext, _connectionSessions, _schemaTables, userName, connectionName);
                await _schemaRefreshCoordinator.NotifyRefreshedAsync(connectionName);
                _completionRuntimeContext.SchemaRefreshed = true;
                _netezzaSqlCompletionServices.InvalidateSchema();
                ResetAutocompleteCachesForAllEditors();
                _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, connectionName);
                if (_mvvmDatabaseExplorerControl is not null)
                    await _mvvmDatabaseExplorerControl.InitializeAsync(connectionName);
            }
            catch (Exception ex)
            {
                _loggerLoud.LogError("Error while applying attached Netezza database {Database}", ex);
            }
        }

        readonly private Dictionary<int, string> _cacheSynonymSequenceRam = new Dictionary<int, string>();

        public string GetAddInfo(int idObj, TypeInDatabase typeId, string connectionName)
        {
            if (_cacheSynonymSequenceRam.TryGetValue(idObj, out string value))
            {
                return value;
            }

            if (!_schemaTables.TablesByConnection.TryGetValue(connectionName, out var baseTables)
                || !baseTables.TryGetValue(idObj, out var tableData))
            {
                return string.Empty;
            }

            if (!_completionContext.DatabaseDictionary.TryGetValue(connectionName, out var databases)
                || !databases.TryGetValue(tableData.DATABASE_ID, out var dbInfo))
            {
                return string.Empty;
            }

            string dbName = dbInfo.DatabaseName;
            if (!_connectionSessions.TryGetValue(connectionName, out var generalDb))
            {
                return string.Empty;
            }

            using DbConnection connection = generalDb.GetConnection(dbName);
            connection.Open();

            string sql = "";
            if (typeId == TypeInDatabase.function)
            {
                sql = NetezzaSystemSql.GetFunctionInfo(tableData.TABLE_NAME);
            }
            else if (typeId == TypeInDatabase.thisAggregate)
            {
                sql = NetezzaSystemSql.GetAggregateInfo(tableData.TABLE_NAME);
            }
            else if (typeId == TypeInDatabase.sequence)
            {
                sql = NetezzaSystemSql.SequenceInfo;
            }
            else if (typeId == TypeInDatabase.synonym)
            {
                sql = NetezzaSystemSql.SynonymInfo;
            }

            DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            using var rd = command.ExecuteReader();
            while (rd.Read())
            {
                int id = rd.GetInt32(0);
                var txt = rd.GetValue(1)?.ToString();
                _cacheSynonymSequenceRam[id] = txt;
            }
            connection.Close();

            return _cacheSynonymSequenceRam[idObj];
        }
    }
}
