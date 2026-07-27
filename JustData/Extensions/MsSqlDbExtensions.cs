#if INCLUDE_MSSQL
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Extensions
{
    public static class MsSqlDbExtensions
    {
        private static bool IsGoodName(string name)
        {
            return !string.IsNullOrEmpty(name) && !name.Contains(' ') && !name.Contains('-') && !name.Contains('.') && !name.Contains('[') && !name.Contains(']');
        }
        /// <summary>
        /// Initializes the schema tree view for MS SQL Server database
        /// </summary>
        public static void InitMsSqlSchema(this MsSqlDb database, TreeView treeView, ContextMenuStrip cmStripTabeli, ContextMenuStrip cmStripViewGeneral, ContextMenuStrip cmStripAliasesGeneral, ContextMenuStrip cmStripProcs, ContextMenuStrip cmStripSynonymsGeneral, string connName, ContextMenuStrip allTablesGeneral, ContextMenuStrip cmColumns, ContextMenuStrip cmConstraints, ContextMenuStrip cmIndexes, ContextMenuStrip cmPartitions, ContextMenuStrip cmTriggers, ContextMenuStrip cmsDB2Server, ContextMenuStrip cmsSynonyms, int index = -1)
        {
            treeView.Invoke(()=>
            {
                database.AutocompleteSuggestions.OneWord.Clear();
                database.AutocompleteSuggestions.TwoWords.Clear();
                database.AutocompleteSuggestions.TreeWords.Clear();


                int a1 = treeView.Nodes.IndexOfKey(connName);
                if (a1 != -1)
                {
                    treeView.Nodes.RemoveAt(a1);
                }

                //TreeNodeCollection tn = treeViewExtaDB.Nodes;
                var root = treeView.Nodes.Add(connName, connName);
                root.ImageIndex = 31;
                root.SelectedImageIndex = 31;

                var databases = root.Nodes.Add("Databases", "Databases");
                databases.ContextMenuStrip = null;
                databases.ImageIndex = 21;
                databases.SelectedImageIndex = 21;

                databases.Nodes.Clear();
                foreach (DataRow currnetDb in database._dtDatabases.Rows)
                {
                    string CurrenDatabaseName = currnetDb[0].ToString();
                    var currentDb = databases.Nodes.Add(CurrenDatabaseName, CurrenDatabaseName);
                    currentDb.ImageIndex = 0;
                    currentDb.SelectedImageIndex = 0;
                    database.AutocompleteSuggestions.OneWord.Add(CurrenDatabaseName);


                    TreeNodeCollection tn = currentDb.Nodes;
                    var tnList = new List<(TreeNodeCollection, string)>();

                    foreach (DataRow user in database._schemas.Select($"CATALOG_NAME = '{CurrenDatabaseName}'"))
                    {
                        TreeNode n1 = currentDb.Nodes.Add((user.ItemArray[1] as string), (user.ItemArray[1] as string));
                        n1.ImageIndex = 1;
                        n1.SelectedImageIndex = 1;
                        tnList.Add((n1.Nodes, n1.Text));
                        database.AutocompleteSuggestions.TwoWords.Add($"{user.ItemArray[0]}.{user.ItemArray[1]}");
                    }

                    foreach (var itx in tnList)
                    {
                        var currentShemaNode = itx.Item1;
                        var user = itx.Item2;

                        var nodeTables = currentShemaNode.Add("Tables", "Tables");
                        nodeTables.ImageIndex = 1;
                        nodeTables.SelectedImageIndex = 1;
                        nodeTables.Nodes.Add("fool");

                        var nodeViews = currentShemaNode.Add("Views", "Views");
                        nodeViews.ImageIndex = 2;
                        nodeViews.SelectedImageIndex = 2;
                        nodeViews.Nodes.Add("fool");

                        var nodePorcedures = currentShemaNode.Add("Procedures", "Procedures");
                        nodePorcedures.ImageIndex = 5;
                        nodePorcedures.SelectedImageIndex = 5;
                        nodePorcedures.Nodes.Add("fool");

                        nodeTables.Nodes.Clear();

                        DataRow[] tableCol;

                        tableCol = database.tables?.Select($"TABLE_SCHEMA = '{user}' AND TABLE_CATALOG = '{CurrenDatabaseName}'");

                        database.objectInSchema[CurrenDatabaseName + "_" + user] = new Dictionary<string, TypeInDatabase>(StringComparer.OrdinalIgnoreCase);

                        foreach (DataRow item in tableCol)//owner, name,type
                        {
                            string tabName = item.ItemArray[2] as string;
                            if (!tabName.IsGoodName())
                            {
                                tabName = $"\"{tabName}\"";
                            }

                            TreeNode n1;
                            n1 = nodeTables.Nodes.Add($"{CurrenDatabaseName}.{user}.{tabName}", tabName);

                            n1.ImageIndex = 8;
                            n1.SelectedImageIndex = 8;
                            n1.ContextMenuStrip = cmStripTabeli;
                            n1.Nodes.Add("fool");
                            database.AutocompleteSuggestions.TreeWords.Add($"{CurrenDatabaseName}.{user}.{tabName}");
                            if (!database.objectInSchema[CurrenDatabaseName + "_" + user].ContainsKey(tabName))
                            {
                                database.objectInSchema[CurrenDatabaseName + "_" + user].Add(tabName, TypeInDatabase.table);
                            }
                        }

                        nodeViews.Nodes.Clear();
                        DataRow[] viewCol;
                        viewCol = database.views?.Select($"TABLE_SCHEMA = '{user}' AND TABLE_CATALOG = '{CurrenDatabaseName}'");

                        foreach (DataRow item in viewCol)
                        {
                            TreeNode n1;
                            n1 = nodeViews.Nodes.Add($"{CurrenDatabaseName}.{user}.{item.ItemArray[2]}", item.ItemArray[2] as string);
                            n1.ImageIndex = 9;
                            n1.SelectedImageIndex = 9;
                            n1.ContextMenuStrip = cmStripViewGeneral;
                            n1.Nodes.Add("fool");
                            database.AutocompleteSuggestions.TreeWords.Add($"{CurrenDatabaseName}.{user}.{item.ItemArray[2]}");
                            if (!database.objectInSchema[CurrenDatabaseName + "_" + user].ContainsKey(item.ItemArray[2] as string))
                            {
                                database.objectInSchema[CurrenDatabaseName + "_" + user].Add(item.ItemArray[2] as string, TypeInDatabase.view);
                            }
                        }

                        var procs = database.procedures;
                        nodePorcedures.Nodes.Clear();

                        DataRow[] procCol;

                        procCol = procs?.Select($"SPECIFIC_SCHEMA = '{user}' AND SPECIFIC_CATALOG = '{CurrenDatabaseName}'");

                        foreach (DataRow item in procCol)
                        {
                            string runtimeName = item.ItemArray[5] as string;
                            var n1 = nodePorcedures.Nodes.Add($"{CurrenDatabaseName}.{user}.{runtimeName}", runtimeName);
                            n1.ImageIndex = 15;
                            n1.SelectedImageIndex = 15;
                            n1.ContextMenuStrip = cmStripProcs;
                            n1.ToolTipText = $"{item.ItemArray[6]}";
                            n1.Nodes.Add("fool");
                            if (!database.objectInSchema[CurrenDatabaseName + "_" + user].ContainsKey(runtimeName))
                            {
                                database.objectInSchema[CurrenDatabaseName + "_" + user].Add(runtimeName, TypeInDatabase.procedure);
                            }
                        }
                    }
                }

                var ndJobs = root.Nodes.Add("Jobs", "Jobs");
                ndJobs.ContextMenuStrip = null;
                ndJobs.ImageIndex = 38;
                ndJobs.SelectedImageIndex = 38;


                foreach (var job in database.Jobs)
                {
                    var ndOneJob = ndJobs.Nodes.Add(job.Key, job.Value.Name);
                    ndOneJob.ContextMenuStrip = null;
                    ndOneJob.ImageIndex = 38;
                    ndOneJob.SelectedImageIndex = 38;

                    var ndTemp = ndOneJob.Nodes.Add($"Enabled = {job.Value.Enabled}");
                    ndTemp.ContextMenuStrip = null;
                    ndTemp = ndOneJob.Nodes.Add($"Description = {job.Value.Description}");
                    ndTemp.ContextMenuStrip = null;
                    ndTemp = ndOneJob.Nodes.Add($"Created = {job.Value.Created}");
                    ndTemp.ContextMenuStrip = null;
                    ndTemp = ndOneJob.Nodes.Add($"Modified = {job.Value.Modified}");
                }

            });
        }
    }
}
#endif
