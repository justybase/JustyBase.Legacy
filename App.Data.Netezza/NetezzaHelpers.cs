using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
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

    public const string SEARCH_VIEW_SQL = NetezzaSystemSql.SearchViewsTemplate;

    public const string SEARCH_PROCEDURE_SQL = NetezzaSystemSql.SearchProceduresTemplate;

    public static readonly string USER_GROUPS = NetezzaSystemSql.UserGroupsSql;

    static string msg = "";

    public static string GetDescSql(string dbName)
        => NetezzaCatalogSql.GetDescSql(dbName);

    public static string KeysSql(string database)
        => NetezzaCatalogSql.GetLegacyKeysSql(database);

    public static string DistributionColumnsSql(string database)
        => NetezzaCatalogSql.GetLegacyDistributionColumnsSql(database);

    public static string NzProcReturnFix(string procReturns)
        => NetezzaProcTypes.FixProcedureReturnType(procReturns);

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

    public static string ExternalSql(string database)
        => NetezzaCatalogSql.GetLegacyExternalSql(database);

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
        string? preferedUserName,
        string connectionName,
        JustyBase.Netezza.Schema.NetezzaSchemaCache? schemaCache = null)
    {
        ArgumentNullException.ThrowIfNull(connectionSessions);
        ArgumentNullException.ThrowIfNull(schemaTables);

        if (!connectionSessions.TryGetValue(connectionName, out var gdb)
            || gdb is not INetezza nz
            || nz.GetConnection() is not { } connection)
        {
            return false;
        }

        using (connection)
        {
            return InitializeConnectionSchemaDataCore(
                baseWindowHelpers,
                nz,
                connection,
                schemaTables,
                preferedUserName,
                connectionName,
                schemaCache);
        }
    }

    private static bool InitializeConnectionSchemaDataCore(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        INetezza nz,
        DbConnection connection,
        INetezzaSchemaTableCatalog schemaTables,
        string? preferedUserName,
        string connectionName,
        JustyBase.Netezza.Schema.NetezzaSchemaCache? schemaCache)
    {
        IDatabaseRuntimeCatalogWriter runtimeWriter = baseWindowHelpers as IDatabaseRuntimeCatalogWriter
            ?? throw new InvalidOperationException("Schema initialization requires the catalog write port.");
        INetezzaSchemaTableCatalogWriter catalogWriter = schemaTables as INetezzaSchemaTableCatalogWriter
            ?? throw new InvalidOperationException("Schema initialization requires the table catalog write port.");

        IReadOnlyList<(string Database, JustyBase.Netezza.Models.NetezzaSchemaSnapshot Snapshot)> snapshots =
            JustyBase.Netezza.Schema.NetezzaSchemaLoader.LoadAllAsync(
                    connection,
                    new JustyBase.Netezza.Schema.NetezzaCatalogLoadOptions
                    {
                        LazyColumnThreshold = int.MaxValue,
                        LoadProcedures = false,
                    })
                .GetAwaiter()
                .GetResult();

        // OBJIDs are unique per database, but the legacy catalog dictionaries are
        // connection-scoped and keyed by OBJID only. Loading every database into one
        // dictionary silently corrupted colliding entries (last database won and the
        // tree/DDL/autocomplete resolved the wrong table). Never overwrite an entry:
        // the default database is processed first and wins, so the primary connection
        // surface stays fully correct; colliding objects in other databases are simply
        // absent instead of pointing at the wrong table.
        string? defaultDatabase = (nz as IGeneralDb)?.DefaultDatabaseName;
        if (string.IsNullOrWhiteSpace(defaultDatabase))
        {
            defaultDatabase = connection.Database;
        }
        if (string.IsNullOrWhiteSpace(defaultDatabase))
        {
            defaultDatabase = null;
        }
        (string Database, JustyBase.Netezza.Models.NetezzaSchemaSnapshot Snapshot)[] orderedSnapshots = snapshots
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Database))
            .OrderByDescending(pair => string.Equals(pair.Database, defaultDatabase, StringComparison.OrdinalIgnoreCase))
            .ThenBy(pair => pair.Database, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (orderedSnapshots.Length == 0)
        {
            // No usable catalog (failed database-list query). Keep the previously
            // populated dictionaries instead of wiping the whole schema silently.
            return false;
        }

        var currentDatabaseTables = new Dictionary<int, NetezzaTableInfo>();
        var columnRows = new List<NetezzaColumnInfoRow>();
        var schemaLookup = new Dictionary<string, Dictionary<string, (string owner, int tableId)>>(StringComparer.OrdinalIgnoreCase);
        var databaseIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (databaseName, snapshot) in orderedSnapshots)
        {
            schemaCache?.Put(connectionName, databaseName, snapshot);

            int databaseId = -1;
            if (baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var dbDict0))
            {
                foreach (var (id, info) in dbDict0)
                {
                    if (string.Equals(info.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        databaseId = id;
                        break;
                    }
                }
            }

            if (databaseId < 0)
            {
                continue;
            }

            databaseIdByName[databaseName] = databaseId;

            var tableLookup = new Dictionary<string, (string owner, int tableId)>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in OrderTables(snapshot.Tables, baseWindowHelpers.Config, preferedUserName))
            {
                int tableId = table.CatalogId;
                if (currentDatabaseTables.ContainsKey(tableId))
                {
                    // OBJID collision with a database processed earlier (default wins).
                    continue;
                }

                var tableKind = table.TextType switch
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

                // The modern catalog SQL returns the full signature for procedures and
                // functions (e.g. GET_P1(INTEGER, VARCHAR(20))); the legacy tree and
                // completion use plain callable names.
                string tableName = table.TextType is "PROCEDURE" or "FUNCTION"
                    ? StripSignature(table.Name)
                    : table.Name;

                currentDatabaseTables[tableId] = new NetezzaTableInfo()
                {
                    DATABASE_ID = databaseId,
                    TABLE_NAME = tableName,
                    TABLE_DESC = table.Description ?? string.Empty,
                    TABLE_OWNER = table.Owner ?? string.Empty,
                    TABLE_SCHEMA = table.Schema ?? string.Empty,
                    TABLE_OBJECT_OWNER = table.Owner ?? string.Empty,
                    TABLE_KIND = tableKind,
                    FIRST_COLUMN_ID = -1,
                    COLUMN_COUNT = 0
                };
                runtimeWriter.AddBaseTable(connectionName, databaseId, tableId);
                tableLookup[tableName] = (table.Owner ?? string.Empty, tableId);
            }

            schemaLookup[databaseName] = tableLookup;
        }

        int columnId = 0;
        var distOrgByKey = LoadDistributionSequences(connection, orderedSnapshots, databaseIdByName);

        foreach (var (databaseName, snapshot) in orderedSnapshots)
        {
            if (!databaseIdByName.TryGetValue(databaseName, out int databaseId))
            {
                continue;
            }

            foreach (var table in snapshot.Tables.OrderBy(t => t.CatalogId))
            {
                // Only the winning (first) entry for an OBJID receives columns — a
                // colliding table in another database must not inherit the winner's
                // columns, nor write rows keyed by an id that resolves to another table.
                if (!currentDatabaseTables.TryGetValue(table.CatalogId, out var tableInfo)
                    || tableInfo.DATABASE_ID != databaseId
                    || table.Columns is not { Count: > 0 } columns)
                {
                    continue;
                }

                tableInfo.FIRST_COLUMN_ID = columnId;
                tableInfo.COLUMN_COUNT = columns.Count;

                foreach (var column in columns)
                {
                    var distOrg = distOrgByKey.TryGetValue((databaseId, table.CatalogId, column.Name), out var seqs)
                        ? seqs
                        : (Dist: (sbyte?)null, Org: (sbyte?)null);

                    columnRows.Add(new NetezzaColumnInfoRow()
                    {
                        COLUMN_NUMBER = (ushort)(columnRows.Count + 1),
                        TABLE_ID = table.CatalogId,
                        DATABASE_ID = databaseId,
                        COLUMN_NAME = column.Name,
                        COLUMN_DESCRIPTION = column.Description,
                        DATA_TYPE = column.DataType ?? string.Empty,
                        IS_NULLABLE = column.Nullable,
                        DISTSEQNO = distOrg.Dist,
                        ORGSEQNO = distOrg.Org,
                        COLDEFAULT = column.DefaultValue,
                    });
                }

                columnId += columns.Count;
            }
        }

        catalogWriter.ReplaceConnection(connectionName, currentDatabaseTables);

        runtimeWriter.SetColumnTable(connectionName, columnRows);

        runtimeWriter.SetSchemaLookup(connectionName, schemaLookup);

        var owners = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (databaseName, tableLookup) in schemaLookup)
        {
            var ownersDictionary = tableLookup.Values.Select(arg => arg.owner).Distinct().ToDictionary(x => x, x => x);
            owners[databaseName] = ownersDictionary;
        }
        runtimeWriter.SetOwners(connectionName, owners);
        return true;
    }

    /// <summary>
    /// Restores the legacy SortMethod / DontShowOwner ordering that the modern catalog
    /// SQL no longer provides (ORDER BY SCHEMA, OBJTYPE, OBJNAME).
    /// </summary>
    private static IEnumerable<JustyBase.Netezza.Models.NetezzaSchemaTable> OrderTables(
        IEnumerable<JustyBase.Netezza.Models.NetezzaSchemaTable> tables,
        AppBase.Common.Configuration.IApplicationConfig config,
        string? preferedUserName)
    {
        string userName = (preferedUserName ?? string.Empty).ToLower();
        return config switch
        {
            { DontShowOwner: true } => tables.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Owner, StringComparer.OrdinalIgnoreCase),
            { SortMethod: 0 } => tables.OrderBy(t => !string.Equals(t.Owner, userName, StringComparison.OrdinalIgnoreCase))
                .ThenBy(t => t.Owner, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase),
            { SortMethod: 1 } => tables.OrderBy(t => t.Owner, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase),
            _ => tables.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Owner, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string StripSignature(string name)
    {
        int paren = name.IndexOf('(');
        return paren > 0 ? name[..paren].Trim() : name;
    }

    /// <summary>
    /// Loads DISTSEQNO/ORGSEQNO for every column of the given databases (modern shared SQL)
    /// so Legacy DDL generation can emit DISTRIBUTE ON / ORGANIZE ON clauses. Keyed by
    /// (database, table, column) because OBJIDs are only unique within a database.
    /// </summary>
    private static Dictionary<(int DatabaseId, int TableId, string ColumnName), (sbyte? Dist, sbyte? Org)> LoadDistributionSequences(
        DbConnection connection,
        IReadOnlyList<(string Database, JustyBase.Netezza.Models.NetezzaSchemaSnapshot Snapshot)> snapshots,
        IReadOnlyDictionary<string, int> databaseIdByName)
    {
        var result = new Dictionary<(int, int, string), (sbyte?, sbyte?)>();

        foreach (var (databaseName, _) in snapshots)
        {
            if (!databaseIdByName.TryGetValue(databaseName, out int databaseId))
            {
                continue;
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = JustyBase.NetezzaCatalogSql.NetezzaCatalogSql.GetLegacyDistributionColumnsSql(databaseName);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int tableId = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                    string columnName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    if (tableId == 0 || string.IsNullOrEmpty(columnName))
                    {
                        continue;
                    }

                    sbyte? dist = reader.IsDBNull(2) ? null : Convert.ToSByte(reader.GetValue(2));
                    sbyte? org = reader.IsDBNull(3) ? null : Convert.ToSByte(reader.GetValue(3));
                    result[(databaseId, tableId, columnName)] = (dist, org);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLine($"Distribution sequence load failed for {databaseName}: {exception.GetType().Name}");
            }
        }

        return result;
    }

}
