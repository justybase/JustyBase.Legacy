using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using AppBase.Data.Ddl;
using JustyBase.NetezzaDdl;
using JustyBase.NetezzaDdl.Models;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDriver;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace AppBase.Services;

public sealed class NetezzaHelperService : INetezzaHelperService
{
    private readonly NetezzaDdlTextBuilder _ddlBuilder = new();
    private readonly IConnectionSessionRegistry _connectionSessions;
    private readonly INetezzaSchemaTableCatalog _schemaTables;

    private INetezzaSchemaRefreshHost _schemaRefreshHost;

    public NetezzaHelperService(
        IConnectionSessionRegistry connectionSessions,
        INetezzaSchemaTableCatalog schemaTables)
    {
        _connectionSessions = connectionSessions ?? throw new ArgumentNullException(nameof(connectionSessions));
        _schemaTables = schemaTables ?? throw new ArgumentNullException(nameof(schemaTables));
    }

    public void Initialize(INetezzaSchemaRefreshHost schemaRefreshHost)
    {
        _schemaRefreshHost = schemaRefreshHost;
    }


    public const string procExample = NetezzaDdlTemplates.CreateProcedurePattern;

    public const string DATABASES = NetezzaHelpers.DATABASES;
    public const string COST = NetezzaHelpers.COST;
    public const string SESSION = NetezzaHelpers.SESSION;
    public const string VIEW_CODE = NetezzaHelpers.VIEW_CODE;
    public const string CurrentDataSql = NetezzaHelpers.CurrentDataSql;
    public const string TABLE_KEYS_NZ_SQL = NetezzaHelpers.TABLE_KEYS_NZ_SQL;
    public const string SEARCH_VIEW_SQL = NetezzaHelpers.SEARCH_VIEW_SQL;
    public const string SEARCH_PROCEDURE_SQL = NetezzaHelpers.SEARCH_PROCEDURE_SQL;
    public readonly string USER_GROUPS = NetezzaHelpers.USER_GROUPS;

    private IReadOnlyDictionary<string, Dictionary<int, NetezzaTableInfo>> BaseTableDictionary => _schemaTables.TablesByConnection;

    public string ServerVersion { get; set; } = "";
    public bool SqliteInProgress { get; set; }

    public string DatabaseTablesSql(string dbName, bool ownerMode = true, bool noDescMode = false)
        => NetezzaHelpers.DatabaseTablesSql(dbName, ownerMode, noDescMode);

    public string GetDescSql(string dbName) => NetezzaHelpers.GetDescSql(dbName);
    public string NzProcReturnFix(string procReturns) => NetezzaHelpers.NzProcReturnFix(procReturns);
    public string OBJECT_COLUMNS_NZ_SQL_OF_DB(string dbName) => NetezzaHelpers.OBJECT_COLUMNS_NZ_SQL_OF_DB(dbName);
    public string OneTableSqlOwner(string tablename) => NetezzaHelpers.OneTableSqlOwner(tablename);
    public string OneTableSqlSchema(string tablename, bool schemaOn) => NetezzaHelpers.OneTableSqlSchema(tablename, schemaOn);

    public void OnSchemaProblemNetezzaAskForRestart(AppBase.Common.Interfaces.IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, string connectionName, Action action)
        => NetezzaHelpers.OnSchemaProblemNetezzaAskForRestart(databaseRuntimeContext, logger, connectionName, action);

    public string SearchInNetezzaSchema(string dbName, string txtToSearch)
        => NetezzaHelpers.SearchInNetezzaSchema(dbName, txtToSearch);

    public string ExternalSql(string database) => NetezzaHelpers.ExternalSql(database);
    public string GetFulidesSql(string databaseName, int databaseId) => NetezzaHelpers.GetFulidesSql(databaseName, databaseId);
    public string ProcSql(string database) => NetezzaHelpers.ProcSql(database);
    public string SynonymSql(string database) => NetezzaHelpers.SynonymSql(database);
    public string ViewSql(string database) => NetezzaHelpers.ViewSql(database);

    public async ValueTask<NzGetTableCodeResult> GetTableCodeById(
        StringBuilder stringBuilder,
        AppBase.Common.Interfaces.IDatabaseRuntimeContext databaseRuntimeContext,
        string connectionName, int objectID, string overrideTableName = null, string middleCode = null, string endingCode = null, List<string> distOverride = null
        ,bool forceNotOnline = false)
    {
        if (!_connectionSessions.TryGetValue(connectionName, out var gdb) || gdb is not INetezza)
        {
            throw new Exception("actual connection is not netezza");
        }

        if (databaseRuntimeContext.Config.OnlineOnlyDdls && !forceNotOnline)
        {
            await _schemaRefreshHost?.RefreshTableListInternalAsync(connectionName, false);
        }

        var input = LegacyDdlSchemaAdapter.BuildTableInput(
            databaseRuntimeContext,
            _schemaTables,
            _connectionSessions,
            connectionName,
            objectID,
            overrideTableName,
            middleCode,
            endingCode);

        StringBuilder sb = stringBuilder ?? new();
        var result = _ddlBuilder.AppendCreateTable(sb, input, distOverride);

        return new NzGetTableCodeResult(sb)
        {
            Dystr = result.DistributeColumns.Select((name, index) => ((byte)(index + 1), name)).ToList(),
            OrganizeList = result.OrganizeColumns.Select((name, index) => ((byte)(index + 1), name)).ToList()
        };
    }



    public async ValueTask<NzGetTableCodeResult> GetRecreateTableCodeById(AppBase.Common.Interfaces.IDatabaseRuntimeContext databaseRuntimeContext,
            string connectionName, int objectID, List<string> distOverride = null)
    {
        if (databaseRuntimeContext.Config.OnlineOnlyDdls)
        {
            await _schemaRefreshHost?.RefreshTableListInternalAsync(connectionName, false);
        }

        var input = LegacyDdlSchemaAdapter.BuildTableInput(
            databaseRuntimeContext,
            _schemaTables,
            _connectionSessions,
            connectionName,
            objectID);
        StringBuilder sb = new();
        var result = _ddlBuilder.AppendRecreateTable(sb, input);

        return new NzGetTableCodeResult(sb)
        {
            Dystr = result.DistributeColumns.Select((name, index) => ((byte)(index + 1), name)).ToList(),
            OrganizeList = result.OrganizeColumns.Select((name, index) => ((byte)(index + 1), name)).ToList()
        };
    }

    public async Task<string> GetViewCodeById(AppBase.Common.Interfaces.IDatabaseRuntimeContext databaseRuntimeContext, int objectId, string connectionName)
    {
        if (!BaseTableDictionary.TryGetValue(connectionName, out var baseTables)
            || !baseTables.TryGetValue(objectId, out var tableInfo))
        {
            return $"-- object {objectId} not found in schema";
        }
        if (!databaseRuntimeContext.DatabaseDictionary.TryGetValue(connectionName, out var dbDict)
            || !dbDict.TryGetValue(tableInfo.DATABASE_ID, out var dbInfo))
        {
            return $"-- database for object {objectId} not found in schema";
        }
        string databaseName = dbInfo.DatabaseName;

        string definition = await Task.Run(() =>
        {
            if (!_connectionSessions.TryGetValue(connectionName, out var gdb))
                return "-- connection not available";
            using DbConnection connection = gdb.GetConnection(databaseName);
            connection.Open();

            using DbCommand cmd1 = connection.CreateCommand();
            cmd1.CommandText = NetezzaSystemSql.GetViewDefinitionLength(objectId);
            if (cmd1.ExecuteScalar() is null)
                return $"-- object {objectId} does not exist";

            using DbCommand cmd2 = connection.CreateCommand();
            cmd2.CommandText = NetezzaSystemSql.GetViewDefinitionByObjectId(objectId);
            return cmd2.ExecuteScalar() as string ?? string.Empty;
        });

        var input = LegacyDdlSchemaAdapter.BuildViewInput(
            databaseRuntimeContext,
            _schemaTables,
            connectionName,
            objectId,
            definition);
        var sb = new StringBuilder();
        _ddlBuilder.AppendCreateView(sb, input);
        return sb.ToString();
    }

    public async Task<string> GetExternaTableCode(AppBase.Common.Interfaces.IDatabaseRuntimeContext databaseRuntimeContext, int OBJECT_ID, string connectionName, bool force = false)
    {
        if (!BaseTableDictionary.TryGetValue(connectionName, out var baseTables)
            || !baseTables.TryGetValue(OBJECT_ID, out var tableInfo))
        {
            return "-- object not found in schema";
        }
        if (!databaseRuntimeContext.DatabaseDictionary.TryGetValue(connectionName, out var dbDict)
            || !dbDict.TryGetValue(tableInfo.DATABASE_ID, out var dbInfo))
        {
            return "-- database not found in schema";
        }
        string databaseName = dbInfo.DatabaseName;
        if (!_connectionSessions.TryGetValue(connectionName, out var generalDb))
        {
            return "-- connection not available";
        }

        NetezzaExternalTableOptions options;
        try
        {
            options = await LegacyExternalOptionsLoader.LoadAsync(generalDb, databaseName, OBJECT_ID, force);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "External table script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            options = new NetezzaExternalTableOptions();
        }

        var input = LegacyDdlSchemaAdapter.BuildExternalInput(
            databaseRuntimeContext,
            _schemaTables,
            connectionName,
            OBJECT_ID,
            options);
        var sb = new StringBuilder();
        _ddlBuilder.AppendCreateExternal(sb, input);
        return sb.ToString();
    }
}
