using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using JustData.Application.Schema;
using JustyBase.Netezza.Ddl;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDdl;
using System.Data.Common;
using System.Text;

namespace JustyBaseLegacy.UI.Schema;

public sealed class LegacySchemaDdlService : ISchemaDdlService
{
    private readonly INetezzaHelperService _netezzaHelperService;
    private readonly AppBase.Common.Interfaces.IDatabaseRuntimeContext _databaseRuntimeContext;

    public LegacySchemaDdlService(INetezzaHelperService netezzaHelperService, AppBase.Common.Interfaces.IDatabaseRuntimeContext databaseRuntimeContext)
    {
        _netezzaHelperService = netezzaHelperService ?? throw new ArgumentNullException(nameof(netezzaHelperService));
        _databaseRuntimeContext = databaseRuntimeContext ?? throw new ArgumentNullException(nameof(databaseRuntimeContext));
    }

    public async Task<string> GetDdlAsync(SchemaDdlRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IGeneralDbService.ConnectionSessions.TryGetValue(request.Node.Path.Connection, out var database))
            throw new InvalidOperationException($"Connection '{request.Node.Path.Connection}' is not initialized.");

        if (database is INetezza && request.Node.LegacyObjectId is int objectId)
            return await GetNetezzaDdlAsync(request, database, objectId, cancellationToken).ConfigureAwait(false);

        string db = request.Node.Path.Database ?? database.DefaultDatabaseName;
        string schema = request.Node.Path.Schema ?? string.Empty;
        string name = request.Node.Path.Object ?? request.Node.Name;
        return request.Kind switch
        {
            SchemaDdlKind.SelectTop => BuildSelect(database, db, schema, name),
            SchemaDdlKind.AddCode => database.GetSqlAddCode(request.Node.Kind.ToString(), db, schema, name),
            _ => await BuildCreateAsync(database, request.Node.Kind, db, schema, name).ConfigureAwait(false)
        };
    }

    private async Task<string> GetNetezzaDdlAsync(
        SchemaDdlRequest request,
        IGeneralDb database,
        int objectId,
        CancellationToken cancellationToken)
    {
        string connection = request.Node.Path.Connection;
        if (!NetezzaHelpers.baseTableDictionary.TryGetValue(connection, out var tables)
            || !tables.TryGetValue(objectId, out var table))
            throw new InvalidOperationException($"Netezza object id '{objectId}' is not present in the loaded catalog.");
        if (!_databaseRuntimeContext.DatabaseDictionary.TryGetValue(connection, out var databases)
            || !databases.TryGetValue(table.DATABASE_ID, out var databaseInfo))
            throw new InvalidOperationException($"Netezza database id '{table.DATABASE_ID}' is not present in the loaded catalog.");

        cancellationToken.ThrowIfCancellationRequested();
        if (request.Kind == SchemaDdlKind.SelectTop)
            return BuildNetezzaSelect(connection, databaseInfo.DatabaseName, table.TABLE_OWNER, table.TABLE_NAME, objectId);
        if (request.Kind == SchemaDdlKind.AddCode)
            return database.GetSqlAddCode(request.Node.Kind.ToString(), databaseInfo.DatabaseName, table.TABLE_OWNER, table.TABLE_NAME);

        TypeInDatabase providerKind = Enum.TryParse(request.Node.ProviderKind, ignoreCase: true, out TypeInDatabase parsed)
            ? parsed
            : table.TABLE_KIND;
        return providerKind switch
        {
            TypeInDatabase.table => (await _netezzaHelperService.GetTableCodeById(
                new StringBuilder(), _databaseRuntimeContext, connection, objectId).ConfigureAwait(false)).Code,
            TypeInDatabase.thisExternal => await _netezzaHelperService.GetExternaTableCode(
                _databaseRuntimeContext, objectId, connection).ConfigureAwait(false),
            TypeInDatabase.view => await _netezzaHelperService.GetViewCodeById(
                _databaseRuntimeContext, objectId, connection).ConfigureAwait(false),
            TypeInDatabase.procedure => await GetProcedureDdlAsync(
                database, databaseInfo.DatabaseName, table.TABLE_OWNER, objectId, cancellationToken).ConfigureAwait(false),
            TypeInDatabase.sequence => await Task.Run(
                () => GetSequenceDdl(database, databaseInfo.DatabaseName, table.TABLE_OWNER, table.TABLE_NAME), cancellationToken).ConfigureAwait(false),
            TypeInDatabase.function or TypeInDatabase.thisAggregate or TypeInDatabase.synonym => await Task.Run(
                () => GetCatalogDdl(database, providerKind, objectId, table.TABLE_NAME, databaseInfo.DatabaseName), cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"DDL is not available for Netezza object kind '{providerKind}'.")
        };
    }

    private string BuildNetezzaSelect(string connection, string database, string owner, string tableName, int objectId)
    {
        string[] columns = [];
        if (NetezzaHelpers.baseTableDictionary.TryGetValue(connection, out var tables)
            && tables.TryGetValue(objectId, out var table)
            && _databaseRuntimeContext.ColumnTablesDictionary.TryGetValue(connection, out var catalogColumns))
        {
            columns = Enumerable.Range(table.FIRST_COLUMN_ID, table.COLUMN_COUNT)
                .Where(index => index >= 0 && index < catalogColumns.Count && catalogColumns[index].TABLE_ID == objectId)
                .Select(index => catalogColumns[index].COLUMN_NAME)
                .ToArray();
        }
        return $"SELECT\r\n    {string.Join(",\r\n    ", columns.Length == 0 ? ["*"] : columns)}\r\nFROM\r\n    {database}.{owner}.{tableName}\r\nLIMIT 100;";
    }

    private async Task<string> GetProcedureDdlAsync(
        IGeneralDb database,
        string databaseName,
        string schema,
        int objectId,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            using DbConnection connection = database.GetConnection(databaseName);
            connection.Open();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = NetezzaSystemSql.GetProcedureByObjectId(objectId);
            using DbDataReader reader = command.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException($"Netezza procedure id '{objectId}' was not found.");

            string signature = reader.GetString(1);
            string returns = _netezzaHelperService.NzProcReturnFix(reader.GetString(2));
            bool executeAsOwner = reader.GetBoolean(3);
            object description = reader.GetValue(4);
            string source = reader.GetString(5);
            var input = NetezzaDdlInputFactory.BuildProcedureFromSignature(
                databaseName,
                schema,
                signature,
                returns,
                source,
                executeAsOwner,
                description == DBNull.Value ? null : description?.ToString());
            return new NetezzaDdlTextBuilder().BuildCreateProcedure(input);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static string GetSequenceDdl(IGeneralDb database, string databaseName, string owner, string sequenceName)
    {
        using DbConnection connection = database.GetConnection(databaseName);
        connection.Open();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = NetezzaSystemSql.GetSequenceMetadata(sequenceName);
        using DbDataReader reader = command.ExecuteReader();
        if (!reader.Read())
            throw new InvalidOperationException($"Netezza sequence '{sequenceName}' was not found.");
        string typeName = reader.GetString(0);
        string lastValue = reader.GetString(1);
        string incrementBy = reader.GetString(2);
        string? minValue = reader.GetValue(3)?.ToString();
        string? maxValue = reader.GetValue(4)?.ToString();
        int isCycled = reader.GetInt32(5);
        bool isDefaultMax = typeName == "INTEGER" && maxValue == "2147483647"
            || typeName == "BIGINT" && maxValue == "9223372036854775807";
        return $"CREATE SEQUENCE {databaseName}.{owner}.{sequenceName}\r\n"+
            $"AS {typeName}\r\nSTART WITH {lastValue}\r\nINCREMENT BY {incrementBy}\r\n"+
            $"{(string.IsNullOrEmpty(minValue) ? "NO MINVALUE" : "MINVALUE " + minValue)}\r\n"+
            $"{(string.IsNullOrEmpty(maxValue) || isDefaultMax ? "NO MAXVALUE" : "MAXVALUE " + maxValue)}\r\n"+
            $"{(isCycled == 1 ? "CYCLE" : "NO CYCLE")};";
    }

    private static string GetCatalogDdl(IGeneralDb database, TypeInDatabase kind, int objectId, string objectName, string databaseName)
    {
        string sql = kind switch
        {
            TypeInDatabase.function => NetezzaSystemSql.GetFunctionInfo(objectName),
            TypeInDatabase.thisAggregate => NetezzaSystemSql.GetAggregateInfo(objectName),
            TypeInDatabase.synonym => NetezzaSystemSql.SynonymInfo,
            _ => throw new NotSupportedException($"DDL is not available for Netezza object kind '{kind}'.")
        };
        using DbConnection connection = database.GetConnection(databaseName);
        connection.Open();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        using DbDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetInt32(0) == objectId)
                return reader.GetValue(1)?.ToString() ?? string.Empty;
        }
        throw new InvalidOperationException($"Netezza object id '{objectId}' was not found.");
    }

    private static async Task<string> BuildCreateAsync(IGeneralDb database, SchemaNodeKind kind, string db, string schema, string name)
    {
        string qualified = string.Join('.', new[] { schema, name }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return kind switch
        {
            SchemaNodeKind.Table => database.GetCreateTableText(db, schema, name),
            SchemaNodeKind.View => database.GetCreateViewText(db, schema, name),
            SchemaNodeKind.Procedure or SchemaNodeKind.Function => await database.GetCreateProcedureText(qualified).ConfigureAwait(false),
            SchemaNodeKind.Alias => await database.GetCreateAliasTextAsync(qualified).ConfigureAwait(false),
            SchemaNodeKind.Synonym => await database.GetCreateSynonymTextAsync(qualified).ConfigureAwait(false),
            _ => database.GetSqlAddCode(kind.ToString(), db, schema, name)
        };
    }

    private static string BuildSelect(IGeneralDb database, string db, string schema, string name)
    {
        string[] columns = database.GetColumns(db, schema, name);
        string prefix = database.DatabaseType is DatabaseTypeEnum.DB2 or DatabaseTypeEnum.Oracle ? string.Empty : $"{db}.";
        string qualified = $"{prefix}{schema}.{name}";
        string limit = database.DatabaseType == DatabaseTypeEnum.Oracle
            ? "WHERE\r\n    ROWNUM < 100"
            : "LIMIT 100";
        return $"SELECT\r\n    {string.Join(",\r\n    ", columns.Length == 0 ? ["*"] : columns)}\r\nFROM\r\n    {qualified}\r\n{limit};";
    }
}
