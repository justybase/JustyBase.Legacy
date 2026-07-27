using AppBase.Data.Core.Models;

namespace AppBase.Data.Core.Interfaces;

/// <summary>
/// Injected catalog of Netezza table metadata keyed by connection name.
/// Prefer this over process-wide static catalogs so tests can
/// isolate schema state without mutating shared globals.
/// </summary>
public interface INetezzaSchemaTableCatalog
{
    IReadOnlyDictionary<string, Dictionary<int, NetezzaTableInfo>> TablesByConnection { get; }
}

/// <summary>Write port for the schema catalog refresh pipeline.</summary>
public interface INetezzaSchemaTableCatalogWriter : INetezzaSchemaTableCatalog
{
    void ClearConnection(string connectionName);
    void ReplaceConnection(string connectionName, Dictionary<int, NetezzaTableInfo> tables);
    void SetTable(string connectionName, int tableId, NetezzaTableInfo table);
}
