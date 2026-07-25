using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDdl;
using JustyBase.NetezzaDriver;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace AppBase.Data;

public static class NetezzaHelpers
{

    public const string procExample = NetezzaDdlTemplates.CreateProcedurePattern;

    public const string DATABASES = NetezzaCatalogSql.DatabasesSql;

    public const string COST = NetezzaCatalogSql.CostSql;

    public const string SESSION = NetezzaSystemSql.CurrentSessionIdSql;

    public const string VIEW_CODE = NetezzaCatalogSql.ViewDefinitionByObjectIdSql;

    public const string CurrentDataSql = NetezzaCatalogSql.DataAktSql;

    public const string TABLE_KEYS_NZ_SQL = NetezzaCatalogSql.LegacyTableKeysSql;

    public const string SEARCH_VIEW_SQL = NetezzaSystemSql.SearchViewsTemplate;

    public const string SEARCH_PROCEDURE_SQL = NetezzaSystemSql.SearchProceduresTemplate;

    public static readonly string USER_GROUPS = NetezzaSystemSql.UserGroupsSql;

    public static string ServerVersion = "";
    public static bool SqliteInProgress = false;
    static string msg = "";

    public static string DatabaseTablesSql(string dbName, bool ownerMode = true, bool noDescMode = false)
        => NetezzaCatalogSql.GetLegacyBazyTabeleSql(dbName, ownerMode, noDescMode);

    public static string GetDescSql(string dbName)
        => NetezzaCatalogSql.GetDescSql(dbName);

    public static string NzProcReturnFix(string procReturns)
        => NetezzaProcTypes.FixProcedureReturnType(procReturns);

    public static string OBJECT_COLUMNS_NZ_SQL_OF_DB(string dbName)
        => NetezzaCatalogSql.GetLegacyObjectColumnsSql(dbName);

    public static string OneTableSqlOwner(string tablename)
        => NetezzaCatalogSql.GetLegacyOneTableSqlOwner(tablename);

    public static string OneTableSqlSchema(string tablename, bool schemaOn)
        => NetezzaCatalogSql.GetLegacyOneTableSqlSchema(tablename, schemaOn);

    public static void OnSchemaProblemNetezzaAskForRestart(AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers, ILogger logger, string connectionName, Action action)
    {
        baseWindowHelpers.Config.ResetSchema = true;

        if (logger.OnSchemaProblemMessage(connectionName) == true)
        {
            action?.Invoke();
        }
    }


    public static bool SchemasOn(DbConnection conn)
    {
        bool res = false;
        string sql = "SHOW ENABLE_SCHEMA_DBO_CHECK";

        if (conn is NzConnection nETConnection)
        {
            nETConnection.NoticeReceived += NETConnection_Notice;

            try
            {
                using (NzCommand tempXmd = new NzCommand(sql, nETConnection))
                {
                    var obj = tempXmd.ExecuteNonQuery();
                    res = !(msg.Trim()[^1..] == "0");
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Netezza schema capability check failed: {exception.GetType().Name}");
            }
            finally
            {
                nETConnection.NoticeReceived -= NETConnection_Notice;
            }
        }
        else
        {
            throw new Exception("SchemasOn - wrong driver");
        }


        return res;
    }

    public static string SearchInNetezzaSchema(string dbName, string txtToSearch)
        => NetezzaCatalogSql.GetLegacySearchInSchemaSql(dbName, txtToSearch);

    public static string ExternalSql(string database)
        => NetezzaCatalogSql.GetLegacyExternalSql(database);

    public static string GetFulidesSql(string databaseName, int databaseId)
        => NetezzaCatalogSql.GetLegacyFulidesSql(databaseName, databaseId);
    private static void NETConnection_Notice(object o, NzNoticeEventArgs message)
    {
        msg = message.Message;
    }

    public static string ProcSql(string database)
        => NetezzaCatalogSql.GetLegacyProcSql(database);

    public static string SynonymSql(string database)
        => NetezzaCatalogSql.GetLegacySynonymSql(database);

    public static string ViewSql(string database)
        => NetezzaCatalogSql.GetLegacyViewSql(database);

    public static bool InitializeConnectionSchemaData(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        IConnectionSessionRegistry connectionSessions,
        INetezzaSchemaTableCatalog schemaTables,
        string preferedUserName,
        string connectionName)
    {
        ArgumentNullException.ThrowIfNull(connectionSessions);
        ArgumentNullException.ThrowIfNull(schemaTables);

        if (!connectionSessions.TryGetValue(connectionName, out var gdb)
            || gdb is not INetezza nz
            || !nz.BasesTablesList.TryGetValue(connectionName, out var basesTabels))
        {
            return false;
        }

        IOrderedEnumerable<NetezzaBasesTables> orderedBaseTables = null;

        preferedUserName ??= Random.Shared.GetString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 32);

        if (baseWindowHelpers.Config.DontShowOwner)
        {
            orderedBaseTables = basesTabels.OrderBy(a => a.DATABASE_ID).
                ThenBy(a => a.TABLE_NAME).
                ThenBy(a => a.OWNER_NAME);
        }
        else if (baseWindowHelpers.Config.SortMethod == 0)
        {
            string userName = preferedUserName.ToLower();
            orderedBaseTables = basesTabels.
                OrderBy(a => a.DATABASE_ID).
                ThenBy(a => !a.OWNER_NAME.Equals(userName, StringComparison.OrdinalIgnoreCase)).
                ThenBy(a => a.OWNER_NAME).ThenBy(a => a.TABLE_NAME);
        }
        else if (baseWindowHelpers.Config.SortMethod == 1)
        {
            string userName = preferedUserName.ToLower();
            orderedBaseTables = basesTabels.
                OrderBy(a => a.DATABASE_ID).
                ThenBy(a => a.OWNER_NAME).
                ThenBy(a => a.TABLE_NAME);
        }
        else
        {
            orderedBaseTables = basesTabels.
                OrderBy(a => a.DATABASE_ID).
                ThenBy(a => a.TABLE_NAME).
                ThenBy(a => a.OWNER_NAME);
        }
        var tablesByConnection = schemaTables.TablesByConnection;
        tablesByConnection[connectionName] = new();
        var currentDatabaseTables = tablesByConnection[connectionName];

        foreach (var row in orderedBaseTables)
        {
            int tableId = row.TABLE_ID;
            int databaseId = row.DATABASE_ID;
            string tableName = row.TABLE_NAME;
            string tableOwner = row.OWNER_NAME;
            string tableSchema = row.SCHEMA_NAME;
            string tableObjectOwner = row.OBJECT_OWNER_NAME;
            var tableKind = row.OBJECT_TYPE switch
            {
                "TABLE" => TypeInDatabase.table,
                "VIEW" => TypeInDatabase.view,
                "PROCEDURE" => TypeInDatabase.procedure,
                "FUNCTION" => TypeInDatabase.function,
                "SEQUENCE" => TypeInDatabase.sequence,
                "SYNONYM" => TypeInDatabase.synonym,
                "EXTERNAL TABLE" => TypeInDatabase.thisExternal,
                "AGGREGATE" => TypeInDatabase.thisAggregate,
                _ => TypeInDatabase.table,
            };

            if (baseWindowHelpers.BaseTableConnections.TryGetValue(connectionName, out var value) && value.TryGetValue(databaseId, out var dbTablesSet))
            {
                string databaseName = baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var dbDict0)
                    && dbDict0.TryGetValue(databaseId, out var dbInfo0)
                    ? dbInfo0.DatabaseName
                    : string.Empty;

                string tableDesc = null;
                if (baseWindowHelpers.DatabaseTableDescriptions.TryGetValue(connectionName, out var res0))
                {
                    if (res0.TryGetValue(databaseName, out var res1))
                    {
                        res1.TryGetValue(tableId, out tableDesc);
                    }
                }

                currentDatabaseTables[tableId] = new NetezzaTableInfo()
                {
                    DATABASE_ID = databaseId,
                    TABLE_NAME = tableName,
                    TABLE_DESC = tableDesc,
                    TABLE_OWNER = tableOwner,
                    TABLE_SCHEMA = tableSchema,
                    TABLE_OBJECT_OWNER = tableObjectOwner,
                    TABLE_KIND = tableKind,
                    FIRST_COLUMN_ID = -1,
                    COLUMN_COUNT = 0
                };
                dbTablesSet.Add(tableId);
            }
        }
        orderedBaseTables = null;

        int columnId = 0;

        List<NetezzaColumnInfoRow> tableColumns = nz.ColumnList;
        foreach (var row in nz.ColumnList)
        {
            if (currentDatabaseTables.TryGetValue(row.TABLE_ID, out var thisValue))
            {
                if (thisValue.FIRST_COLUMN_ID == -1)
                {
                    thisValue.FIRST_COLUMN_ID = columnId;
                }
                thisValue.COLUMN_COUNT++;
                columnId++;
            }
        }

        baseWindowHelpers.ColumnTablesDictionary[connectionName] = tableColumns;

        baseWindowHelpers.DatabaseSchemaLookup[string.Intern(connectionName)] = new Dictionary<string, Dictionary<string, (string, int)>>(StringComparer.OrdinalIgnoreCase);
        if (baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var dbDict1))
        {
            foreach (var database in dbDict1)
            {
                baseWindowHelpers.DatabaseSchemaLookup[connectionName][database.Value.DatabaseName] = new Dictionary<string, (string owner, int tableId)>(StringComparer.OrdinalIgnoreCase);
                if (baseWindowHelpers.BaseTableConnections.TryGetValue(connectionName, out var btConn)
                    && btConn.TryGetValue(database.Key, out var dbTablesSet1))
                {
                    foreach (int baseTable in dbTablesSet1)
                    {
                        if (tablesByConnection.TryGetValue(connectionName, out var btDict)
                            && btDict.TryGetValue(baseTable, out var table))
                        {
                            baseWindowHelpers.DatabaseSchemaLookup[connectionName][database.Value.DatabaseName][table.TABLE_NAME] = (table.TABLE_OWNER, baseTable);
                        }
                    }
                }
            }
        }

        baseWindowHelpers.DatabaseOwners[connectionName] = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        if (baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var dbDict2))
        {
            foreach (var item in dbDict2)
            {
                if (baseWindowHelpers.DatabaseSchemaLookup.TryGetValue(connectionName, out var schemaLookup)
                    && schemaLookup.TryGetValue(item.Value.DatabaseName, out var schemaEntry))
                {
                    var ownersDictionary = schemaEntry.Select(arg => arg.Value.owner).Distinct().ToDictionary(x => x, x => x);
                    baseWindowHelpers.DatabaseOwners[connectionName][item.Value.DatabaseName] = ownersDictionary;
                }
            }
        }
        return true;
    }

}
