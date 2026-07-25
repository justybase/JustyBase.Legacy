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
    new Dictionary<string, Dictionary<int, DatabaseInfo>> DatabaseDictionary { get; set; }
}
