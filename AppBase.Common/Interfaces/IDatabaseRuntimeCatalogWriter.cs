using AppBase.Common.Configuration;

namespace AppBase.Common.Interfaces;

/// <summary>
/// Explicit mutation port for provider schema refresh. Read consumers use
/// <see cref="IDatabaseRuntimeContext"/> and never receive these dictionaries.
/// </summary>
public interface IDatabaseRuntimeCatalogWriter : IDatabaseRuntimeContext
{
    void ClearDatabaseConnection(string connectionName);
    void SetDatabase(string connectionName, int databaseId, DatabaseInfo database);
    void EnsureBaseTableConnection(string connectionName, int databaseId);
    void ClearBaseTableConnection(string connectionName);
    void AddBaseTable(string connectionName, int databaseId, int tableId);
    void SetColumnTable(string connectionName, List<NetezzaColumnInfoRow> columns);
    void SetColumnTableValue(string connectionName, int columnId, NetezzaColumnInfoRow column);
    void SetSchemaLookup(string connectionName, Dictionary<string, Dictionary<string, (string owner, int tableId)>> lookup);
    void SetOwners(string connectionName, Dictionary<string, Dictionary<string, string>> owners);
    void SetTableDescription(string connectionName, string databaseName, int tableId, string? description);
    IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>> GetDatabaseSnapshot();
}
