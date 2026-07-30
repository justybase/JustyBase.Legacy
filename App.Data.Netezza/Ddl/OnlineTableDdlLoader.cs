using AppBase.Data.Core.Core;
using JustyBase.Netezza.Ddl;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDdl.Models;
using System.Data.Common;

namespace AppBase.Data.Ddl;

/// <summary>
/// Lite-style live per-object table DDL metadata fetch. Does not refresh the schema tree/cache.
/// </summary>
public static class OnlineTableDdlLoader
{
    public static async Task<NetezzaTableDdlInput> LoadAsync(
        IGeneralDb database,
        string databaseName,
        string schema,
        string tableName,
        string? tableOwner = null,
        string? overrideTableName = null,
        string? middleCode = null,
        string? endingCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using DbConnection connection = database.GetConnection(databaseName);
            connection.Open();

            IReadOnlyList<NetezzaSchemaColumn> columns = ReadColumns(connection, databaseName, schema, tableName);
            if (columns.Count == 0)
                throw new InvalidOperationException($"Table {databaseName}.{schema}.{tableName} not found or has no columns.");

            IReadOnlyList<string> distribute = ReadNameList(
                connection,
                NetezzaCatalogSql.GetDistributionKeysSql(databaseName, schema, tableName));
            IReadOnlyList<string> organize = ReadNameList(
                connection,
                NetezzaCatalogSql.GetOrganizeColumnsSql(databaseName, schema, tableName));
            IReadOnlyList<NetezzaKeyDdl> keys = ReadKeys(connection, databaseName, schema, tableName);
            string? tableComment = ReadTableComment(connection, databaseName, schema, tableName);

            return BuildInput(
                databaseName,
                schema,
                tableName,
                columns,
                distribute,
                organize,
                keys,
                tableComment,
                tableOwner,
                overrideTableName,
                middleCode,
                endingCode);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds DDL input from already-fetched metadata (unit-test friendly).</summary>
    public static NetezzaTableDdlInput BuildInput(
        string databaseName,
        string schema,
        string tableName,
        IReadOnlyList<NetezzaSchemaColumn> columns,
        IReadOnlyList<string>? distributeColumns = null,
        IReadOnlyList<string>? organizeColumns = null,
        IReadOnlyList<NetezzaKeyDdl>? keys = null,
        string? tableComment = null,
        string? tableOwner = null,
        string? overrideTableName = null,
        string? middleCode = null,
        string? endingCode = null)
    {
        var table = new NetezzaSchemaTable(
            tableName,
            schema,
            databaseName,
            IsView: false,
            columns,
            string.IsNullOrEmpty(tableComment) ? null : tableComment);

        return NetezzaDdlInputFactory.BuildTable(
            table,
            distributeColumns,
            organizeColumns,
            keys,
            overrideTableName,
            middleCode,
            endingCode,
            tableOwner);
    }

    /// <summary>Groups key rows into <see cref="NetezzaKeyDdl"/> (same aggregation as Lite getKeysInfo).</summary>
    public static IReadOnlyList<NetezzaKeyDdl> MapKeys(IEnumerable<OnlineTableKeyRow> rows)
    {
        var grouped = new Dictionary<string, List<OnlineTableKeyRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (OnlineTableKeyRow row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ConstraintName))
                continue;
            if (!grouped.TryGetValue(row.ConstraintName, out var list))
            {
                list = [];
                grouped[row.ConstraintName] = list;
            }
            list.Add(row);
        }

        var keys = new List<NetezzaKeyDdl>(grouped.Count);
        foreach ((string keyName, List<OnlineTableKeyRow> keyRows) in grouped)
        {
            char keyType = char.ToLowerInvariant(keyRows[0].ConstraintType);
            var columnNames = keyRows.Select(r => r.ColumnName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            if (keyType == 'f')
            {
                OnlineTableKeyRow first = keyRows[0];
                var refColumns = keyRows
                    .Select(r => r.PkColumnName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Cast<string>()
                    .ToList();
                keys.Add(new NetezzaKeyDdl(
                    keyType,
                    keyName,
                    columnNames,
                    first.PkDatabase,
                    first.PkSchema,
                    first.PkRelation,
                    refColumns,
                    first.OnDelete ?? "NO ACTION",
                    first.OnUpdate ?? "NO ACTION"));
            }
            else
            {
                keys.Add(new NetezzaKeyDdl(keyType, keyName, columnNames));
            }
        }

        return keys;
    }

    private static IReadOnlyList<NetezzaSchemaColumn> ReadColumns(
        DbConnection connection,
        string databaseName,
        string schema,
        string tableName)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = NetezzaCatalogSql.GetTableColumnsSql(databaseName, schema, tableName);
        using DbDataReader reader = command.ExecuteReader();
        var columns = new List<NetezzaSchemaColumn>();
        while (reader.Read())
        {
            string name = reader.GetString(1);
            string? description = reader.IsDBNull(2) ? null : reader.GetValue(2)?.ToString();
            string? dataType = reader.IsDBNull(3) ? null : reader.GetValue(3)?.ToString();
            bool notNull = !reader.IsDBNull(4) && Convert.ToBoolean(reader.GetValue(4));
            string? defaultValue = reader.IsDBNull(5) ? null : reader.GetValue(5)?.ToString();
            columns.Add(new NetezzaSchemaColumn(
                name,
                dataType,
                Nullable: !notNull,
                string.IsNullOrEmpty(description) ? null : description,
                string.IsNullOrEmpty(defaultValue) ? null : defaultValue));
        }

        return columns;
    }

    private static IReadOnlyList<string> ReadNameList(DbConnection connection, string sql)
    {
        try
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            using DbDataReader reader = command.ExecuteReader();
            var names = new List<string>();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    string? name = reader.GetValue(0)?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        names.Add(name);
                }
            }

            return names;
        }
        catch
        {
            // Dist/organize views may be unavailable on some Netezza versions.
            return [];
        }
    }

    private static IReadOnlyList<NetezzaKeyDdl> ReadKeys(
        DbConnection connection,
        string databaseName,
        string schema,
        string tableName)
    {
        try
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = NetezzaCatalogSql.GetTableKeysSql(databaseName, schema, tableName);
            using DbDataReader reader = command.ExecuteReader();
            var rows = new List<OnlineTableKeyRow>();
            while (reader.Read())
            {
                string constraintName = reader.IsDBNull(2) ? string.Empty : reader.GetValue(2)?.ToString() ?? string.Empty;
                string contype = reader.IsDBNull(3) ? string.Empty : reader.GetValue(3)?.ToString() ?? string.Empty;
                if (constraintName.Length == 0 || contype.Length == 0)
                    continue;

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

            return MapKeys(rows);
        }
        catch
        {
            return [];
        }
    }

    private static string? ReadTableComment(
        DbConnection connection,
        string databaseName,
        string schema,
        string tableName)
    {
        try
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = NetezzaCatalogSql.GetObjectCommentSql(
                databaseName, schema, tableName, "TABLE");
            object? scalar = command.ExecuteScalar();
            string? comment = scalar is null or DBNull ? null : scalar.ToString();
            if (!string.IsNullOrEmpty(comment))
                return comment;

            command.CommandText = NetezzaCatalogSql.GetObjectCommentSql(
                databaseName, schema, tableName);
            scalar = command.ExecuteScalar();
            return scalar is null or DBNull ? null : scalar.ToString();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>One row from single-table key metadata SQL.</summary>
public readonly record struct OnlineTableKeyRow(
    string ConstraintName,
    char ConstraintType,
    string ColumnName,
    string? PkDatabase,
    string? PkSchema,
    string? PkRelation,
    string? PkColumnName,
    string? OnUpdate,
    string? OnDelete);
