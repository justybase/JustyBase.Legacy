using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Common.Models;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Models;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace AppBase.Data.Core.Core;

public interface IGeneralDb
{
    Color LogErrorStdColor { get; set; }
    DatabaseTypeEnum DatabaseType { get; }
    Task<string> GetCreateAliasTextAsync(string schemaTablename);
    Task<string> GetCreateSynonymTextAsync(string schemaTablename);
    Task<(string, string)> GetAliasDataAsync(string schema, string aliasName);
    Task<string[]> GetLinkedServerTablesAsync(string linkedServerName);
    Task<(string, string)> GetSynonymDataAsync(string schema, string aliasName);

    event Action<string>? NoticeEvent;
    void BlobQuery(string sql);
    string ConnectionName { get; set; }
    string ConnectionString { get; set; }
    List<string> DatabaseList { get; set; }
    string DefaultDatabaseName { get; set; }
    bool GetInitSchemaInProgress { get; }
    bool ImportNotifyEventAdded { get; set; }
    int NotifyAfter { get; set; }
    string Username { get; set; }
    Dictionary<string, Dictionary<string, TypeInDatabase>> objectInSchema { get; set; }
    IAutocompleteSuggestionStore AutocompleteSuggestions { get; }

    event Action<string>? OnImportNotify;

    Task AbortAsync(object o);
    void AddToCacheStandard(string dbName, string schema, string tablename);
    void DoCsvAdvanced(string runCommand, AdvancedExportOptions options);
    void DoCsvOrXlsxExport(string runCommand, ISqlExecutionLog log, Stopwatch st);
    void DoXlsxAdvanced(string runCommand, AdvancedExportOptions options);
    void Eksport(string sql, string filePath, string fileType);
    string[] GetColumns(string dbName, string schema, string tablename);
    (string[], string[], short[], string[]) GetColumnsEx(string dbName, string schema, string tablename);
    DbConnection GetConnection();
    DbConnection GetConnection(string databaseName, bool usePool = true);
    string[] GetConstraints(string dbName, string schema, string tablename);
    string GetCreateAllTablesText(string schema);
    string GetCreatePorcedureText(string schema, string viewName);
    Task<string> GetCreateProcedureText(string schemaTablename);
    string GetCreateTableText(string schemaTablename);
    string GetCreateTableText(string dbName, string schema, string tablename);
    string GetCreateViewText(string schemaTablename);
    string GetCreateViewText(string dbName, string schema, string viewName);
    (DbDataReader, DbConnection) GetDbDataReader(string sql);
    string[] GetIndexes(string dbName, string schema, string tablename);
    string[] GetPartitions(string dbName, string schema, string tablename);
    string GetSqlAddCode(string objectType, string db, string schema, string parentObject);
    List<string> GetTablesOfSchema(string schema);
    string[] GetTriggers(string dbName, string schema, string tablename);
    string GetViewCodeStandard(string schema, string tablename);
    void ImportedSomeRows(string rows);

    Task ImportFromFile(Func<string, Encoding> getEncoding,
        Func<int, string> getName,
        Func<string[], List<string>> getTabs,
        IImportExportTasks imp, string filePath, IImportProgressForm f, string db, List<string> tableName, List<string> tabs, int skipRows = 0, bool silent = false);

    void InitDb();
    Task PerformImportFromText(char escapechar, char sep, IImportProgressForm f, string db, string SelectedConnectionName);
    Task PerformImportXmlAsync(IDataObject clipboard, char escapechar, char sep, IImportProgressForm f, string db);
    void ResetDbName(string connectionName, string dbName);
    void ResetDynamicCollection();
    (DbConnection conn, string res) RunScalarSql(string sql);
    void RunSqlNoResults(string sql);
    string SearchInProcedureSource(string txtToSearch);
    string SearchInViewsSource(string txtToSearch);
}
