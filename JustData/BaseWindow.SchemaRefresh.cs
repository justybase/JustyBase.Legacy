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


        public void EnsureNetezzaLoadSchemaTreeViewPhaseInvoked(string selConnName)
        {
            if (InvokeRequired)
            {
                Invoke(() => NetezzaLoadSchemaTreeViewPhase(selConnName, addToExisting: true));
            }
            else
            {
                NetezzaLoadSchemaTreeViewPhase(selConnName, addToExisting: true);
            }
        }
        public async Task CbConnectionsSelectedIndexChanged(Action<bool> chageEnableStateOfNotAddedTab)
        {
            //SelectedConnectionName = cbConnections.SelectedItem as string;
            string selConnName = SelectedConnectionName;
            _completionRuntimeContext.SelectedConnectionName = SelectedConnectionName;
            _netezzaSqlCompletionServices.InvalidateSchema();
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
                    var nz = new Netezza(_databaseRuntimeContext, _loggerLoud, _importExportTasks, _generalDbService)
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
                        _completionRuntimeContext.DatabaseDictionary = _applicationSettingsContext.Config.CachedDatabaseDictionary;
                    }

                    // Skip legacy NetezzaLoadSchemaTreeViewPhase before download — no data yet.
                    // The callback inside DownloadSchemaNetezza provides progress, and the
                    // final MVVM InitializeAsync(selConnName) renders the tree correctly.
                    _completionContext.DatabaseDictionary?.Clear(); // to avoid keeping dummy data in memory

                    schemaDownloadSucceeded = await nz.DownloadSchemaNetezza(selConnName, NetezzaRefreshMode.partial, null, false,
                        () => EnsureNetezzaLoadSchemaTreeViewPhaseInvoked(selConnName));
             
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
                        await RefreshTableListInternalAsync(selConnName, false);
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
                    _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, selConnName);
                    DynamicCollectionForNettezaHelpers.ResetCache();
                }
            }
            else if (_generalDbService.LoginDataDic.ContainsKey(selConnName)
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
                        // Schema populated via MVVM ViewModel instead of legacy InitSchema.
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
            else if (!_generalDbService.LoginDataDic.ContainsKey(selConnName))
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
        private static void CopyTree(TreeView source, TreeView destination, bool addToExisting = false)
        {
            if (source.InvokeRequired)
            {
                source.Invoke(() =>
                {
                    doCopy(source, destination, addToExisting);
                });
            }
            else
            {
                doCopy(source, destination, addToExisting);
            }

            static void doCopy(TreeView cel, TreeView zrodlo, bool addToExisting)
            {
                cel.BeginUpdate();
                cel.ShowNodeToolTips = true;
                int index = -1;

                // Snapshot source nodes to guard against concurrent modification.
                TreeNode[] sourceNodes = zrodlo.Nodes.Cast<TreeNode>().ToArray();
                if (sourceNodes.Length == 0)
                {
                    cel.EndUpdate();
                    return;
                }

                if (!addToExisting)
                {
                    cel.Nodes.Clear();
                }
                else if (cel.Nodes.ContainsKey(sourceNodes[0].Name))
                {
                    index = cel.Nodes[sourceNodes[0].Name].Index;
                    cel.Nodes.RemoveAt(cel.Nodes.IndexOfKey(sourceNodes[0].Name));
                }

                if (addToExisting && index != -1 && sourceNodes.Length == 1 && cel.Nodes.Count >= index)
                {
                    cel.Nodes.Insert(index, (TreeNode)sourceNodes[0].Clone());
                }
                else
                {
                    foreach (TreeNode node in sourceNodes)
                    {
                        cel.Nodes.Add((TreeNode)node.Clone());
                    }
                }
                cel.EndUpdate();
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

        private void SwapTreeViewNodes(bool addToExisting, string connectionName, TreeView auxiliaryDatabaseTreeView, List<(TreeNode, string, List<string> names)> tvl)
        {
            if (CurrentUpper is not null && _completionContext.DatabaseDictionary.ContainsKey(connectionName))
            {
                CurrentUpper.ExtendDatabasesList(_completionContext.DatabaseDictionary[connectionName].Values.Select(arg => arg.DatabaseName).ToArray());
                _mvvmDatabaseExplorerControl.CbWhatDb.Items.Clear();
                _mvvmDatabaseExplorerControl.CbWhatDb.Items.Add("all");
                _mvvmDatabaseExplorerControl.CbWhatDb.Items.AddRange(_completionContext.DatabaseDictionary[connectionName].Values.ToArray().Select(arg => arg.DatabaseName).ToArray());
                _mvvmDatabaseExplorerControl.CbWhatDb.SelectedIndex = 0;
            }
            _mvvmDatabaseExplorerControl?.DatabaseTreeView?.BeginUpdate();

            var selNode = _mvvmDatabaseExplorerControl?.DatabaseTreeView?.SelectedNode;

            string selNodeName = "";
            string selNodeFullPath = "";
            if (selNode != null)
            {
                selNodeName = selNode.Name;
                selNodeFullPath = _mvvmDatabaseExplorerControl?.DatabaseTreeView?.SelectedNode?.FullPath ?? "";
            }
            BuildExpandedFullPath(_mvvmDatabaseExplorerControl?.DatabaseTreeView, tvl);
            CopyTree(_mvvmDatabaseExplorerControl?.DatabaseTreeView, auxiliaryDatabaseTreeView, addToExisting);

            _completionRuntimeContext.SchemaRefreshed = true;

            try
            {
                // ExpandLastKnownFull removed with old DatabaseExplorerControl
                TryExpandTreeNodes(_mvvmDatabaseExplorerControl?.DatabaseTreeView, tvl);
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Restoring the schema tree expansion failed: {exception.GetType().Name}");
            }

            if (selNode != null)
            {
                var nodesArray = _mvvmDatabaseExplorerControl?.DatabaseTreeView?.Nodes.Find(selNodeName, true);
                if (nodesArray is not null)
                {
                    for (int i = 0; i < nodesArray.Length; i++)
                    {
                        if (nodesArray[i].FullPath == selNodeFullPath)
                        {
                            _mvvmDatabaseExplorerControl.DatabaseTreeView.SelectedNode = nodesArray[i];
                            break;
                        }
                    }
                }
            }
            _completionRuntimeContext.SchemaRefreshed = false;
            _mvvmDatabaseExplorerControl?.DatabaseTreeView?.EndUpdate();
        }

        private void NetezzaLoadSchemaTreeViewPhaseInvoked(string connectionName, bool addToExisting = false, string swapOnlyDbName = null)
        {
            Invoke(() => NetezzaLoadSchemaTreeViewPhase(connectionName, addToExisting, swapOnlyDbName));
        }

        public void NetezzaLoadSchemaTreeViewPhase(string connectionName, bool addToExisting = false, string swapOnlyDbName = null)
        {
            connectionName = string.Intern(connectionName);
            if (_completionContext.SchemaRefreshed)
            {
                _completionRuntimeContext.SchemaRefreshed = false;
                if (_mvvmDatabaseExplorerControl is not null)
                    _mvvmDatabaseExplorerControl.DatabaseTreeView.Enabled = false;
                TreeView auxiliaryDatabaseTreeView = new TreeView();

                try
                {
                    statusTextBox.Text = $"schema loading";
                    try
                    {
                        if (_schemaTables.TablesByConnection.TryGetValue(connectionName, out var value2))
                        {
                            value2.Clear();
                        }
                        if (_completionContext.DatabaseSchemaLookup.TryGetValue(connectionName, out var value4))
                        {
                            value4.Clear();
                        }
                        if (_completionContext.DatabaseOwners.TryGetValue(connectionName, out var value5))
                        {
                            value5.Clear();
                        }

                        TreeNode root = auxiliaryDatabaseTreeView.Nodes.Add(connectionName, connectionName);

                        root.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.server, OBJECT_ID = 0 };
                        root.ToolTipText = _generalDbService.Server(connectionName);
                        root.ImageIndex = 25;
                        root.SelectedImageIndex = 25;

                        if (_completionContext.DatabaseDictionary.TryGetValue(connectionName, out var pairs))
                        {
                            foreach (var database in pairs)
                            {
                                //pod tree view
                                var dbNode = root.Nodes.Add(database.Value.DatabaseName);
                                dbNode.Name = database.Value.DatabaseName;
                                dbNode.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.dbase, OBJECT_ID = database.Key };
                                dbNode.ImageIndex = 0;
                                dbNode.SelectedImageIndex = 0;

                                var n1 = dbNode.Nodes.Add("Tables", "Tables");
                                n1.ContextMenuStrip = cmAllTables;
                                n1.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseTables, OBJECT_ID = database.Key };
                                n1.Nodes.Add("fool", "Loading…");
                                n1.ImageIndex = 1;
                                n1.SelectedImageIndex = 1;

                                var n7 = dbNode.Nodes.Add("External Tables", "External Tables");
                                n7.ContextMenuStrip = _emptyContextMenuStrip;
                                n7.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseExternals, OBJECT_ID = database.Key };
                                n7.Nodes.Add("fool", "Loading…");
                                n7.ImageIndex = 10;
                                n7.SelectedImageIndex = 10;

                                var n2 = dbNode.Nodes.Add("Views", "Views");
                                n2.ContextMenuStrip = cmAllViews;
                                n2.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseViews, OBJECT_ID = database.Key };
                                n2.Nodes.Add("fool", "Loading…");
                                n2.ImageIndex = 2;
                                n2.SelectedImageIndex = 2;

                                var n3 = dbNode.Nodes.Add("Procedures", "Procedures");
                                n3.ContextMenuStrip = cmAllProcsNetezza;
                                n3.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseProcedures, OBJECT_ID = database.Key };
                                n3.Nodes.Add("fool", "Loading…");
                                n3.ImageIndex = 5;
                                n3.SelectedImageIndex = 5;

                                var n4 = dbNode.Nodes.Add("Sequences", "Sequences");
                                n4.ContextMenuStrip = contextMenuStripNetezzaSequences;
                                n4.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseSequence, OBJECT_ID = database.Key };
                                n4.Nodes.Add("fool", "Loading…");
                                n4.ImageIndex = 7;
                                n4.SelectedImageIndex = 7;

                                var n5 = dbNode.Nodes.Add("Functions", "Functions");
                                n5.ContextMenuStrip = _emptyContextMenuStrip;
                                n5.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseFunctions, OBJECT_ID = database.Key };
                                n5.Nodes.Add("fool", "Loading…");
                                n5.ImageIndex = 15;
                                n5.SelectedImageIndex = 15;

                                var n6 = dbNode.Nodes.Add("Synonyms", "Synonyms");
                                n6.ContextMenuStrip = cmSynonyms;
                                n6.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseSynonyms, OBJECT_ID = database.Key };
                                n6.Nodes.Add("fool", "Loading…");
                                n6.ImageIndex = 17;
                                n6.SelectedImageIndex = 17;

                                var n8 = dbNode.Nodes.Add("Aggregate", "Aggregate");
                                n8.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseAggregates, OBJECT_ID = database.Key };
                                n8.Nodes.Add("fool", "Loading…");
                                n8.ImageIndex = 16;
                                n8.SelectedImageIndex = 16;

                                var n9 = dbNode.Nodes.Add("Fluid Query Data Sources", "Fluid Query Data Sources");
                                n9.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.baseFluides, OBJECT_ID = database.Key };
                                n9.ImageIndex = 35;
                                n9.SelectedImageIndex = 35;
                                n9.Nodes.Add("fool", "Loading…");
                            }
                        }
                        var treeNode = root.Nodes.Add("Server Info");
                        treeNode.Name = "Server Info";
                        treeNode.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfo, OBJECT_ID = -1 };
                        treeNode.ImageIndex = 21;
                        treeNode.SelectedImageIndex = 21;
                        treeNode.ContextMenuStrip = _emptyContextMenuStrip;

                        var nx = treeNode.Nodes.Add("Server");
                        nx.Name = "Server";
                        nx.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfoNext, OBJECT_ID = -2 };
                        nx.ImageIndex = treeNode.ImageIndex;
                        nx.SelectedImageIndex = treeNode.ImageIndex;
                        nx.ContextMenuStrip = _emptyContextMenuStrip;

                        var nn = nx.Nodes.Add(NetezzaSystemSql.ServerInformation, NetezzaHelpers.ServerVersion);
                        nn.ToolTipText = "Double click for more info";
                        nn.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfo, OBJECT_ID = -3 };
                        nn.ImageIndex = nx.ImageIndex;
                        nn.SelectedImageIndex = nx.SelectedImageIndex;
                        nn.ContextMenuStrip = _emptyContextMenuStrip;

                        nn = nx.Nodes.Add(NetezzaSystemSql.EnvironmentInformation, "Environment Variables");
                        nn.ToolTipText = "Double click for more info";
                        nn.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfo, OBJECT_ID = -4 };
                        nn.ImageIndex = nx.ImageIndex;
                        nn.SelectedImageIndex = nx.SelectedImageIndex;
                        nn.ContextMenuStrip = _emptyContextMenuStrip;

                        nn = nx.Nodes.Add(NetezzaSystemSql.HardwareInformation, "SPU Units");
                        nn.ToolTipText = "Double click for more info";
                        nn.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfo, OBJECT_ID = -5 };
                        nn.ImageIndex = nx.ImageIndex;
                        nn.SelectedImageIndex = nx.SelectedImageIndex;
                        nn.ContextMenuStrip = _emptyContextMenuStrip;

                        nx = treeNode.Nodes.Add("Security");
                        nx.Name = "Security";
                        nx.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfoNext, OBJECT_ID = -6 };
                        nx.ImageIndex = 22;
                        nx.SelectedImageIndex = 22;
                        nx.ContextMenuStrip = _emptyContextMenuStrip;

                        nn = nx.Nodes.Add(NetezzaSystemSql.Users, "_v_user");
                        nn.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfo, OBJECT_ID = -7 };
                        nn.ToolTipText = "Double click for more info";
                        nn.ImageIndex = nx.ImageIndex;
                        nn.SelectedImageIndex = nx.SelectedImageIndex;
                        nn.ContextMenuStrip = contextMenuStripNetezzaUsersOrGroups;

                        nn = nx.Nodes.Add(NetezzaSystemSql.GroupUsers, "_v_groupusers");
                        nn.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfo, OBJECT_ID = -8 };
                        nn.ToolTipText = "Double click for more info";
                        nn.ImageIndex = nx.ImageIndex;
                        nn.SelectedImageIndex = nx.SelectedImageIndex;
                        nn.ContextMenuStrip = contextMenuStripNetezzaUsersOrGroups;


                        nn = nx.Nodes.Add(NetezzaSystemSql.UserSecurity, "_v_user_security");
                        nn.Tag = new DatabaseTag() { KIND_ID = TypeInDatabase.serverInfo, OBJECT_ID = -9 };
                        nn.ToolTipText = "Double click for more info";
                        nn.ImageIndex = nx.ImageIndex;
                        nn.SelectedImageIndex = nx.SelectedImageIndex;
                        nn.ContextMenuStrip = _emptyContextMenuStrip;

                        List<(TreeNode, string, List<string>)> tvl = new List<(TreeNode, string, List<string>)>();
                        string userName = _applicationSession.CurrentLogin?.Profile.UserName ?? string.Empty;
                        bool flowControl = NetezzaHelpers.InitializeConnectionSchemaData(_databaseRuntimeContext, _connectionSessions, _schemaTables, userName, connectionName);
                        //if (!flowControl)
                        //{
                        //    return;
                        //}

                        if (!string.IsNullOrWhiteSpace(swapOnlyDbName))
                        {
                            // SwapTreeViewNodesOnDb removed with old DatabaseExplorerControl
                            _ = _mvvmDatabaseExplorerControl?.RefreshAsync();
                        }
                        else
                        {
                            SwapTreeViewNodes(addToExisting, connectionName, auxiliaryDatabaseTreeView, tvl);
                        }

                        _completionRuntimeContext.SchemaRefreshed = true;
                    }
                    catch (Exception e)
                    {
                        _loggerLoud.MessageBox_Show(this, e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        var action = () =>
                        {
                            if (InvokeRequired)
                            {
                                Invoke(() => Application.Restart());
                            }
                            else
                            {
                                Application.Restart();
                            }
                        };
                        NetezzaHelpers.OnSchemaProblemNetezzaAskForRestart(_databaseRuntimeContext, _loggerLoud, connectionName, action);
                    }

                }
                catch (Exception)
                {
                    NetezzaSchemaRefreshErrorInfo();
                }
                finally
                {
                    if (_mvvmDatabaseExplorerControl is not null)
                        _mvvmDatabaseExplorerControl.DatabaseTreeView.Enabled = true;
                    _completionRuntimeContext.SchemaRefreshed = true;
                    _netezzaSqlCompletionServices.InvalidateSchema();
                        _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, connectionName);
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("problem NetezzaLoadSchemaFromSqliteDb");
            }
        }
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
                treeView.SelectedNode.Level == 0 && _generalDbService.LoginDataDic.ContainsKey(treeView.SelectedNode.Name))
            {
                conName = treeView.SelectedNode.Name;
            }
            else if (treeView?.SelectedNode is not null &&
                treeView.SelectedNode.Level == 1 && _generalDbService.LoginDataDic.ContainsKey(treeView.SelectedNode.Parent.Name))
            {
                conName = treeView.SelectedNode.Parent.Name;
            }
            else if (treeView?.SelectedNode is not null &&
            treeView.SelectedNode.Level == 2 && _generalDbService.LoginDataDic.ContainsKey(treeView.SelectedNode.Parent.Parent.Name))
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
                // index was consumed by legacy InitSchema; MVVM manages its own tree

                if (_generalDbService.DriverName(conName) == "NetezzaSQL")
                {
                    await nzNodeRefresh(conName, refreshMode);
                }
                else
                {
                    try
                    {
                        if (!_connectionSessions.TryGetValue(conName, out var generalDb) || generalDb is null)
                        {
                            IGeneralDb gdb = _generalDbService.GetGeneralDb(_databaseRuntimeContext, _loggerLoud, _importExportTasks, conName, out string dbName);
                            gdb.Username = _generalDbService.UserName(conName);

                            CurrentUpper.ExtendDatabasesList(new string[] { _generalDbService.DBname(conName) });

                            SchemaRefreshOptionEnable(false);
                            //await refreshSecond();
                            statusTextBox.Text = $"{dbName} schema refreshing";

                            _connectionSessions.Set(conName, gdb);
                        }

                        await _schemaRefreshCoordinator.RefreshAsync(conName);
                        // Schema refreshed via MVVM ViewModel instead of legacy InitSchema
                        if (_mvvmDatabaseExplorerControl is not null)
                        {
                            await _mvvmDatabaseExplorerControl.RefreshAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggerLoud.MessageBox_Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
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

        private async Task nzNodeRefresh(string conName, NetezzaRefreshMode refreshMode = NetezzaRefreshMode.full)
        {
            InvokeOnMainWindow(() =>
            {
                statusTextBox.Text = $"Schema downloading";
            });

            List<string> dbsToRefresh = null;
            if (refreshMode == NetezzaRefreshMode.partialOnlyTables && _mvvmDatabaseExplorerControl?.DatabaseTreeView.Nodes.ContainsKey(conName) == true)
            {
                dbsToRefresh = new List<string>();
                foreach (TreeNode node in _mvvmDatabaseExplorerControl.DatabaseTreeView.Nodes[conName].Nodes)
                {
                    if (node.IsExpanded && node.Tag is DatabaseTag dlaBazy && dlaBazy.KIND_ID == TypeInDatabase.dbase)
                    {
                        dbsToRefresh.Add(node.Text);
                    }
                }
            }

            bool res = await (_connectionSessions[conName] as INetezza).DownloadSchemaNetezza(conName, refreshMode, dbsToRefresh);
            InvokeOnMainWindow(() =>
            {
                statusTextBox.Text = $"Schema downloaded";
            });
            if (!res)
            {
                NetezzaSchemaRefreshErrorInfo();
                return;
            }

            // After a full refresh, run the schema-data side effects that legacy
            // NetezzaLoadSchemaTreeViewPhase would have done. Clear dictionaries first
            // so InitializeConnectionSchemaData starts from a clean state.
            if (_schemaTables.TablesByConnection.TryGetValue(conName, out var nzValue))
                nzValue.Clear();
            if (_completionContext.DatabaseSchemaLookup.TryGetValue(conName, out var lookupValue))
                lookupValue.Clear();
            if (_completionContext.DatabaseOwners.TryGetValue(conName, out var ownersValue))
                ownersValue.Clear();

            string userName = _applicationSession.CurrentLogin?.Profile.UserName ?? string.Empty;
            NetezzaHelpers.InitializeConnectionSchemaData(_databaseRuntimeContext, _connectionSessions, _schemaTables, userName, conName);
            _netezzaSqlCompletionServices.InvalidateSchema();
            _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, conName);

            // The MVVM tree is rendered by InitializeAsync(selConnName) at the end of
            // CbConnectionsSelectedIndexChanged — no legacy tree building needed here.
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
                            if (_schemaTables.TablesByConnection.TryGetValue(connName, out var nzValue))
                                nzValue.Clear();
                            if (_completionContext.DatabaseSchemaLookup.TryGetValue(connName, out var lookupValue))
                                lookupValue.Clear();
                            if (_completionContext.DatabaseOwners.TryGetValue(connName, out var ownersValue))
                                ownersValue.Clear();

                            string userName = _applicationSession.CurrentLogin?.Profile.UserName ?? string.Empty;
                            NetezzaHelpers.InitializeConnectionSchemaData(_databaseRuntimeContext, _connectionSessions, _schemaTables, userName, connName);
                            _completionRuntimeContext.SchemaRefreshed = true;
                            _netezzaSqlCompletionServices.InvalidateSchema();
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

        private static void BuildExpandedFullPath(TreeView treeView, List<(TreeNode, string, List<string>)> expandedItems)
        {
            expandedItems.Clear();
            if (treeView is null) return;

            // Snapshot to prevent "Collection was modified" when CopyTree/SwapTreeViewNodes
            // re-enter the same TreeView during the same UI-thread operation.
            TreeNode[] roots = treeView.Nodes.Cast<TreeNode>().ToArray();
            foreach (TreeNode item in roots)
            {
                if (item.IsExpanded)
                {
                    expandedItems.Add((item, item.FullPath, new List<string>() { item.Name }));
                }
            }

            int i = 0;
            while (i < expandedItems.Count)
            {
                TreeNode node = expandedItems[i++].Item1;
                // Snapshot child nodes to avoid modification during enumeration
                TreeNode[] children = node.Nodes.Cast<TreeNode>().ToArray();
                foreach (TreeNode item in children)
                {
                    if (item.IsExpanded)
                    {
                        string fullPath = item.FullPath;
                        expandedItems.Add((item, fullPath, new List<string>()));
                    }
                }
            }
        }
        /// <summary>Simple helper to expand tree nodes by path — replaces old ExpandLastKnownFull.</summary>
        private static void TryExpandTreeNodes(TreeView? treeView, List<(TreeNode, string, List<string>)> expandedItems)
        {
            if (treeView is null) return;
            // Snapshot root nodes to prevent "Collection was modified" during re-entrant
            // BeforeExpand/OnChildrenAppended mutations of treeView.Nodes.
            TreeNode[] roots = treeView.Nodes.Cast<TreeNode>().ToArray();
            foreach (var (_, fullPath, _) in expandedItems)
            {
                foreach (TreeNode node in roots)
                {
                    if (node.FullPath == fullPath)
                    {
                        node.Expand();
                        break;
                    }
                }
            }
        }

        private ContextMenuStrip _emptyContextMenuStrip = new ContextMenuStrip();

        public async Task AddOneDbToNetezzaSchemaTree(string connectionName, IDatabaseDownloader dbObject, string dbName)
        {
            bool success = await dbObject.DownloadOneDb(connectionName, dbName);
            if (success)
            {
                // Clear dictionaries so InitializeConnectionSchemaData starts from a clean state
                if (_schemaTables.TablesByConnection.TryGetValue(connectionName, out var nzValue))
                    nzValue.Clear();
                if (_completionContext.DatabaseSchemaLookup.TryGetValue(connectionName, out var lookupValue))
                    lookupValue.Clear();
                if (_completionContext.DatabaseOwners.TryGetValue(connectionName, out var ownersValue))
                    ownersValue.Clear();

                string userName = _applicationSession.CurrentLogin?.Profile.UserName ?? string.Empty;
                NetezzaHelpers.InitializeConnectionSchemaData(_databaseRuntimeContext, _connectionSessions, _schemaTables, userName, connectionName);
                _completionRuntimeContext.SchemaRefreshed = true;
                _netezzaSqlCompletionServices.InvalidateSchema();
                _netezzaSqlCompletionServices.EnsureSchemaForConnection(_completionContext, connectionName);

                // MVVM tree re-renders from refreshed data — skip legacy auxiliary tree building
                if (_mvvmDatabaseExplorerControl is not null)
                    await _mvvmDatabaseExplorerControl.RefreshAsync();
            }
            else
            {
                NetezzaSchemaRefreshErrorInfo();
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
