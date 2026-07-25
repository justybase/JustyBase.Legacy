using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Common.Enums;
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
        string connectionName)
    {
        if (schemaProvider is null || completionContext is null || string.IsNullOrEmpty(connectionName))
            return NetezzaSchemaSnapshot.Empty;

        var snapshot = BuildSnapshot(completionContext, [connectionName]);
        NetezzaSchemaProviderAdapter.Apply(schemaProvider, snapshot);
        return snapshot;
    }

    /// <summary>
    /// Rebuilds schema for every connection that already has a loaded cache in Legacy.
    /// </summary>
    public static void SyncAllLoadedConnections(
        InMemorySchemaProvider schemaProvider,
        INetezzaCompletionContext completionContext)
    {
        if (schemaProvider is null || completionContext?.DatabaseSchemaLookup is null)
            return;

        NetezzaSchemaProviderAdapter.Apply(
            schemaProvider,
            BuildSnapshot(completionContext, completionContext.DatabaseSchemaLookup.Keys));
    }

    public static void SyncSelectedConnection(
        InMemorySchemaProvider schemaProvider,
        INetezzaCompletionContext completionContext)
    {
        if (string.IsNullOrEmpty(completionContext?.SelectedConnectionName))
            return;

        SyncConnection(schemaProvider, completionContext, completionContext.SelectedConnectionName);
    }

    private static NetezzaSchemaSnapshot BuildSnapshot(
        INetezzaCompletionContext completionContext,
        IEnumerable<string> connectionNames)
    {
        var tables = new List<NetezzaSchemaTable>();

        foreach (var connectionName in connectionNames)
        {
            if (!completionContext.DatabaseSchemaLookup.TryGetValue(connectionName, out var databases)
                || !NetezzaHelpers.baseTableDictionary.TryGetValue(connectionName, out var tablesById))
                continue;

            completionContext.ColumnTablesDictionary.TryGetValue(connectionName, out var columnsByIndex);

            foreach (var (databaseName, tableLookup) in databases)
            {
                foreach (var (tableName, (_, tableId)) in tableLookup)
                {
                    if (!tablesById.TryGetValue(tableId, out var tableInfo))
                        continue;

                    tables.Add(new NetezzaSchemaTable(
                        tableName,
                        tableInfo.TABLE_SCHEMA,
                        databaseName,
                        tableInfo.TABLE_KIND == TypeInDatabase.view,
                        BuildColumns(tableInfo, columnsByIndex),
                        string.IsNullOrEmpty(tableInfo.TABLE_DESC) ? null : tableInfo.TABLE_DESC));
                }
            }
        }

        return new NetezzaSchemaSnapshot(tables, DateTime.UtcNow.Ticks);
    }

    private static NetezzaSchemaColumn[] BuildColumns(
        NetezzaTableInfo tableInfo,
        List<NetezzaColumnInfoRow> columnsByIndex)
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
