using AppBase.Common;
using AppBase.Common.Interfaces;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>
/// Adapts catalog state to the small DDL contract consumed by the hint window.
/// </summary>
public sealed class LegacyNetezzaDdlCodeProvider : INetezzaDdlCodeProvider
{
    private readonly INetezzaHelperService _netezzaHelperService;
    private readonly INetezzaCompletionContext _completionContext;
    private readonly IDatabaseRuntimeContext _databaseRuntimeContext;

    public LegacyNetezzaDdlCodeProvider(
        INetezzaHelperService netezzaHelperService,
        INetezzaCompletionContext completionContext,
        IDatabaseRuntimeContext databaseRuntimeContext)
    {
        _netezzaHelperService = netezzaHelperService ?? throw new ArgumentNullException(nameof(netezzaHelperService));
        _completionContext = completionContext ?? throw new ArgumentNullException(nameof(completionContext));
        _databaseRuntimeContext = databaseRuntimeContext ?? throw new ArgumentNullException(nameof(databaseRuntimeContext));
    }

    public async Task<string> GetTableCodeByName(string database, string name, string? connectionName = null)
    {
        connectionName ??= _completionContext.SelectedConnectionName;
        if (!_completionContext.DatabaseSchemaLookup.TryGetValue(connectionName, out var byDatabase)
            || !byDatabase.TryGetValue(database, out var byObject)
            || !byObject.TryGetValue(name, out var objectInfo))
        {
            return string.Empty;
        }

        return (await _netezzaHelperService
            .GetTableCodeById(null, _databaseRuntimeContext, connectionName, objectInfo.tableId))
            .Code;
    }

    public async Task<string> GetRecreateTableCodeByName(string database, string name, string? connectionName = null)
    {
        connectionName ??= _completionContext.SelectedConnectionName;
        if (!_completionContext.DatabaseSchemaLookup.TryGetValue(connectionName, out var byDatabase)
            || !byDatabase.TryGetValue(database, out var byObject)
            || !byObject.TryGetValue(name, out var objectInfo))
        {
            return string.Empty;
        }

        return (await _netezzaHelperService
            .GetRecreateTableCodeById(_databaseRuntimeContext, connectionName, objectInfo.tableId))
            .Code;
    }

    public async Task<string> GetExternaTableCodeByName(string database, string name, string connectionName)
    {
        connectionName ??= _completionContext.SelectedConnectionName;
        int objectId = _completionContext.DatabaseSchemaLookup[connectionName][database][name].tableId;
        string externalSql = await _netezzaHelperService
            .GetExternaTableCode(_databaseRuntimeContext, objectId, connectionName);

        if (externalSql.EndsWith("_problem", StringComparison.Ordinal))
        {
            externalSql = await _netezzaHelperService
                .GetExternaTableCode(_databaseRuntimeContext, objectId, connectionName, force: true);
        }

        return externalSql;
    }
}
