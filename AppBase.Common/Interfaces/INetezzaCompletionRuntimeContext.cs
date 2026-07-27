using AppBase.Common.Configuration;

namespace AppBase.Common.Interfaces;

/// <summary>
/// Mutable shell state required while a connection/schema selection is updated.
/// </summary>
public interface INetezzaCompletionRuntimeContext : INetezzaCompletionContext
{
    new bool SchemaRefreshed { get; set; }
    new string SelectedConnectionName { get; set; }
    new string SelectedDatabase { get; set; }
    void ReplaceDatabaseDictionary(Dictionary<string, Dictionary<int, DatabaseInfo>> value);
    void ClearDatabaseDictionary();
    void ClearSchemaLookup(string connectionName);
    void ClearDatabaseOwners(string connectionName);
}
