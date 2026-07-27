using AppBase.Common.Configuration;

namespace AppBase.Common.Interfaces;

/// <summary>
/// Read-only state needed by the Netezza autocomplete fallback.
/// </summary>
public interface INetezzaCompletionContext
{
    bool SchemaRefreshed { get; }
    string SelectedConnectionName { get; }
    string SelectedDatabase { get; }

    IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>> DatabaseDictionary { get; }
    IReadOnlyDictionary<string, List<NetezzaColumnInfoRow>> ColumnTablesDictionary { get; }
    IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>> DatabaseSchemaLookup { get; }
    IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, string>>> DatabaseOwners { get; }
}
