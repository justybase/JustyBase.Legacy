using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Common.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaSqlParser.Caching;
using JustyBase.NetezzaSqlParser.Completion;
using JustyBase.NetezzaSqlParser.Dialects;
using JustyBase.NetezzaSqlParser.Ast;
using JustyBase.NetezzaSqlParser.Visitor;
using System.Data;
using System.Data.Common;

namespace AppBase.Data.Completion;

/// <summary>
/// Shared SQL completion infrastructure (schema, parse cache) for all SQL editor tabs.
/// Each editor tab gets its own <see cref="NzCompletionEngine"/> with a unique document URI.
/// </summary>
public sealed class NetezzaSqlCompletionServices
{
    private readonly INetezzaSchemaTableCatalog _schemaTables;
    private string _schemaSyncedForConnection;

    public InMemorySchemaProvider SchemaProvider { get; } = new();
    public NetezzaSchemaSnapshot MetadataSnapshot { get; private set; } = NetezzaSchemaSnapshot.Empty;
    public DocumentParsingCoordinator ParsingCoordinator { get; } = new();
    public event Action? SchemaInvalidated;

    public NetezzaSqlCompletionServices(INetezzaSchemaTableCatalog schemaTables)
    {
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
    }

    public NzCompletionEngine CreateEngine(string documentUri, SqlDialect dialect = SqlDialect.Netezza)
    {
        var engine = new NzCompletionEngine(
            SchemaProvider,
            ParsingCoordinator,
            DialectRuntime.AuthoringCatalog(dialect),
            dialect);
        engine.SetDocumentUri(documentUri);
        return engine;
    }

    public void EnsureSchemaForConnection(INetezzaCompletionContext completionContext, string connectionName)
    {
        if (completionContext is null || string.IsNullOrEmpty(connectionName))
            return;

        if (string.Equals(_schemaSyncedForConnection, connectionName, StringComparison.OrdinalIgnoreCase)
            && SchemaProvider.HasTables())
            return;

        MetadataSnapshot = LegacySchemaSync.SyncConnection(SchemaProvider, completionContext, _schemaTables, connectionName);
        _schemaSyncedForConnection = connectionName;
    }

    /// <summary>
    /// Projects the already-loaded DB2 catalog into the same parser provider
    /// used by Netezza. Columns are part of the provider snapshot so the shared
    /// CompletionAliasResolver can resolve A. for both schema.table and
    /// database.schema.table references without depending on a host workaround.
    /// </summary>
    public void EnsureDb2Schema(IGeneralDb database, string connectionName, string databaseName)
    {
        if (database is null || string.IsNullOrWhiteSpace(connectionName))
            return;

        string syncKey = $"DB2:{connectionName}:{databaseName}";
        if (string.Equals(_schemaSyncedForConnection, syncKey, StringComparison.OrdinalIgnoreCase)
            && SchemaProvider.HasTables())
            return;

        SchemaProvider.Clear();
        foreach ((string schema, Dictionary<string, TypeInDatabase> objects) in database.objectInSchema)
        {
            foreach ((string name, TypeInDatabase kind) in objects)
            {
                if (kind is not (TypeInDatabase.table or TypeInDatabase.view
                    or TypeInDatabase.synonym or TypeInDatabase.db2alias or TypeInDatabase.db2nickname))
                    continue;

                ColumnInfo[] columns;
                try
                {
                    columns = database.GetColumns(databaseName, schema, name)
                        .Where(column => !string.IsNullOrWhiteSpace(column))
                        .Select(column => new ColumnInfo(column))
                        .ToArray();
                }
                catch
                {
                    // Keep the object visible when one object cannot expose
                    // columns. The live fallback can still serve that object.
                    columns = [];
                }

                SchemaProvider.AddTable(new TableInfo(
                    name,
                    schema,
                    databaseName,
                    Columns: columns,
                    IsView: kind == TypeInDatabase.view));
            }
        }

        SchemaProvider.BumpMetadataEpoch();
        MetadataSnapshot = NetezzaSchemaSnapshot.Empty;
        _schemaSyncedForConnection = syncKey;
    }

    /// <summary>
    /// Projects the SQL Server catalog already loaded by <see cref="IGeneralDb"/>
    /// (INFORMATION_SCHEMA rows captured during InitDb) into the shared parser
    /// schema provider, mirroring <see cref="EnsureDb2Schema"/>. Columns are
    /// fetched in one batch query per database instead of one connection per
    /// catalog object.
    /// </summary>
    public void EnsureMsSqlSchema(IGeneralDb database, string connectionName, string databaseName)
    {
        if (database is not GeneralDb generalDb || string.IsNullOrWhiteSpace(connectionName))
            return;

        string syncKey = $"MSSQL:{connectionName}:{databaseName}";
        if (string.Equals(_schemaSyncedForConnection, syncKey, StringComparison.OrdinalIgnoreCase))
            return;

        var objects = new List<(string Db, string Schema, string Name, bool IsView)>();
        foreach (DataTable catalog in new[] { generalDb.tables, generalDb.views })
        {
            if (catalog is null || catalog.Rows.Count == 0)
                continue;

            foreach (DataRow row in catalog.Rows)
            {
                string db = row.ItemArray.Length > 0 ? row.ItemArray[0]?.ToString() ?? string.Empty : string.Empty;
                string schema = row.ItemArray.Length > 1 ? row.ItemArray[1]?.ToString() ?? string.Empty : string.Empty;
                string name = row.ItemArray.Length > 2 ? row.ItemArray[2]?.ToString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(name))
                    continue;

                if (!string.IsNullOrWhiteSpace(databaseName)
                    && !string.Equals(db, databaseName, StringComparison.OrdinalIgnoreCase))
                    continue;

                objects.Add((db, schema, name, ReferenceEquals(catalog, generalDb.views)));
            }
        }

        Dictionary<(string Db, string Schema, string Name), List<string>> columnsByObject =
            FetchMsSqlColumnsBatch(generalDb, objects);

        SchemaProvider.Clear();
        foreach ((string db, string schema, string name, bool isView) in objects)
        {
            ColumnInfo[] columns;
            if (columnsByObject.TryGetValue((db, schema, name), out var batchColumns))
            {
                columns = batchColumns
                    .Where(column => !string.IsNullOrWhiteSpace(column))
                    .Select(column => new ColumnInfo(column))
                    .ToArray();
            }
            else
            {
                try
                {
                    // GetColumns returns the ["fool"] placeholder on failure;
                    // drop it so no phantom column reaches completion.
                    columns = generalDb.GetColumns(db, schema, name)
                        .Where(column => !string.IsNullOrWhiteSpace(column)
                            && !string.Equals(column, "fool", StringComparison.OrdinalIgnoreCase))
                        .Select(column => new ColumnInfo(column))
                        .ToArray();
                }
                catch
                {
                    // Keep the object visible when one object cannot expose
                    // columns. The live fallback can still serve that object.
                    columns = [];
                }
            }

            SchemaProvider.AddTable(new TableInfo(
                name,
                schema,
                db,
                Columns: columns,
                IsView: isView));
        }

        SchemaProvider.BumpMetadataEpoch();
        MetadataSnapshot = NetezzaSchemaSnapshot.Empty;
        _schemaSyncedForConnection = syncKey;
    }

    /// <summary>
    /// Projects the PostgreSQL catalog already loaded by <see cref="IGeneralDb"/>
    /// (pg_class/information_schema rows captured during InitDb) into the shared
    /// parser schema provider. PostgreSQL is single-database per connection, so
    /// rows carry no catalog column; all objects belong to the default database.
    /// Columns are fetched in one batch query.
    /// </summary>
    public void EnsurePostgresSchema(IGeneralDb database, string connectionName)
    {
        if (database is not GeneralDb generalDb || string.IsNullOrWhiteSpace(connectionName))
            return;

        string db = generalDb.DefaultDatabaseName;
        string syncKey = $"POSTGRES:{connectionName}:{db}";
        if (string.Equals(_schemaSyncedForConnection, syncKey, StringComparison.OrdinalIgnoreCase))
            return;

        var objects = new List<(string Schema, string Name, bool IsView)>();
        foreach (DataTable? catalog in new[] { generalDb.tables, generalDb.views })
        {
            if (catalog is null || catalog.Rows.Count == 0)
                continue;

            foreach (DataRow row in catalog.Rows)
            {
                string schema = row.ItemArray.Length > 0 ? row.ItemArray[0]?.ToString() ?? string.Empty : string.Empty;
                string name = row.ItemArray.Length > 1 ? row.ItemArray[1]?.ToString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(name))
                    continue;

                objects.Add((schema, name, ReferenceEquals(catalog, generalDb.views)));
            }
        }

        Dictionary<(string Schema, string Name), List<string>> columnsByObject =
            FetchPostgresColumnsBatch(generalDb);

        SchemaProvider.Clear();
        foreach ((string schema, string name, bool isView) in objects)
        {
            ColumnInfo[] columns;
            if (columnsByObject.TryGetValue((schema, name), out var batchColumns))
            {
                columns = batchColumns
                    .Where(column => !string.IsNullOrWhiteSpace(column))
                    .Select(column => new ColumnInfo(column))
                    .ToArray();
            }
            else
            {
                try
                {
                    // GetColumns returns the ["fool"] placeholder on failure;
                    // drop it so no phantom column reaches completion.
                    columns = generalDb.GetColumns(db, schema, name)
                        .Where(column => !string.IsNullOrWhiteSpace(column)
                            && !string.Equals(column, "fool", StringComparison.OrdinalIgnoreCase))
                        .Select(column => new ColumnInfo(column))
                        .ToArray();
                }
                catch
                {
                    // Keep the object visible when one object cannot expose
                    // columns. The live fallback can still serve that object.
                    columns = [];
                }
            }

            SchemaProvider.AddTable(new TableInfo(
                name,
                schema,
                db,
                Columns: columns,
                IsView: isView));
        }

        SchemaProvider.BumpMetadataEpoch();
        MetadataSnapshot = NetezzaSchemaSnapshot.Empty;
        _schemaSyncedForConnection = syncKey;
    }

    private sealed class PostgresObjectKeyComparer : IEqualityComparer<(string Schema, string Name)>
    {
        public static PostgresObjectKeyComparer OrdinalIgnoreCase { get; } = new();

        public bool Equals((string Schema, string Name) x, (string Schema, string Name) y) =>
            string.Equals(x.Schema, y.Schema, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Schema, string Name) obj) =>
            HashCode.Combine(obj.Schema.ToUpperInvariant(), obj.Name.ToUpperInvariant());
    }

    private static Dictionary<(string Schema, string Name), List<string>> FetchPostgresColumnsBatch(
        IGeneralDb database)
    {
        var columnsByObject = new Dictionary<(string, string), List<string>>(PostgresObjectKeyComparer.OrdinalIgnoreCase);
        try
        {
            using var conn = database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 30;
            cmd.CommandText =
                "SELECT table_schema, table_name, column_name FROM information_schema.columns" +
                " WHERE table_schema NOT IN ('information_schema', 'pg_catalog')";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                string schema = rdr.GetString(0);
                string name = rdr.GetString(1);
                string column = rdr.GetString(2);
                if (!columnsByObject.TryGetValue((schema, name), out var list))
                {
                    list = [];
                    columnsByObject[(schema, name)] = list;
                }

                list.Add(column);
            }
        }
        catch
        {
            // Keep the objects visible without columns; the per-object
            // fallback in the caller still covers the failed batch.
        }

        return columnsByObject;
    }

    private sealed class ObjectKeyComparer : IEqualityComparer<(string Db, string Schema, string Name)>
    {
        public static ObjectKeyComparer OrdinalIgnoreCase { get; } = new();

        public bool Equals((string Db, string Schema, string Name) x, (string Db, string Schema, string Name) y) =>
            string.Equals(x.Db, y.Db, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Schema, y.Schema, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Db, string Schema, string Name) obj) =>
            HashCode.Combine(
                obj.Db.ToUpperInvariant(),
                obj.Schema.ToUpperInvariant(),
                obj.Name.ToUpperInvariant());
    }

    private static Dictionary<(string Db, string Schema, string Name), List<string>> FetchMsSqlColumnsBatch(
        IGeneralDb database,
        List<(string Db, string Schema, string Name, bool IsView)> objects)
    {
        var columnsByObject = new Dictionary<(string, string, string), List<string>>(ObjectKeyComparer.OrdinalIgnoreCase);
        foreach (string db in objects.Select(objectItem => objectItem.Db).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var conn = database.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 30;
                // Brackets keep database names with spaces/hyphens valid; the
                // escaping mirrors the VSCode extension catalog prefix builder.
                cmd.CommandText =
                    $"SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM [{db.Replace("]", "]]")}].INFORMATION_SCHEMA.COLUMNS";
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string catalog = rdr.GetString(0);
                    string schema = rdr.GetString(1);
                    string name = rdr.GetString(2);
                    string column = rdr.GetString(3);
                    if (!columnsByObject.TryGetValue((catalog, schema, name), out var list))
                    {
                        list = [];
                        columnsByObject[(catalog, schema, name)] = list;
                    }

                    list.Add(column);
                }
            }
            catch
            {
                // Keep the objects visible without columns; the per-object
                // fallback in the caller still covers the missed database.
            }
        }

        return columnsByObject;
    }

    public void InvalidateSchema()
    {
        _schemaSyncedForConnection = null;
        SchemaProvider.Clear();
        MetadataSnapshot = NetezzaSchemaSnapshot.Empty;
        SchemaProvider.BumpMetadataEpoch();
        SchemaInvalidated?.Invoke();
    }
}
