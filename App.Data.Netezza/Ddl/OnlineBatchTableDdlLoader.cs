using AppBase.Data.Core.Core;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDdl;
using JustyBase.NetezzaDdl.Models;
using System.Data.Common;

namespace AppBase.Data.Ddl;

/// <summary>
/// Lite-style bulk table DDL for a whole database (or optional schema): few queries, then local assembly.
/// </summary>
public static class OnlineBatchTableDdlLoader
{
    public static async Task<string> LoadTablesDdlAsync(
        IGeneralDb database,
        string databaseName,
        string? schemaFilter = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NetezzaTableDdlInput> tables = await LoadTableInputsAsync(
            database, databaseName, schemaFilter, cancellationToken).ConfigureAwait(false);
        return new NetezzaBatchDdlBuilder().Build(new NetezzaBatchDdlInput(Tables: tables));
    }

    public static async Task<IReadOnlyList<NetezzaTableDdlInput>> LoadTableInputsAsync(
        IGeneralDb database,
        string databaseName,
        string? schemaFilter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using DbConnection connection = database.GetConnection(databaseName);
            connection.Open();

            var columnsByTable = ReadColumnsByTable(connection, databaseName, schemaFilter);
            var distribution = ReadNameListsByTable(
                connection,
                NetezzaCatalogSql.GetDistributeSql(databaseName),
                schemaIndex: 0,
                tableIndex: 1,
                nameIndex: 3,
                schemaFilter);
            var organize = ReadNameListsByTable(
                connection,
                NetezzaCatalogSql.GetOrganizeSql(databaseName),
                schemaIndex: 0,
                tableIndex: 1,
                nameIndex: 3,
                schemaFilter);
            var keysByTable = ReadKeysByTable(connection, databaseName, schemaFilter);
            var comments = ReadTableComments(connection, databaseName, schemaFilter);

            return BuildTableInputs(
                databaseName,
                columnsByTable,
                distribution,
                organize,
                keysByTable,
                comments);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds table DDL inputs from already-fetched bulk maps (unit-test friendly).</summary>
    public static IReadOnlyList<NetezzaTableDdlInput> BuildTableInputs(
        string databaseName,
        IReadOnlyDictionary<string, List<NetezzaSchemaColumn>> columnsByTable,
        IReadOnlyDictionary<string, List<string>>? distributionByTable = null,
        IReadOnlyDictionary<string, List<string>>? organizeByTable = null,
        IReadOnlyDictionary<string, IReadOnlyList<NetezzaKeyDdl>>? keysByTable = null,
        IReadOnlyDictionary<string, string>? commentsByTable = null)
    {
        var result = new List<NetezzaTableDdlInput>(columnsByTable.Count);
        foreach ((string key, List<NetezzaSchemaColumn> columns) in columnsByTable.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            int dot = key.IndexOf('.');
            string schema = dot < 0 ? string.Empty : key[..dot];
            string tableName = dot < 0 ? key : key[(dot + 1)..];
            string? comment = null;
            commentsByTable?.TryGetValue(key, out comment);

            List<string>? dist = null;
            distributionByTable?.TryGetValue(key, out dist);

            List<string>? org = null;
            organizeByTable?.TryGetValue(key, out org);

            IReadOnlyList<NetezzaKeyDdl>? keys = null;
            keysByTable?.TryGetValue(key, out keys);

            result.Add(OnlineTableDdlLoader.BuildInput(
                databaseName,
                schema,
                tableName,
                columns,
                dist,
                org,
                keys,
                comment));
        }

        return result;
    }

    private static Dictionary<string, List<NetezzaSchemaColumn>> ReadColumnsByTable(
        DbConnection connection,
        string databaseName,
        string? schemaFilter)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = NetezzaCatalogSql.GetBatchColumnsSql(databaseName, schemaFilter);
        using DbDataReader reader = command.ExecuteReader();
        var map = new Dictionary<string, List<NetezzaSchemaColumn>>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            string schema = reader.IsDBNull(0) ? string.Empty : reader.GetValue(0)?.ToString() ?? string.Empty;
            string tableName = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty;
            string objType = reader.IsDBNull(2) ? string.Empty : reader.GetValue(2)?.ToString() ?? string.Empty;
            if (!objType.Equals("TABLE", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(tableName))
                continue;

            string key = $"{schema}.{tableName}";
            if (!map.TryGetValue(key, out var columns))
            {
                columns = [];
                map[key] = columns;
            }

            string name = reader.IsDBNull(3) ? string.Empty : reader.GetValue(3)?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            string? description = reader.IsDBNull(4) ? null : reader.GetValue(4)?.ToString();
            string? dataType = reader.IsDBNull(5) ? null : reader.GetValue(5)?.ToString();
            bool notNull = !reader.IsDBNull(6) && Convert.ToBoolean(reader.GetValue(6));
            string? defaultValue = reader.IsDBNull(7) ? null : reader.GetValue(7)?.ToString();
            columns.Add(new NetezzaSchemaColumn(
                name,
                dataType,
                Nullable: !notNull,
                string.IsNullOrEmpty(description) ? null : description,
                string.IsNullOrEmpty(defaultValue) ? null : defaultValue));
        }

        return map;
    }

    private static Dictionary<string, List<string>> ReadNameListsByTable(
        DbConnection connection,
        string sql,
        int schemaIndex,
        int tableIndex,
        int nameIndex,
        string? schemaFilter)
    {
        try
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            using DbDataReader reader = command.ExecuteReader();
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                string schema = reader.IsDBNull(schemaIndex) ? string.Empty : reader.GetValue(schemaIndex)?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(schemaFilter)
                    && !schema.Equals(schemaFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                string tableName = reader.IsDBNull(tableIndex) ? string.Empty : reader.GetValue(tableIndex)?.ToString() ?? string.Empty;
                string? name = reader.IsDBNull(nameIndex) ? null : reader.GetValue(nameIndex)?.ToString();
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(name))
                    continue;

                string key = $"{schema}.{tableName}";
                if (!map.TryGetValue(key, out var list))
                {
                    list = [];
                    map[key] = list;
                }

                list.Add(name);
            }

            return map;
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<string, IReadOnlyList<NetezzaKeyDdl>> ReadKeysByTable(
        DbConnection connection,
        string databaseName,
        string? schemaFilter)
    {
        try
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = NetezzaCatalogSql.GetKeysSql(databaseName);
            using DbDataReader reader = command.ExecuteReader();
            var rowsByTable = new Dictionary<string, List<OnlineTableKeyRow>>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                string schema = reader.IsDBNull(0) ? string.Empty : reader.GetValue(0)?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(schemaFilter)
                    && !schema.Equals(schemaFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                string tableName = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty;
                string constraintName = reader.IsDBNull(2) ? string.Empty : reader.GetValue(2)?.ToString() ?? string.Empty;
                string contype = reader.IsDBNull(3) ? string.Empty : reader.GetValue(3)?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(tableName) || constraintName.Length == 0 || contype.Length == 0)
                    continue;

                string key = $"{schema}.{tableName}";
                if (!rowsByTable.TryGetValue(key, out var rows))
                {
                    rows = [];
                    rowsByTable[key] = rows;
                }

                rows.Add(new OnlineTableKeyRow(
                    constraintName,
                    contype[0],
                    reader.IsDBNull(4) ? string.Empty : reader.GetValue(4)?.ToString() ?? string.Empty,
                    reader.IsDBNull(5) ? null : reader.GetValue(5)?.ToString(),
                    reader.IsDBNull(6) ? null : reader.GetValue(6)?.ToString(),
                    reader.IsDBNull(7) ? null : reader.GetValue(7)?.ToString(),
                    reader.IsDBNull(8) ? null : reader.GetValue(8)?.ToString(),
                    reader.IsDBNull(9) ? null : reader.GetValue(9)?.ToString(),
                    reader.IsDBNull(10) ? null : reader.GetValue(10)?.ToString()));
            }

            return rowsByTable.ToDictionary(
                pair => pair.Key,
                pair => OnlineTableDdlLoader.MapKeys(pair.Value),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<string, string> ReadTableComments(
        DbConnection connection,
        string databaseName,
        string? schemaFilter)
    {
        try
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = NetezzaCatalogSql.GetObjectDescriptionsSql(databaseName, schemaFilter);
            using DbDataReader reader = command.ExecuteReader();
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                // OBJID, OBJTYPE, OBJNAME, SCHEMA, OWNER, DESCRIPTION
                string objType = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty;
                if (!objType.Equals("TABLE", StringComparison.OrdinalIgnoreCase))
                    continue;

                string tableName = reader.IsDBNull(2) ? string.Empty : reader.GetValue(2)?.ToString() ?? string.Empty;
                string schema = reader.IsDBNull(3) ? string.Empty : reader.GetValue(3)?.ToString() ?? string.Empty;
                string? description = reader.IsDBNull(5) ? null : reader.GetValue(5)?.ToString();
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(description))
                    continue;

                map[$"{schema}.{tableName}"] = description;
            }

            return map;
        }
        catch
        {
            return [];
        }
    }
}
