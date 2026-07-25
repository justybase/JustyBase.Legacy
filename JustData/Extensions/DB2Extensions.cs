#if INCLUDE_DB2
using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data;
using JustyBaseLegacy.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Extensions
{
    public static class DB2Extensions
    {
        /// <summary>
        /// Initializes the schema tree view for DB2 database
        /// </summary>
        public static void InitDB2Schema(this DB2 database, TreeView treeView, ContextMenuStrip cmStripTabeli, ContextMenuStrip cmStripViewGeneral, ContextMenuStrip cmStripAliasesGeneral, ContextMenuStrip cmStripProcs, ContextMenuStrip cmStripSynonymsGeneral, string connName, ContextMenuStrip allTablesGeneral, ContextMenuStrip cmColumns, ContextMenuStrip cmConstraints, ContextMenuStrip cmIndexes, ContextMenuStrip cmPartitions, ContextMenuStrip cmTriggers, ContextMenuStrip cmsDB2Server, ContextMenuStrip cmsSynonyms, int index = -1)
        {
            try
            {
                database._initSchemaInProgress = true;
                treeView.Invoke(()=>
                {
                    DynamicCollectionForGeneralHelpers.oneWord.Clear();

                    int a1 = treeView.Nodes.IndexOfKey(connName);
                    if (a1 != -1)
                    {
                        treeView.Nodes.RemoveAt(a1);
                    }

                    //TreeNodeCollection tn = treeViewExtaDB.Nodes;

                    TreeNode root;
                    if (index != -1)
                    {
                        root = treeView.Nodes.Insert(index, connName, connName);
                    }
                    else
                    {
                        root = treeView.Nodes.Add(connName, connName);
                    }

                    root.ImageIndex = 26;
                    root.SelectedImageIndex = 26;
                    //root.ToolTipText = _generalDbService.Server(connName);

                    var databases = root.Nodes.Add("Databases", "Databases");
                    databases.ContextMenuStrip = EmptyContextMenuStrip;
                    databases.ImageIndex = 21;
                    databases.SelectedImageIndex = 21;


                    var linkedServers = root.Nodes.Add("Linked Servers", "Linked Servers");
                    linkedServers.ImageIndex = 35;
                    linkedServers.SelectedImageIndex = 35;
                    linkedServers.ContextMenuStrip = cmsDB2Server;

                    var nodeWr = linkedServers.Nodes.Add("Wrappers", "Wrappers");
                    nodeWr.ContextMenuStrip = EmptyContextMenuStrip;
                    nodeWr.ImageIndex = linkedServers.ImageIndex;
                    nodeWr.SelectedImageIndex = linkedServers.SelectedImageIndex;


                    Dictionary<string, List<(string, string)>> wrapOptionsDic = new();

                    foreach (DataRow item in database._wrappersOptionsDt.Rows)
                    {
                        string WRAPNAME = item["WRAPNAME"].ToString();
                        string OPTION = item["OPTION"].ToString();
                        string SETTING = item["SETTING"].ToString();
                        if (!wrapOptionsDic.ContainsKey(WRAPNAME))
                        {
                            wrapOptionsDic[WRAPNAME] = new List<(string, string)>();
                        }
                        wrapOptionsDic[WRAPNAME].Add((OPTION, SETTING));
                    }


                    foreach (DataRow item in database._wrappersDt.Rows)
                    {
                        string wrapName = item["WRAPNAME"].ToString();
                        var n = nodeWr.Nodes.Add(wrapName, wrapName);
                        n.ToolTipText = $"WRAPNAME: {wrapName}";
                        n.ContextMenuStrip = EmptyContextMenuStrip;
                        n.ImageIndex = linkedServers.ImageIndex;
                        n.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        string wrapType = item["WRAPTYPE"].ToString();
                        var n1 = n.Nodes.Add($"Wraptype: {wrapType}", $"Wraptype: {wrapType}");
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        n1.ImageIndex = linkedServers.ImageIndex;
                        n1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        string wrapVersion = item["WRAPVERSION"].ToString();
                        n1 = n.Nodes.Add($"Wrapversion: {wrapVersion}", $"Wrapversion: {wrapVersion}");
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        n1.ImageIndex = linkedServers.ImageIndex;
                        n1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        string wrapLibrary = item["LIBRARY"].ToString();
                        n1 = n.Nodes.Add($"Library: {wrapLibrary}", $"Library: {wrapLibrary}");
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        n1.ImageIndex = linkedServers.ImageIndex;
                        n1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        string wrapRemarks = item["REMARKS"].ToString();
                        n1 = n.Nodes.Add($"Remarks: {wrapRemarks}", $"Remarks: {wrapRemarks}");
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        n1.ImageIndex = linkedServers.ImageIndex;
                        n1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        n1 = n.Nodes.Add($"Options", $"Options");
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        n1.ImageIndex = 32;
                        n1.SelectedImageIndex = 32;

                        foreach (var listItme in wrapOptionsDic[wrapName])
                        {
                            var n2 = n1.Nodes.Add($"{listItme.Item1}:{listItme.Item2}", $"{listItme.Item1}:{listItme.Item2}");
                            n2.ContextMenuStrip = EmptyContextMenuStrip;
                            n2.ImageIndex = 32;
                            n2.SelectedImageIndex = 32;
                        }
                    }

                    var servers = linkedServers.Nodes.Add("Servers", "Servers");
                    servers.ContextMenuStrip = EmptyContextMenuStrip;
                    servers.ImageIndex = linkedServers.ImageIndex;
                    servers.SelectedImageIndex = linkedServers.SelectedImageIndex;



                    Dictionary<string, List<(string, string)>> serverOptionDic = new();

                    foreach (DataRow item in database._linkedServersOptionsDt.Rows)
                    {
                        string SERVERNAME = item["SERVERNAME"].ToString();
                        string OPTION = item["OPTION"].ToString();
                        string SETTING = item["SETTING"].ToString();
                        if (!serverOptionDic.ContainsKey(SERVERNAME))
                        {
                            serverOptionDic[SERVERNAME] = new List<(string, string)>();
                        }
                        serverOptionDic[SERVERNAME].Add((OPTION, SETTING));
                    }


                    foreach (DataRow item in database._linkedServersDt.Rows)
                    {
                        string SERVERNAME = item["SERVERNAME"].ToString();
                        var n = servers.Nodes.Add(item["SERVERNAME"].ToString(), item["SERVERNAME"].ToString());
                        n.ToolTipText = item["SERVERTYPE"].ToString() + " " + item["REMARKS"].ToString();
                        n.ContextMenuStrip = EmptyContextMenuStrip;
                        n.ImageIndex = linkedServers.ImageIndex;
                        n.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        string WRAPNAME = item["WRAPNAME"].ToString();
                        string SERVERTYPE = item["SERVERTYPE"].ToString();
                        string SERVERVERSION = item["SERVERVERSION"].ToString();
                        string REMARKS = item["REMARKS"].ToString();

                        var n2 = n.Nodes.Add($"Wrapper:{WRAPNAME}", $"Wrapper:{WRAPNAME}");
                        n2.ContextMenuStrip = EmptyContextMenuStrip;
                        n2.ImageIndex = linkedServers.ImageIndex;
                        n2.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        n2 = n.Nodes.Add($"Servertype:{SERVERTYPE}", $"Servertype:{SERVERTYPE}");
                        n2.ContextMenuStrip = EmptyContextMenuStrip;
                        n2.ImageIndex = linkedServers.ImageIndex;
                        n2.SelectedImageIndex = linkedServers.SelectedImageIndex;


                        n2 = n.Nodes.Add($"Serverversion:{SERVERVERSION}", $"Serverversion:{SERVERVERSION}");
                        n2.ContextMenuStrip = EmptyContextMenuStrip;
                        n2.ImageIndex = linkedServers.ImageIndex;
                        n2.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        n2 = n.Nodes.Add($"Remarks:{REMARKS}", $"Remarks:{REMARKS}");
                        n2.ContextMenuStrip = EmptyContextMenuStrip;
                        n2.ImageIndex = linkedServers.ImageIndex;
                        n2.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        n2 = n.Nodes.Add("Options", "Options");
                        n2.ContextMenuStrip = EmptyContextMenuStrip;
                        n2.ImageIndex = linkedServers.ImageIndex;
                        n2.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        foreach (var listItme in serverOptionDic[SERVERNAME])
                        {
                            var n3 = n2.Nodes.Add($"{listItme.Item1}:{listItme.Item2}", $"{listItme.Item1}:{listItme.Item2}");
                            n3.ContextMenuStrip = EmptyContextMenuStrip;
                            n3.ImageIndex = 32;
                            n3.SelectedImageIndex = 32;
                        }

                        n2 = n.Nodes.Add($"Server Objects", $"Server Objects");
                        n2.ContextMenuStrip = EmptyContextMenuStrip;
                        n2.ImageIndex = linkedServers.ImageIndex;
                        n2.SelectedImageIndex = linkedServers.SelectedImageIndex;
                        n2.Nodes.Add("fool");
                    }

                    var mappings = linkedServers.Nodes.Add("User mappings", "User mappings");
                    mappings.ContextMenuStrip = EmptyContextMenuStrip;
                    mappings.ImageIndex = linkedServers.ImageIndex;
                    mappings.SelectedImageIndex = linkedServers.SelectedImageIndex;


                    Dictionary<string, Dictionary<string, List<(string, string, string)>>> mappingsDic = new();
                    //         SERVERNAME,         AUTHID ->   AUTHIDTYPE,  OPTION, SETTING

                    foreach (DataRow item in database._userMapingsDt.Rows)
                    {
                        string SERVERNAME = item["SERVERNAME"].ToString();

                        string AUTHID = item["AUTHID"].ToString();
                        string AUTHIDTYPE = item["AUTHIDTYPE"].ToString();

                        string OPTION = item["OPTION"].ToString();
                        string SETTING = item["SETTING"].ToString();

                        if (!mappingsDic.ContainsKey(SERVERNAME))
                        {
                            mappingsDic[SERVERNAME] = new Dictionary<string, List<(string, string, string)>>();
                        }
                        if (!mappingsDic[SERVERNAME].ContainsKey(AUTHID))
                        {
                            mappingsDic[SERVERNAME][AUTHID] = new List<(string, string, string)>();
                        }
                        mappingsDic[SERVERNAME][AUTHID].Add((AUTHIDTYPE, OPTION, SETTING));
                    }

                    foreach (var item in mappingsDic)
                    {
                        var n1 = mappings.Nodes.Add(item.Key, item.Key); // server name
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        n1.ImageIndex = linkedServers.ImageIndex;
                        n1.SelectedImageIndex = linkedServers.SelectedImageIndex;
                        foreach (var item2 in item.Value)
                        {
                            var n2 = n1.Nodes.Add(item2.Key, item2.Key); // user name
                            n2.ContextMenuStrip = EmptyContextMenuStrip;
                            n2.ImageIndex = 22;
                            n2.SelectedImageIndex = 22;

                            string AUTHIDTYPE = "";
                            foreach (var item3 in item2.Value)
                            {
                                var n3 = n2.Nodes.Add($"{item3.Item2}: {item3.Item3}", $"{item3.Item2}: {item3.Item3}");
                                n3.ContextMenuStrip = EmptyContextMenuStrip;
                                n3.ImageIndex = 32;
                                n3.SelectedImageIndex = 32;
                                AUTHIDTYPE = item3.Item1;
                            }
                            var n4 = n2.Nodes.Add($"AUTHIDTYPE: {AUTHIDTYPE}", $"AUTHIDTYPE: {AUTHIDTYPE}");
                            n4.ContextMenuStrip = EmptyContextMenuStrip;
                            n4.ImageIndex = 32;
                            n4.SelectedImageIndex = 32;
                        }
                    }


                    var passthru = linkedServers.Nodes.Add("Passthru Auth", "Passthru Auth");
                    passthru.ContextMenuStrip = EmptyContextMenuStrip;
                    passthru.ImageIndex = linkedServers.ImageIndex;
                    passthru.SelectedImageIndex = linkedServers.SelectedImageIndex;

                    Dictionary<string, List<(string, string, string, string)>> passthruDic = new();

                    foreach (DataRow row in database._passthruDt.Rows)
                    {
                        string SERVERNAME = row["SERVERNAME"].ToString().Trim();
                        string GRANTOR = row["GRANTOR"].ToString().Trim();
                        string GRANTORTYPE = row["GRANTORTYPE"].ToString().Trim();
                        string GRANTEE = row["GRANTEE"].ToString().Trim();
                        string GRANTEETYPE = row["GRANTEETYPE"].ToString().Trim();
                        if (!passthruDic.ContainsKey(SERVERNAME))
                        {
                            passthruDic[SERVERNAME] = new List<(string, string, string, string)>();
                        }
                        passthruDic[SERVERNAME].Add((GRANTOR, GRANTORTYPE, GRANTEE, GRANTEETYPE));
                    }
                    foreach (var item in passthruDic)
                    {
                        var n1 = passthru.Nodes.Add(item.Key, item.Key); // server name
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        n1.ImageIndex = linkedServers.ImageIndex;
                        n1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                        int nx = 1;
                        foreach (var item2 in item.Value)
                        {
                            var nn = n1.Nodes.Add($"Auth {nx} ({item2.Item3})", $"Auth {nx} ({item2.Item3})");
                            nn.ContextMenuStrip = EmptyContextMenuStrip;
                            nn.ImageIndex = linkedServers.ImageIndex;
                            nn.SelectedImageIndex = linkedServers.SelectedImageIndex;
                            ++nx;

                            var nn1 = nn.Nodes.Add($"Grantee: {item2.Item3}", $"Grantee: {item2.Item3}");
                            nn1.ContextMenuStrip = EmptyContextMenuStrip;
                            nn1.ImageIndex = linkedServers.ImageIndex;
                            nn1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                            nn1 = nn.Nodes.Add($"Grantee type: {item2.Item4}", $"Grantee type: {item2.Item4}");
                            nn1.ContextMenuStrip = EmptyContextMenuStrip;
                            nn1.ImageIndex = linkedServers.ImageIndex;
                            nn1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                            nn1 = nn.Nodes.Add($"Grantor: {item2.Item1}", $"Grantor: {item2.Item1}");
                            nn1.ContextMenuStrip = EmptyContextMenuStrip;
                            nn1.ImageIndex = linkedServers.ImageIndex;
                            nn1.SelectedImageIndex = linkedServers.SelectedImageIndex;

                            nn1 = nn.Nodes.Add($"Grantor type: {item2.Item2}", $"Grantor type: {item2.Item2}");
                            nn1.ContextMenuStrip = EmptyContextMenuStrip;
                            nn1.ImageIndex = linkedServers.ImageIndex;
                            nn1.SelectedImageIndex = linkedServers.SelectedImageIndex;
                        }
                    }

                    var currentDb = databases.Nodes.Add(database.DefaultDatabaseName, database.DefaultDatabaseName + " (" + ((double)database._bytesSize / 1024 / 1024 / 1024).ToString("N2") + " GB)");
                    currentDb.ImageIndex = 0;
                    currentDb.SelectedImageIndex = 0;
                    currentDb.ContextMenuStrip = EmptyContextMenuStrip;

                    TreeNodeCollection tn = currentDb.Nodes;
                    var tnList = new List<(TreeNodeCollection, TreeNode)>();

                    foreach (DataRow item in database._schemas.Rows)
                    {
                        string schemaName = (item.ItemArray[0] as string);
                        string validSchemaName = schemaName;
                        //QuoteNameIfNeeded(ref validSchemaName);

                        TreeNode n1 = currentDb.Nodes.Add(schemaName, validSchemaName);
                        n1.ImageIndex = 1;
                        n1.SelectedImageIndex = 1;
                        tnList.Add((n1.Nodes, n1));
                        n1.ContextMenuStrip = EmptyContextMenuStrip;
                        DynamicCollectionForGeneralHelpers.oneWord.Add(validSchemaName);
                    }

                    foreach (var itx in tnList)
                    {
                        var currentShemaNode = itx.Item1;
                        var schema = itx.Item2.Name;
                        var validSchemaName = itx.Item2.Text;

                        var nodeTables = currentShemaNode.Add("Tables", "Tables");
                        nodeTables.ImageIndex = 1;
                        nodeTables.SelectedImageIndex = 1;
                        nodeTables.ContextMenuStrip = allTablesGeneral;
                        nodeTables.Nodes.Add("fool");

                        var nodeViews = currentShemaNode.Add("Views", "Views");
                        nodeViews.ImageIndex = 2;
                        nodeViews.SelectedImageIndex = 2;
                        nodeViews.ContextMenuStrip = EmptyContextMenuStrip;
                        nodeViews.Nodes.Add("fool");


                        var nodeSynonyms = currentShemaNode.Add("Synonyms", "Synonyms");
                        nodeSynonyms.ImageIndex = 17;
                        nodeSynonyms.SelectedImageIndex = 17;
                        nodeSynonyms.ContextMenuStrip = cmsSynonyms;
                        nodeSynonyms.Nodes.Add("fool");

                        var nodeAliases = currentShemaNode.Add("Aliases", "Aliases");
                        nodeAliases.ImageIndex = 13;
                        nodeAliases.SelectedImageIndex = 13;
                        nodeAliases.ContextMenuStrip = EmptyContextMenuStrip;
                        nodeAliases.Nodes.Add("fool");

                        var nodePorcedures = currentShemaNode.Add("Procedures", "Procedures");
                        nodePorcedures.ImageIndex = 5;
                        nodePorcedures.SelectedImageIndex = 5;
                        nodePorcedures.ContextMenuStrip = EmptyContextMenuStrip;
                        nodePorcedures.Nodes.Add("fool");

                        nodeTables.Nodes.Clear();

                        DataRow[] tableCol;

                        tableCol = database.tables?.Select($"TABLE_SCHEMA = '{schema}'");

                        database.objectInSchema[validSchemaName] = new Dictionary<string, TypeInDatabase>(StringComparer.OrdinalIgnoreCase);
                        foreach (DataRow item in tableCol)
                        {
                            string tableName = item.ItemArray[2] as string;
                            //QuoteNameIfNeeded(ref tableName);

                            TreeNode n1;
                            n1 = nodeTables.Nodes.Add($"{schema}.{tableName}", tableName);
                            n1.ImageIndex = 8;
                            n1.SelectedImageIndex = 8;
                            n1.ContextMenuStrip = cmStripTabeli;
                            var tmpNode = n1.Nodes.Add("Columns");
                            tmpNode.ImageIndex = 11;
                            tmpNode.SelectedImageIndex = 11;
                            tmpNode.ContextMenuStrip = cmColumns;
                            tmpNode.Nodes.Add("fool");

                            tmpNode = n1.Nodes.Add("Constraints");
                            tmpNode.ImageIndex = 33;
                            tmpNode.SelectedImageIndex = 33;
                            tmpNode.ContextMenuStrip = cmConstraints;
                            tmpNode.Nodes.Add("fool");

                            tmpNode = n1.Nodes.Add("Indexes");
                            tmpNode.ImageIndex = 34;
                            tmpNode.SelectedImageIndex = 34;
                            tmpNode.ContextMenuStrip = cmIndexes;
                            tmpNode.Nodes.Add("fool");

                            tmpNode = n1.Nodes.Add("Partitions");
                            tmpNode.ImageIndex = 14;
                            tmpNode.SelectedImageIndex = 14;
                            tmpNode.ContextMenuStrip = cmPartitions;
                            tmpNode.Nodes.Add("fool");

                            tmpNode = n1.Nodes.Add("Triggers");
                            tmpNode.ImageIndex = 15;
                            tmpNode.SelectedImageIndex = 15;
                            tmpNode.ContextMenuStrip = cmTriggers;
                            tmpNode.Nodes.Add("fool");
                            DynamicCollectionForGeneralHelpers.twoWords.Add($"{validSchemaName}.{tableName}");

                            if (!database.objectInSchema[validSchemaName].ContainsKey(tableName))
                            {
                                database.objectInSchema[validSchemaName].Add(tableName, TypeInDatabase.table);
                            }
                        }

                        nodeSynonyms.Nodes.Clear();
                        var synonymCol = database._synonyms.Select($"TABLE_SCHEMA = '{schema}'");
                        foreach (DataRow item in synonymCol)
                        {
                            TreeNode n1 = nodeSynonyms.Nodes.Add($"{schema}.{item.ItemArray[2]}", item.ItemArray[2] as string);
                            n1.ImageIndex = 18;
                            n1.SelectedImageIndex = 18;
                            n1.ContextMenuStrip = cmStripSynonymsGeneral;
                            n1.Nodes.Add("fool");
                            DynamicCollectionForGeneralHelpers.twoWords.Add($"{validSchemaName}.{item.ItemArray[2]}");
                            if (!database.objectInSchema[validSchemaName].ContainsKey(item.ItemArray[2] as string))
                            {
                                database.objectInSchema[validSchemaName].Add(item.ItemArray[2] as string, TypeInDatabase.synonym);
                            }

                        }

                        nodeAliases.Nodes.Clear();
                        var aliasesCol = database._aliases.Select($"TABLE_SCHEMA = '{schema}'");
                        foreach (DataRow item in aliasesCol)
                        {
                            TreeNode n1 = nodeAliases.Nodes.Add($"{schema}.{item.ItemArray[2]}", item.ItemArray[2] as string);
                            n1.ImageIndex = 18;
                            n1.SelectedImageIndex = 18;
                            n1.ContextMenuStrip = cmStripAliasesGeneral;
                            n1.Nodes.Add("fool");
                            DynamicCollectionForGeneralHelpers.twoWords.Add($"{validSchemaName}.{item.ItemArray[2]}");
                            if (!database.objectInSchema[validSchemaName].ContainsKey(item.ItemArray[2] as string))
                            {
                                database.objectInSchema[validSchemaName].Add(item.ItemArray[2] as string, TypeInDatabase.db2alias);
                            }

                        }

                        nodeViews.Nodes.Clear();
                        DataRow[] viewCol;
                        viewCol = database.views?.Select($"TABLE_SCHEMA = '{schema}'");

                        foreach (DataRow item in viewCol)
                        {
                            TreeNode n1;
                            n1 = nodeViews.Nodes.Add($"{schema}.{item.ItemArray[2]}", item.ItemArray[2] as string);
                            n1.ImageIndex = 9;
                            n1.SelectedImageIndex = 9;
                            n1.ContextMenuStrip = cmStripViewGeneral;

                            var tmpNode = n1.Nodes.Add("Columns");
                            tmpNode.ImageIndex = 11;
                            tmpNode.SelectedImageIndex = 11;
                            tmpNode.ContextMenuStrip = EmptyContextMenuStrip;
                            tmpNode.Nodes.Add("fool");
                            DynamicCollectionForGeneralHelpers.twoWords.Add($"{validSchemaName}.{item.ItemArray[2]}");
                            if (!database.objectInSchema[validSchemaName].ContainsKey(item.ItemArray[2] as string))
                            {
                                database.objectInSchema[validSchemaName].Add(item.ItemArray[2] as string, TypeInDatabase.view);
                            }
                        }

                        var procs = database.procedures;
                        nodePorcedures.Nodes.Clear();

                        DataRow[] procCol;

                        procCol = procs?.Select($"PROCEDURE_SCHEMA = '{schema}'");

                        foreach (DataRow item in procCol)
                        {
                            var n1 = nodePorcedures.Nodes.Add((item.ItemArray[1] as string) + "." + (item.ItemArray[2] as string), item.ItemArray[2] as string);
                            n1.ImageIndex = 15;
                            n1.SelectedImageIndex = 15;
                            n1.ContextMenuStrip = cmStripProcs;
                            n1.Nodes.Add("fool");
                            if (!database.objectInSchema[validSchemaName].ContainsKey(item.ItemArray[2] as string))
                            {
                                database.objectInSchema[validSchemaName].Add(item.ItemArray[2] as string, TypeInDatabase.procedure);
                            }
                        }
                    }
                });
            }
            finally
            {
                database._initSchemaInProgress = false;
            }
        }

        private static void AddLinkedServersSection(DB2 database, TreeNode root, ContextMenuStrip cmsDB2Server)
        {
            var linkedServers = root.Nodes.Add("Linked Servers", "Linked Servers");
            linkedServers.ImageIndex = 35;
            linkedServers.SelectedImageIndex = 35;
            linkedServers.ContextMenuStrip = cmsDB2Server;

            // Add Wrappers, Servers, User mappings, and Passthru Auth nodes
            // This would require access to the specific DB2 data tables
            // For now, we'll add placeholder nodes
            var nodeWr = linkedServers.Nodes.Add("Wrappers", "Wrappers");
            nodeWr.ContextMenuStrip = EmptyContextMenuStrip;
            nodeWr.ImageIndex = linkedServers.ImageIndex;
            nodeWr.SelectedImageIndex = linkedServers.SelectedImageIndex;

            var servers = linkedServers.Nodes.Add("Servers", "Servers");
            servers.ContextMenuStrip = EmptyContextMenuStrip;
            servers.ImageIndex = linkedServers.ImageIndex;
            servers.SelectedImageIndex = linkedServers.SelectedImageIndex;

            var mappings = linkedServers.Nodes.Add("User mappings", "User mappings");
            mappings.ContextMenuStrip = EmptyContextMenuStrip;
            mappings.ImageIndex = linkedServers.ImageIndex;
            mappings.SelectedImageIndex = linkedServers.SelectedImageIndex;

            var passthru = linkedServers.Nodes.Add("Passthru Auth", "Passthru Auth");
            passthru.ContextMenuStrip = EmptyContextMenuStrip;
            passthru.ImageIndex = linkedServers.ImageIndex;
            passthru.SelectedImageIndex = linkedServers.SelectedImageIndex;
        }

        private static void AddSchemaNodes(DB2 database, TreeNodeCollection currentShemaNode, string schema, string validSchemaName, ContextMenuStrip cmStripTabeli, ContextMenuStrip cmStripViewGeneral, ContextMenuStrip cmStripAliasesGeneral, ContextMenuStrip cmStripProcs, ContextMenuStrip cmsSynonyms, ContextMenuStrip allTablesGeneral, ContextMenuStrip cmColumns, ContextMenuStrip cmConstraints, ContextMenuStrip cmIndexes, ContextMenuStrip cmPartitions, ContextMenuStrip cmTriggers)
        {
            var nodeTables = currentShemaNode.Add("Tables", "Tables");
            nodeTables.ImageIndex = 1;
            nodeTables.SelectedImageIndex = 1;
            nodeTables.ContextMenuStrip = allTablesGeneral;
            nodeTables.Nodes.Add("fool");

            var nodeViews = currentShemaNode.Add("Views", "Views");
            nodeViews.ImageIndex = 2;
            nodeViews.SelectedImageIndex = 2;
            nodeViews.ContextMenuStrip = EmptyContextMenuStrip;
            nodeViews.Nodes.Add("fool");

            var nodeSynonyms = currentShemaNode.Add("Synonyms", "Synonyms");
            nodeSynonyms.ImageIndex = 17;
            nodeSynonyms.SelectedImageIndex = 17;
            nodeSynonyms.ContextMenuStrip = cmsSynonyms;
            nodeSynonyms.Nodes.Add("fool");

            var nodeAliases = currentShemaNode.Add("Aliases", "Aliases");
            nodeAliases.ImageIndex = 13;
            nodeAliases.SelectedImageIndex = 13;
            nodeAliases.ContextMenuStrip = EmptyContextMenuStrip;
            nodeAliases.Nodes.Add("fool");

            var nodePorcedures = currentShemaNode.Add("Procedures", "Procedures");
            nodePorcedures.ImageIndex = 5;
            nodePorcedures.SelectedImageIndex = 5;
            nodePorcedures.ContextMenuStrip = EmptyContextMenuStrip;
            nodePorcedures.Nodes.Add("fool");

            // Clear and populate tables
            nodeTables.Nodes.Clear();
            DataRow[] tableCol = database.tables?.Select($"TABLE_SCHEMA = '{schema}'");
            database.objectInSchema[validSchemaName] = new Dictionary<string, TypeInDatabase>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow item in tableCol)
            {
                string tableName = item.ItemArray[2] as string;
                TreeNode n1 = nodeTables.Nodes.Add($"{schema}.{tableName}", tableName);
                n1.ImageIndex = 8;
                n1.SelectedImageIndex = 8;
                n1.ContextMenuStrip = cmStripTabeli;

                // Add child nodes for table details
                AddTableChildNodes(n1, cmColumns, cmConstraints, cmIndexes, cmPartitions, cmTriggers);

                DynamicCollectionForGeneralHelpers.twoWords.Add($"{validSchemaName}.{tableName}");
                if (!database.objectInSchema[validSchemaName].ContainsKey(tableName))
                {
                    database.objectInSchema[validSchemaName].Add(tableName, TypeInDatabase.table);
                }
            }

            // Add other schema objects (views, synonyms, aliases, procedures)
            // This would require access to the specific data tables
        }

        private static void AddTableChildNodes(TreeNode tableNode, ContextMenuStrip cmColumns, ContextMenuStrip cmConstraints, ContextMenuStrip cmIndexes, ContextMenuStrip cmPartitions, ContextMenuStrip cmTriggers)
        {
            var tmpNode = tableNode.Nodes.Add("Columns");
            tmpNode.ImageIndex = 11;
            tmpNode.SelectedImageIndex = 11;
            tmpNode.ContextMenuStrip = cmColumns;
            tmpNode.Nodes.Add("fool");

            tmpNode = tableNode.Nodes.Add("Constraints");
            tmpNode.ImageIndex = 33;
            tmpNode.SelectedImageIndex = 33;
            tmpNode.ContextMenuStrip = cmConstraints;
            tmpNode.Nodes.Add("fool");

            tmpNode = tableNode.Nodes.Add("Indexes");
            tmpNode.ImageIndex = 34;
            tmpNode.SelectedImageIndex = 34;
            tmpNode.ContextMenuStrip = cmIndexes;
            tmpNode.Nodes.Add("fool");

            tmpNode = tableNode.Nodes.Add("Partitions");
            tmpNode.ImageIndex = 14;
            tmpNode.SelectedImageIndex = 14;
            tmpNode.ContextMenuStrip = cmPartitions;
            tmpNode.Nodes.Add("fool");

            tmpNode = tableNode.Nodes.Add("Triggers");
            tmpNode.ImageIndex = 15;
            tmpNode.SelectedImageIndex = 15;
            tmpNode.ContextMenuStrip = cmTriggers;
            tmpNode.Nodes.Add("fool");
        }

        public static ContextMenuStrip EmptyContextMenuStrip { get; set; } = new();
    }
}
#endif
