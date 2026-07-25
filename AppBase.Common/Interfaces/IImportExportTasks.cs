using AppBase.Common.Configuration;
using AppBase.Common.Models;
using DatabaseDataGridView.WinForms.Interfaces;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace AppBase.Common.Interfaces;

public interface IImportExportTasks : IExportMakes
{
    int SkipRows { get; set; }
    Dictionary<string, (string path, Dictionary<int, string> headersDic, int RowsCount)> TabsTablesColumns { get; set; }
    string[] SheetNames { get; set; }
    void ChooseTypes(Dictionary<int, Dictionary<AppBase.Common.DatabaseColumnType, int[]>> typesCount, string[] headers);
    void DBReaderStreamPipeServer(DbDataReader rdr, string serverName, Action<int> act, int progressSize = 10000);
    void DisposeFile();
    long ExportCSVReader(Encoding enc, IDataReader rdr, string csvPath, string colSep = ";", bool useSytemNewline = true, string? NewLine = null, Action<long>? action = null, bool ms = true, bool header = true);

    void ExportXlsxReader(IDataReader rdr, string xlsxPath, string sql, IApplicationConfig config);
    void ExportToExistingXlsxReader(IDataReader rdr, AdvancedExportOptions options);
    void FileStreamPipeServer(string path, string serverName, IImportProgressForm? form, int RowCounts, string newline = "\r\n");
    DataTable GetDataTableFromClipboard(IDataObject clipboard, char escapechar, char sep, bool TypesFromFirstRow);
    string[] GetHeaders(DbDataReader rdr, string? selCon = null);
    string ImportAction(IImportProgressForm? form, KeyValuePair<string, (string path, Dictionary<int, string> headersDic, int RowsCount)> item, DbConnection dbConnection, IApplicationConfig config, string configDirecotry, string? preferedName = null, bool importToExisting = false, string? importDatabaseName = null, string? importSchemaName = null);
    void LinesPipeServer(string[] lines, string serverName, IImportProgressForm? form);
    void MakeSilentCsvExport(string ConnectionString, string sql, string filePath, char sep = ';', bool useSytemNewline = true, Action<long>? action = null, ConnectionTypes connType = ConnectionTypes.odbc);
    void MakeSilentXlsxExport(string ConnectionString, string sql, string filePath, Action<int>? f = null, Action? onCompress = null, ConnectionTypes connType = ConnectionTypes.odbc);
    void ReadAndMakeTextFileNew(string filePath, string externalCsvPath, char columnDelim, IImportProgressForm? f, bool onlyFirstTab = true, string PreferedName = "", long rowLimit = 9223372036854775797, List<string>? tabs = null);
    void ReadAndMakeTextFileNewPart1(string filePath, string externalCsvPath, char columnDelim, IImportProgressForm? f, bool onlyFirstTab = true, string PreferedName = "", long rowLimit = 9223372036854775797, List<string>? tabs = null, Encoding? encoding = null);
    void ReadAndMakeTextFileNewPart2(string filePath, string externalCsvPath, char columnDelim, IImportProgressForm? f, bool onlyFirstTab = true, string PreferedName = "", long rowLimit = 9223372036854775797, List<string>? tabs = null);
    DataSet ReadFileAndMakeDataSet(string filePath, int skipRows, bool onlyFirst = true);
    void XlsxManyTabs(string filePath, IDataReader rdr, string sql, string tabName, bool suppresSomeData = true, Action<int>? on10k = null, Action? onCompress = null);
    void DoXlsxTxtImportFromCodeAsync(IApplicationSettingsContext applicationSettingsContext, string ConnectionString, string importComand, string configDirecotry, IApplicationConfig config, ISqlExecutionLog log, Stopwatch st, bool silent = false);
}
