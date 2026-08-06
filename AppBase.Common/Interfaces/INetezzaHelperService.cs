using System.Text;

using AppBase.Common.Interfaces;

namespace AppBase.Common;

public interface INetezzaHelperService
{
    /// <summary>Last observed Netezza server version from a schema download.</summary>
    string ServerVersion { get; set; }

    /// <summary>True while a Netezza schema/SQLite cache refresh is in progress.</summary>
    bool SqliteInProgress { get; set; }

    string GetDescSql(string dbName);
    string NzProcReturnFix(string procReturns);
    void OnSchemaProblemNetezzaAskForRestart(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, string connectionName, Action action);
    string ExternalSql(string database);
    string ProcSql(string database);
    string SynonymSql(string database);
    string ViewSql(string database);
    ValueTask<NzGetTableCodeResult> GetTableCodeById(
        StringBuilder stringBuilder,
        IDatabaseRuntimeContext databaseRuntimeContext,
        string connectionName, int objectID, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null,
        bool forceNotOnline = false);
    ValueTask<NzGetTableCodeResult> GetRecreateTableCodeById(
        IDatabaseRuntimeContext databaseRuntimeContext,
        string connectionName, int objectID, List<string>? distOverride = null);
    Task<string> GetAllTablesDdlAsync(string connectionName, string databaseName, CancellationToken cancellationToken = default);
    Task<string> GetExternaTableCode(IDatabaseRuntimeContext databaseRuntimeContext, int OBJECT_ID, string connectionName, bool force = false);
    Task<string> GetViewCodeById(IDatabaseRuntimeContext databaseRuntimeContext, int objectId, string connectionName);
    void Initialize(INetezzaSchemaRefreshHost schemaRefreshHost);
}

public sealed class NzGetTableCodeResult
{
    private readonly StringBuilder _stringBuilder;
    public NzGetTableCodeResult(StringBuilder stringBuilder)
    {
        _stringBuilder = stringBuilder;
    }
    public string Code => field ??= _stringBuilder.ToString();
    public List<(byte, string)> Dystr { get; init; } = [];
    public List<(byte, string)> OrganizeList { get; init; } = [];
}






