using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustyBase.Netezza.Ddl;
using JustyBase.Netezza.Models;
using JustyBase.NetezzaDdl.Models;

namespace AppBase.Data.Ddl;

public static class LegacyDdlSchemaAdapter
{
    public static NetezzaTableDdlInput BuildTableInput(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        INetezzaSchemaTableCatalog schemaTables,
        IConnectionSessionRegistry connectionSessions,
        string connectionName,
        int objectId,
        string? overrideTableName = null,
        string? middleCode = null,
        string? endingCode = null)
    {
        ArgumentNullException.ThrowIfNull(schemaTables);
        ArgumentNullException.ThrowIfNull(connectionSessions);

        if (!schemaTables.TablesByConnection.TryGetValue(connectionName, out var baseTables)
            || !baseTables.TryGetValue(objectId, out var tableInfo))
        {
            throw new InvalidOperationException($"Object {objectId} not found in schema for {connectionName}");
        }
        string databaseName = baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var dbDict)
            && dbDict.TryGetValue(tableInfo.DATABASE_ID, out var dbInfo)
            ? dbInfo.DatabaseName
            : string.Empty;

        return NetezzaDdlInputFactory.BuildTable(
            BuildSchemaTable(baseWindowHelpers, connectionName, tableInfo, databaseName),
            BuildOrderedColumnNames(baseWindowHelpers, connectionName, tableInfo, useDist: true),
            BuildOrderedColumnNames(baseWindowHelpers, connectionName, tableInfo, useDist: false),
            BuildKeys(baseWindowHelpers, schemaTables, connectionSessions, connectionName, objectId),
            overrideTableName,
            middleCode,
            endingCode,
            tableInfo.TABLE_OBJECT_OWNER);
    }

    public static NetezzaExternalDdlInput BuildExternalInput(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        INetezzaSchemaTableCatalog schemaTables,
        string connectionName,
        int objectId,
        NetezzaExternalTableOptions options)
    {
        ArgumentNullException.ThrowIfNull(schemaTables);

        if (!schemaTables.TablesByConnection.TryGetValue(connectionName, out var baseTables)
            || !baseTables.TryGetValue(objectId, out var tableInfo))
        {
            throw new InvalidOperationException($"Object {objectId} not found in schema for {connectionName}");
        }
        string databaseName = baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var dbDict)
            && dbDict.TryGetValue(tableInfo.DATABASE_ID, out var dbInfo)
            ? dbInfo.DatabaseName
            : string.Empty;

        return NetezzaDdlInputFactory.BuildExternal(
            BuildSchemaTable(baseWindowHelpers, connectionName, tableInfo, databaseName),
            options);
    }

    public static NetezzaViewDdlInput BuildViewInput(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        INetezzaSchemaTableCatalog schemaTables,
        string connectionName,
        int objectId,
        string viewDefinition)
    {
        ArgumentNullException.ThrowIfNull(schemaTables);

        if (!schemaTables.TablesByConnection.TryGetValue(connectionName, out var baseTables)
            || !baseTables.TryGetValue(objectId, out var tableInfo))
        {
            throw new InvalidOperationException($"Object {objectId} not found in schema for {connectionName}");
        }
        string databaseName = baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var dbDict)
            && dbDict.TryGetValue(tableInfo.DATABASE_ID, out var dbInfo)
            ? dbInfo.DatabaseName
            : string.Empty;

        return new NetezzaViewDdlInput(
            databaseName,
            tableInfo.TABLE_SCHEMA,
            tableInfo.TABLE_NAME,
            viewDefinition,
            string.IsNullOrEmpty(tableInfo.TABLE_DESC) ? null : tableInfo.TABLE_DESC);
    }

    private static NetezzaSchemaTable BuildSchemaTable(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        string connectionName,
        NetezzaTableInfo tableInfo,
        string databaseName)
    {
        var columns = new List<NetezzaSchemaColumn>();
        if (!baseWindowHelpers.ColumnTablesDictionary.TryGetValue(connectionName, out var columnsByIndex)
            || tableInfo.COLUMN_COUNT <= 0
            || tableInfo.FIRST_COLUMN_ID < 0)
        {
            return new NetezzaSchemaTable(
                tableInfo.TABLE_NAME,
                tableInfo.TABLE_SCHEMA,
                databaseName,
                tableInfo.TABLE_KIND == TypeInDatabase.view,
                columns,
                string.IsNullOrEmpty(tableInfo.TABLE_DESC) ? null : tableInfo.TABLE_DESC);
        }

        int columnCount = columnsByIndex.Count;
        if (tableInfo.FIRST_COLUMN_ID >= columnCount)
            return new NetezzaSchemaTable(
                tableInfo.TABLE_NAME,
                tableInfo.TABLE_SCHEMA,
                databaseName,
                tableInfo.TABLE_KIND == TypeInDatabase.view,
                columns,
                string.IsNullOrEmpty(tableInfo.TABLE_DESC) ? null : tableInfo.TABLE_DESC);

        int available = Math.Min(tableInfo.COLUMN_COUNT, columnCount - tableInfo.FIRST_COLUMN_ID);
        for (int i = 0; i < available; i++)
        {
            var column = columnsByIndex[tableInfo.FIRST_COLUMN_ID + i];
            columns.Add(new NetezzaSchemaColumn(
                column.COLUMN_NAME,
                column.DATA_TYPE,
                column.IS_NULLABLE,
                string.IsNullOrEmpty(column.COLUMN_DESCRIPTION) ? null : column.COLUMN_DESCRIPTION,
                column.COLDEFAULT));
        }

        return new NetezzaSchemaTable(
            tableInfo.TABLE_NAME,
            tableInfo.TABLE_SCHEMA,
            databaseName,
            tableInfo.TABLE_KIND == TypeInDatabase.view,
            columns,
            string.IsNullOrEmpty(tableInfo.TABLE_DESC) ? null : tableInfo.TABLE_DESC);
    }

    private static List<string> BuildOrderedColumnNames(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        string connectionName,
        NetezzaTableInfo tableInfo,
        bool useDist)
    {
        var result = new List<(byte Seq, string Name)>();
        if (!baseWindowHelpers.ColumnTablesDictionary.TryGetValue(connectionName, out var columnsByIndex)
            || tableInfo.COLUMN_COUNT <= 0
            || tableInfo.FIRST_COLUMN_ID < 0)
        {
            return [];
        }

        int columnCount = columnsByIndex.Count;
        if (tableInfo.FIRST_COLUMN_ID >= columnCount)
            return [];

        int available = Math.Min(tableInfo.COLUMN_COUNT, columnCount - tableInfo.FIRST_COLUMN_ID);
        for (int i = 0; i < available; i++)
        {
            var column = columnsByIndex[tableInfo.FIRST_COLUMN_ID + i];
            sbyte? seq = useDist ? column.DISTSEQNO : column.ORGSEQNO;
            if (seq is not null)
                result.Add(((byte)seq, column.COLUMN_NAME));
        }

        result.Sort((a, b) => a.Seq.CompareTo(b.Seq));
        return result.Select(x => x.Name).ToList();
    }

    private static List<NetezzaKeyDdl> BuildKeys(
        AppBase.Common.Interfaces.IDatabaseRuntimeContext baseWindowHelpers,
        INetezzaSchemaTableCatalog schemaTables,
        IConnectionSessionRegistry connectionSessions,
        string connectionName,
        int objectId)
    {
        if (!connectionSessions.TryGetValue(connectionName, out var gdb)
            || gdb is not INetezza nz
            || !nz.keysInTables.TryGetValue(objectId, out var keyRows)
            || keyRows.Count == 0)
        {
            return [];
        }

        if (!schemaTables.TablesByConnection.TryGetValue(connectionName, out var baseTableMap))
            return [];

        var keys = new List<NetezzaKeyDdl>();

        foreach (var keyName in keyRows.Select(k => k.keyName).Distinct())
        {
            var rowsForKey = keyRows.Where(k => k.keyName == keyName).OrderBy(k => k.columnPosition).ToList();
            char keyType = rowsForKey[0].keyType;
            var columnNames = rowsForKey.Select(k => k.columnName).ToList();

            if (keyType == 'f')
            {
                var first = rowsForKey[0];
                if (first.refTableId is not int refTableId || !baseTableMap.TryGetValue(refTableId, out var refTable))
                    continue;

                string pkDatabase = baseWindowHelpers.DatabaseDictionary.TryGetValue(connectionName, out var pkDbDict)
                    && pkDbDict.TryGetValue(refTable.DATABASE_ID, out var pkDbInfo)
                    ? pkDbInfo.DatabaseName
                    : string.Empty;
                var refColumns = rowsForKey.Select(k => k.refColumnName).Where(c => c is not null).Cast<string>().ToList();

                keys.Add(new NetezzaKeyDdl(
                    keyType,
                    keyName,
                    columnNames,
                    pkDatabase,
                    refTable.TABLE_SCHEMA,
                    refTable.TABLE_NAME,
                    refColumns,
                    first.DEL_TYPE ?? "NO ACTION",
                    first.UPDT_TYPE ?? "NO ACTION"));
            }
            else
            {
                keys.Add(new NetezzaKeyDdl(keyType, keyName, columnNames));
            }
        }

        return keys;
    }
}
