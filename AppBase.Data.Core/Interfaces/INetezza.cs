using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Models;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace AppBase.Data.Core.Interfaces;

public interface INetezza : IDatabaseDownloader
{
    int DatabasesCount { get; set; }
    public Dictionary<int, string> DatabaseIdToName { get; set; }
    Dictionary<string, List<(string name, string database, string DEFINITION, string DESCRIPTION)>> ProcCache { get; set; }
    Dictionary<string, List<(string name, string database, string DEFINITION, string DESCRIPTION)>> ViewCache { get; set; }
    Dictionary<string, List<(string name, string database, string extobjname, string DESCRIPTION)>> ExternalCache { get; set; }

    Dictionary<string, List<(string name, string database, string refobjname, string DESCRIPTION)>> SynonymCache { get; set; }
    Dictionary<int, List<(string keyName, char keyType, Int16 columnPosition, string columnName, int? refTableId, string? refColumnName, string? UPDT_TYPE, string? DEL_TYPE)>> keysInTables
    { get; init; }
    Dictionary<string, DateTime> AttachedDbsToSchema { get; init; }
    void DoCsvOrXlsxExport(string runCommand, ISqlExecutionLog log, Stopwatch st);
    Task<bool> DownloadSchemaNetezza(string connectionName, NetezzaRefreshMode netezzaRefresh, List<string> dbsToRefresh, bool loadSources = false, Action? showInUiExtra = null);
    DbConnection GetConnection();
    DbConnection GetConnection(string databaseName, bool usePool = true);
    List<string> GetTablesOfSchema(string schema);
    List<string> GroupsList();

    Task ImportFromFile(Func<string, Encoding> getEncoding, Func<int, string> getName, Func<string[], List<string>> getTabs, IImportExportTasks imp, string filePath, IImportProgressForm f, string db, List<string> tableName, List<string> tabs, int skipRows = 0, bool silent = false);
    bool IsDbInProgress(string db);
    Task LoadSourceTextCache();
    Task PerformImportFromText(char escapechar, char sep, IImportProgressForm f, string db, string SelectedConnectionName);
    Task PerformImportXmlAsync(IDataObject clipboard, char escapechar, char sep, IImportProgressForm f, string db);
    void ResetDynamicCollection();
    void ResetLists();
    string SearchInProcedureSource(string txtToSearch);
    string SearchInViewsSource(string txtToSearch);
}
