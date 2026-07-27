using AppBase.Common.Configuration;
using System.Text.RegularExpressions;

namespace AppBase.Common.Interfaces;

/// <summary>
/// Provides only the application state that database providers need while they
/// execute queries or refresh schema metadata. It excludes UI, file-watcher
/// and document-tab operations.
/// </summary>
public interface IDatabaseRuntimeContext
{
    Color LogErrorStdColor { get; }
    Regex RxExportCsvXlsx { get; }
    IApplicationConfig Config { get; }
    string ConfigDirectory { get; }

    IReadOnlyDictionary<string, Dictionary<int, DatabaseInfo>> DatabaseDictionary { get; }
    IReadOnlyDictionary<string, List<NetezzaColumnInfoRow>> ColumnTablesDictionary { get; }
    IReadOnlyDictionary<string, Dictionary<int, List<int>>> BaseTableConnections { get; }
    IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, (string owner, int tableId)>>> DatabaseSchemaLookup { get; }
    IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, string>>> DatabaseOwners { get; }
    IReadOnlyDictionary<string, Dictionary<string, Dictionary<int, string>>> DatabaseTableDescriptions { get; }
}
