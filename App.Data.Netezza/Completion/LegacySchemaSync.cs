using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustyBase.Netezza.Models;
using JustyBase.Netezza.Schema;
using JustyBase.NetezzaSqlParser.Visitor;

namespace AppBase.Data.Completion;

/// <summary>
/// Maps Legacy DatabaseSchemaLookup / ColumnTablesDictionary cache into InMemorySchemaProvider.
/// No new DB queries — uses data already loaded by NetezzaHelpers.
/// </summary>
public static class LegacySchemaSync
{
    /// <summary>
    /// Rebuilds schema for one connection (clears provider first — active connection only).
    /// </summary>
    public static NetezzaSchemaSnapshot SyncConnection(
        InMemorySchemaProvider schemaProvider,
        INetezzaCompletionContext completionContext,
        INetezzaSchemaTableCatalog schemaTables,
        string connectionName,
        string? databaseName = null)
    {
        if (schemaProvider is null || completionContext is null || schemaTables is null || string.IsNullOrEmpty(connectionName))
            return NetezzaSchemaSnapshot.Empty;

        var snapshot = BuildSnapshot(completionContext, schemaTables, [connectionName], databaseName);
        NetezzaSchemaProviderAdapter.Apply(schemaProvider, snapshot);
        return snapshot;
    }

    /// <summary>
    /// Rebuilds schema for every connection that already has a loaded cache in Legacy.
    /// </summary>
    public static void SyncAllLoadedConnections(
        InMemorySchemaProvider schemaProvider,
        INetezzaCompletionContext completionContext,
        INetezzaSchemaTableCatalog schemaTables)
    {
        if (schemaProvider is null || completionContext is null || schemaTables is null)
            return;

        NetezzaSchemaProviderAdapter.Apply(
            schemaProvider,
            BuildSnapshot(completionContext, schemaTables, schemaTables.TablesByConnection.Keys, null));
    }

    public static void SyncSelectedConnection(
        InMemorySchemaProvider schemaProvider,
        INetezzaCompletionContext completionContext,
        INetezzaSchemaTableCatalog schemaTables)
    {
        if (string.IsNullOrEmpty(completionContext?.SelectedConnectionName) || schemaTables is null)
            return;

        SyncConnection(schemaProvider, completionContext, schemaTables, completionContext.SelectedConnectionName);
    }

    private static NetezzaSchemaSnapshot BuildSnapshot(
        INetezzaCompletionContext completionContext,
        INetezzaSchemaTableCatalog schemaTables,
        IEnumerable<string> connectionNames,
        string? requestedDatabaseName)
    {
        var tables = new List<NetezzaSchemaTable>();

        foreach (var connectionName in connectionNames)
        {
            if (!schemaTables.TablesByConnection.TryGetValue(connectionName, out var tablesById))
                continue;

            completionContext.DatabaseDictionary.TryGetValue(connectionName, out var databasesById);
            completionContext.ColumnTablesDictionary.TryGetValue(connectionName, out var columnsByIndex);
            completionContext.DatabaseSchemaLookup.TryGetValue(connectionName, out var lookupByDatabase);

            foreach (var (tableId, tableInfo) in tablesById)
            {
                string? catalogDatabaseName = null;
                if (databasesById is not null
                    && databasesById.TryGetValue(tableInfo.DATABASE_ID, out var databaseInfo))
                {
                    catalogDatabaseName = databaseInfo.DatabaseName;
                }
                else if (lookupByDatabase is not null)
                {
                    catalogDatabaseName = lookupByDatabase
                        .Where(pair => pair.Value.Values.Any(value => value.tableId == tableId))
                        .Select(pair => pair.Key)
                        .FirstOrDefault();
                }

                string canonicalDatabaseName = !string.IsNullOrWhiteSpace(catalogDatabaseName)
                    ? catalogDatabaseName
                    : requestedDatabaseName ?? string.Empty;

                // Some Netezza catalog variants expose the namespace only as
                // TABLE_OWNER and leave TABLE_SCHEMA empty. Keep the parser and
                // legacy completion projections consistent in that case.
                string schema = string.IsNullOrWhiteSpace(tableInfo.TABLE_SCHEMA)
                    ? tableInfo.TABLE_OWNER
                    : tableInfo.TABLE_SCHEMA;

                tables.Add(new NetezzaSchemaTable(
                    tableInfo.TABLE_NAME,
                    schema,
                    canonicalDatabaseName,
                    Kind: tableInfo.TABLE_KIND == TypeInDatabase.view ? NetezzaObjectKind.View : NetezzaObjectKind.Table,
                    IsView: tableInfo.TABLE_KIND == TypeInDatabase.view,
                    Columns: BuildColumns(tableInfo, columnsByIndex),
                    Description: string.IsNullOrEmpty(tableInfo.TABLE_DESC) ? null : tableInfo.TABLE_DESC));
            }
        }

        return new NetezzaSchemaSnapshot(tables, DateTime.UtcNow.Ticks);
    }

    private static NetezzaSchemaColumn[] BuildColumns(
        NetezzaTableInfo tableInfo,
        List<NetezzaColumnInfoRow>? columnsByIndex)
    {
        if (columnsByIndex is null
            || tableInfo.COLUMN_COUNT <= 0
            || tableInfo.FIRST_COLUMN_ID < 0)
            return Array.Empty<NetezzaSchemaColumn>();

        int columnCount = columnsByIndex.Count;
        if (tableInfo.FIRST_COLUMN_ID >= columnCount)
            return Array.Empty<NetezzaSchemaColumn>();

        int available = Math.Min(tableInfo.COLUMN_COUNT, columnCount - tableInfo.FIRST_COLUMN_ID);
        if (available <= 0)
            return Array.Empty<NetezzaSchemaColumn>();

        var columns = new NetezzaSchemaColumn[available];
        for (int i = 0; i < available; i++)
        {
            var col = columnsByIndex[tableInfo.FIRST_COLUMN_ID + i];
            columns[i] = new NetezzaSchemaColumn(
                col.COLUMN_NAME,
                col.DATA_TYPE,
                col.IS_NULLABLE,
                col.COLUMN_DESCRIPTION,
                col.COLDEFAULT);
        }

        return columns;
    }
}
