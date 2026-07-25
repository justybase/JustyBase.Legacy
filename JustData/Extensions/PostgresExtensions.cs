#if INCLUDE_POSTGRES
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Extensions
{
    public static class PostgresExtensions
    {
        private static bool IsGoodName(string name)
        {
            return !string.IsNullOrEmpty(name) && !name.Contains(' ') && !name.Contains('-') && !name.Contains('.') && !name.Contains('[') && !name.Contains(']');
        }
        /// <summary>
        /// Initializes the schema tree view for PostgreSQL database
        /// </summary>
        public static void InitPostgresSchema(this Postgres database, TreeView treeView, ContextMenuStrip cmStripTabeli, ContextMenuStrip cmStripViewGeneral, ContextMenuStrip cmStripAliasesGeneral, ContextMenuStrip cmStripProcs, ContextMenuStrip cmStripSynonymsGeneral, string connName, ContextMenuStrip allTablesGeneral, ContextMenuStrip cmColumns, ContextMenuStrip cmConstraints, ContextMenuStrip cmIndexes, ContextMenuStrip cmPartitions, ContextMenuStrip cmTriggers, ContextMenuStrip cmsDB2Server, ContextMenuStrip cmsSynonyms, int index = -1)
        {
            treeView.Invoke(()=>
            {
                DynamicCollectionForGeneralHelpers.oneWord.Clear();
                database.indexes = null;
                int a1 = treeView.Nodes.IndexOfKey(connName);
                if (a1 != -1)
                {
                    treeView.Nodes.RemoveAt(a1);
                }

                //TreeNodeCollection tn = treeViewExtaDB.Nodes;
                var root = treeView.Nodes.Add(connName, connName);
                root.ImageIndex = 28;
                root.SelectedImageIndex = 28;

                var databases = root.Nodes.Add("Databases", "Databases");
                databases.ContextMenuStrip = null;
                databases.ImageIndex = 21;
                databases.SelectedImageIndex = 21;

                int n = database.dbs.Rows.Count;

                var currentDb = databases.Nodes.Add(database.DefaultDatabaseName, database.DefaultDatabaseName);
                currentDb.ImageIndex = 0;
                currentDb.SelectedImageIndex = 0;

                for (int i = 0; i < n; i++)
                {
                    string nm = database.dbs.Rows[i][0].ToString();
                    if (nm == database.DefaultDatabaseName)
                        continue;
                    var nn = databases.Nodes.Add(database.dbs.Rows[i][0].ToString(), database.dbs.Rows[i][0].ToString());
                    nn.ImageIndex = 0;
                    nn.SelectedImageIndex = 0;
                }

                TreeNodeCollection tn = currentDb.Nodes;
                var tnList = new List<(TreeNodeCollection, string)>();

                foreach (DataRow user in database._users.Rows)
                {
                    TreeNode n1 = currentDb.Nodes.Add((user.ItemArray[0] as string), (user.ItemArray[0] as string));
                    n1.ImageIndex = 1;
                    n1.SelectedImageIndex = 1;
                    tnList.Add((n1.Nodes, n1.Text));
                    DynamicCollectionForGeneralHelpers.oneWord.Add(user.ItemArray[0] as string);
                }

                foreach (var itx in tnList)
                {
                    var currentShemaNode = itx.Item1;
                    var user = itx.Item2;

                    var nodeTables = currentShemaNode.Add("Tables", "Tables");
                    nodeTables.ImageIndex = 1;
                    nodeTables.SelectedImageIndex = 1;

                    DataRow[] tableCol;
                    tableCol = database.tables?.Select($"table_schema = '{user}'");
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

                        var tmpNode = n1.Nodes.Add("Columns");
                        tmpNode.ImageIndex = 11;
                        tmpNode.SelectedImageIndex = 11;
                        tmpNode.Nodes.Add("fool");
                        tmpNode = n1.Nodes.Add("Constraints");
                        tmpNode.ImageIndex = 33;
                        tmpNode.SelectedImageIndex = 33;
                        tmpNode.Nodes.Add("fool");
                        tmpNode = n1.Nodes.Add("Indexes");
                        tmpNode.ImageIndex = 34;
                        tmpNode.SelectedImageIndex = 34;
                        tmpNode.Nodes.Add("fool");
                        tmpNode = n1.Nodes.Add("Partitions");
                        tmpNode.ImageIndex = 14;
                        tmpNode.SelectedImageIndex = 14;
                        tmpNode.Nodes.Add("fool");
                        tmpNode = n1.Nodes.Add("Triggers");
                        tmpNode.ImageIndex = 15;
                        tmpNode.SelectedImageIndex = 15;
                        tmpNode.Nodes.Add("fool");
                        DynamicCollectionForGeneralHelpers.twoWords.Add($"{user}.{tabName}");
                        if (!database.objectInSchema[user].ContainsKey(tabName))
                        {
                            database.objectInSchema[user].Add(tabName, TypeInDatabase.table);
                        }
                    }

                    var nodeViews = currentShemaNode.Add("Views", "Views");
                    nodeViews.ImageIndex = 2;
                    nodeViews.SelectedImageIndex = 2;

                    DataRow[] viewCol;
                    viewCol = database.views?.Select($"table_schema = '{user}'");
                    foreach (DataRow item in viewCol)
                    {
                        TreeNode n1;
                        n1 = nodeViews.Nodes.Add($"{user}.{item.ItemArray[1]}", item.ItemArray[1] as string);
                        n1.ImageIndex = 9;
                        n1.SelectedImageIndex = 9;
                        n1.ContextMenuStrip = cmStripViewGeneral;
                        var tmpNode = n1.Nodes.Add("Columns");
                        tmpNode.ImageIndex = 11;
                        tmpNode.SelectedImageIndex = 11;
                        tmpNode.Nodes.Add("fool");
                        DynamicCollectionForGeneralHelpers.twoWords.Add($"{user}.{item.ItemArray[1]}");
                        if (!database.objectInSchema[user].ContainsKey(item.ItemArray[1] as string))
                        {
                            database.objectInSchema[user].Add(item.ItemArray[1] as string, TypeInDatabase.view);
                        }
                    }


                    var funcs = currentShemaNode.Add("Functions", "Functions");
                    funcs.ImageIndex = 19;
                    funcs.SelectedImageIndex = 19;
                    funcs.ToolTipText = "information_schema.routines";

                    foreach (DataRow item in database.procedures.Select($"routine_schema = '{user}'"))
                    {
                        string tabName = item["routine_name"].ToString();
                        string type = item["routine_type"].ToString();
                        if (!tabName.IsGoodName())
                        {
                            tabName = $"\"{tabName}\"";
                        }
                        TreeNode n1 = null;
                        switch (type)
                        {
                            //case "PROCEDURE":
                            //    n1 = procs.Nodes.Add($"{user}.{tabName}", tabName);
                            //    n1.ImageIndex = 15;
                            //    n1.SelectedImageIndex = 15;
                            //    break;
                            case "FUNCTION":
                                n1 = funcs.Nodes.Add($"{user}.{tabName}", tabName);
                                n1.ImageIndex = 19;
                                n1.SelectedImageIndex = 19;
                                n1.ContextMenuStrip = null;
                                break;
                            default:
                                break;
                        }

                    }

                    var seq = currentShemaNode.Add("Sequences", "Sequences");
                    seq.ImageIndex = 7;
                    seq.SelectedImageIndex = 7;
                    seq.ToolTipText = "information_schema.sequences ";

                    foreach (DataRow item in database._sequences.Select($"sequence_schema = '{user}'"))
                    {
                        string tabName = item["sequence_name"].ToString();
                        if (!tabName.IsGoodName())
                        {
                            tabName = $"\"{tabName}\"";
                        }
                        TreeNode n1 = null;
                        n1 = seq.Nodes.Add($"{user}.{tabName}", tabName);
                        n1.ContextMenuStrip = null;
                    }
                }
            });
        }
    }
}
#endif
