#if INCLUDE_ORACLE
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data;

using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Extensions
{
    public static class OracleExtensions
    {
        private static bool IsGoodName(string name)
        {
            return !string.IsNullOrEmpty(name) && !name.Contains(' ') && !name.Contains('-') && !name.Contains('.') && !name.Contains('[') && !name.Contains(']');
        }
        /// <summary>
        /// Initializes the schema tree view for Oracle database
        /// </summary>
        public static void InitOracleSchema(this AppBase.Data.Oracle database, TreeView treeView, ContextMenuStrip cmStripTabeli, ContextMenuStrip cmStripViewGeneral, ContextMenuStrip cmStripAliasesGeneral, ContextMenuStrip cmStripProcs, ContextMenuStrip cmStripSynonymsGeneral, string connName, ContextMenuStrip allTablesGeneral, ContextMenuStrip cmColumns, ContextMenuStrip cmConstraints, ContextMenuStrip cmIndexes, ContextMenuStrip cmPartitions, ContextMenuStrip cmTriggers, ContextMenuStrip cmsDB2Server, ContextMenuStrip cmsSynonyms, int index = -1)
        {
            treeView.Invoke(()=>
            {
                database.AutocompleteSuggestions.OneWord.Clear();

                int a1 = treeView.Nodes.IndexOfKey(connName);
                if (a1 != -1)
                {
                    treeView.Nodes.RemoveAt(a1);
                }

                //TreeNodeCollection tn = treeViewExtaDB.Nodes;
                var root = treeView.Nodes.Add(connName, connName);
                root.ImageIndex = 27;
                root.SelectedImageIndex = 27;

                var databases = root.Nodes.Add("Databases", "Databases");
                databases.ContextMenuStrip = null;
                databases.ImageIndex = 21;
                databases.SelectedImageIndex = 21;

                var currentDb = databases.Nodes.Add(database.DefaultDatabaseName, database.DefaultDatabaseName);
                currentDb.ImageIndex = 0;
                currentDb.SelectedImageIndex = 0;

                TreeNodeCollection tn = currentDb.Nodes;
                var tnList = new List<(TreeNodeCollection, string)>();

                foreach (DataRow user in database._users.Rows)
                {
                    TreeNode n1 = currentDb.Nodes.Add((user.ItemArray[0] as string), (user.ItemArray[0] as string));
                    n1.ImageIndex = 1;
                    n1.SelectedImageIndex = 1;
                    tnList.Add((n1.Nodes, n1.Text));
                    database.AutocompleteSuggestions.OneWord.Add(user.ItemArray[0] as string);
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

                    var nodeSynonyms = currentShemaNode.Add("Synonyms", "Synonyms");
                    nodeSynonyms.ImageIndex = 17;
                    nodeSynonyms.SelectedImageIndex = 17;
                    nodeSynonyms.Nodes.Add("fool");

                    var nodePorcedures = currentShemaNode.Add("Procedures", "Procedures");
                    nodePorcedures.ImageIndex = 5;
                    nodePorcedures.SelectedImageIndex = 5;
                    nodePorcedures.Nodes.Add("fool");

                    nodeTables.Nodes.Clear();

                    DataRow[] tableCol;

                    tableCol = database.tables?.Select($"TABLE_SCHEMA = '{user}'");
                    database.objectInSchema[user] = new Dictionary<string, TypeInDatabase>(StringComparer.OrdinalIgnoreCase);

                    foreach (DataRow item in tableCol)//owner, name,type
                    {
                        string tabName = item.ItemArray[1] as string;
                        if (!tabName.IsGoodName())
                        {
                            tabName = $"\"{tabName}\"";
                        }

                        TreeNode n1;
                        n1 = nodeTables.Nodes.Add($"{user}.{tabName}", tabName);

                        n1.ImageIndex = 8;
                        n1.SelectedImageIndex = 8;
                        n1.ContextMenuStrip = cmStripTabeli;
                        n1.Nodes.Add("fool");
                        database.AutocompleteSuggestions.TwoWords.Add($"{user}.{tabName}");
                        if (!database.objectInSchema[user].ContainsKey(tabName))
                        {
                            database.objectInSchema[user].Add(tabName, TypeInDatabase.table);
                        }
                    }

                    nodeSynonyms.Nodes.Clear();

                    var synonymCol = database._synonyms.Select($"OWNER = '{user}'");
                    foreach (DataRow item in synonymCol)
                    {
                        TreeNode n1 = nodeSynonyms.Nodes.Add($"{user}.{item.ItemArray[1]}", item.ItemArray[1] as string);
                        n1.ImageIndex = 18;
                        n1.SelectedImageIndex = 18;
                        n1.Nodes.Add("fool");
                        database.AutocompleteSuggestions.TwoWords.Add($"{user}.{item.ItemArray[1]}");
                        if (!database.objectInSchema[user].ContainsKey(item.ItemArray[1] as string))
                        {
                            database.objectInSchema[user].Add(item.ItemArray[1] as string, TypeInDatabase.synonym);
                        }
                    }
                    nodeViews.Nodes.Clear();
                    DataRow[] viewCol;
                    viewCol = database.views?.Select($"OWNER = '{user}'");

                    if (viewCol is not null)
                    {
                        foreach (DataRow item in viewCol)
                        {
                            TreeNode n1;
                            n1 = nodeViews.Nodes.Add($"{user}.{item["VIEW_NAME"]}", item["VIEW_NAME"] as string);
                            n1.ImageIndex = 9;
                            n1.SelectedImageIndex = 9;
                            n1.ContextMenuStrip = cmStripViewGeneral;
                            n1.Nodes.Add("fool");
                            database.AutocompleteSuggestions.TwoWords.Add($"{user}.{item["VIEW_NAME"]}");
                            if (!database.objectInSchema[user].ContainsKey(item["VIEW_NAME"] as string))
                            {
                                database.objectInSchema[user].Add(item["VIEW_NAME"] as string, TypeInDatabase.view);
                            }
                        }
                    }


                    var procs = database.procedures;
                    nodePorcedures.Nodes.Clear();

                    DataRow[] procCol;

                    procCol = procs?.Select($"OWNER = '{user}'");

                    foreach (DataRow item in procCol)
                    {
                        var n1 = nodePorcedures.Nodes.Add($"{user}.{item.ItemArray[1]}", item.ItemArray[1] as string);
                        n1.ImageIndex = 15;
                        n1.SelectedImageIndex = 15;
                        n1.ContextMenuStrip = cmStripProcs;
                        n1.Nodes.Add("fool");
                        if (!database.objectInSchema[user].ContainsKey(item.ItemArray[1] as string))
                        {
                            database.objectInSchema[user].Add(item.ItemArray[1] as string, TypeInDatabase.procedure);
                        }
                    }
                }
            });
        }
    }
}
#endif
