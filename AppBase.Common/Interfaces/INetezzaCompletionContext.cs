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

    Dictionary<string, Dictionary<int, DatabaseInfo>> DatabaseDictionary { get; }
    Dictionary<string, List<NetezzaColumnInfoRow>> ColumnTablesDictionary { get; }
    Dictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>> DatabaseSchemaLookup { get; }
    Dictionary<string, Dictionary<string, Dictionary<string, string>>> DatabaseOwners { get; }
}
