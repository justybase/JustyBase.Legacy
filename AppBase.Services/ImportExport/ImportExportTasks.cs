#if INCLUDE_DB2
using IBM.Data.Db2;
#endif
#if INCLUDE_ORACLE
using Oracle.ManagedDataAccess.Client;
#endif
using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Interfaces;
using AppBase.Common.JsonContext;
using AppBase.Common.Models;
using AppBase.Data;
using JustyBase.ImportExport.Import;
using JustyBase.NetezzaDriver;
using JustyBase.NetezzaDdl;
using SpreadSheetTasks;
using Sylvan.Data.Csv;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace AppBase.Services;

public sealed partial class ImportExportTasks : IImportExportTasks
{
    public Dictionary<string, (string path, Dictionary<int, string> headersDic, int RowsCount)> TabsTablesColumns { get; set; }

    private const int _minimumNumericPrecision = 6;

    private static NumberFormatInfo _nfi = new NumberFormatInfo()
    {
        NumberDecimalSeparator = ".",
        NumberDecimalDigits = _minimumNumericPrecision
    };

    private readonly IApplicationSettingsContext _applicationSettingsContext;
    public ImportExportTasks(IApplicationSettingsContext applicationSettingsContext)
    {
        _applicationSettingsContext = applicationSettingsContext;
        TabsTablesColumns = new Dictionary<string, (string, Dictionary<int, string>, int)>();
    }

    IImportProgressForm _fx;
    public int SkipRows { get; set; }
    public string[] SheetNames { get; set; }
    private ExcelReaderAbstract _fileToImport;

    #region Database Connection Helpers



    private DbConnection CreateConnectionByType(ConnectionTypes connType, string connectionString)
    {
        return connType switch
        {
            ConnectionTypes.dotnet => new NzConnection(connectionString),
            _ => throw new ArgumentException($"Unsupported connection type: {connType}")
        };
    }

    #endregion

    #region Progress and Buffer Management

    private void UpdateProgress(IImportProgressForm form, long currentRow, long totalRows)
    {
        if (currentRow % 1_000 == 0 || currentRow == totalRows - 1)
        {
            if (totalRows != 123123124 && form is not null)
            {
                form?.SetProgressBarValue((int)(100 * currentRow / totalRows));
            }
            else
            {
                int progressPercent = Convert.ToInt32(100 * _fileToImport.RelativePositionInStream());
                form?.SetProgressBarValue(progressPercent > 100 ? 100 : progressPercent);
            }
        }
    }

    private void FlushBufferIfNeeded(StreamWriter writer, char[] buffer, ref int position, int threshold = 8192)
    {
        if (position > threshold)
        {
            writer.Write(buffer.AsSpan().Slice(0, position));
            position = 0;
        }
    }

    private void WriteDelimiterAndNewline(char[] buffer, ref int position, char columnDelim, int currentColumn, int totalColumns)
    {
        if (currentColumn < totalColumns - 1)
        {
            buffer[position++] = columnDelim;
        }
        else
        {
            buffer[position++] = '\n';
        }
    }

    #endregion

    #region String Processing Helpers

    private string ProcessStringForCsv(string stringValue, char escape, char columnDelim)
    {
        return TabularTextExporter.EscapeCsvField(stringValue, columnDelim);
    }

    private void WriteStringToBuffer(string val, char[] buffer, ref int position)
    {
        for (int i = 0; i < val.Length; i++)
        {
            buffer[position++] = val[i];
        }
    }

    #endregion

    #region Data Type Processing

    private void ProcessDataValue(ref SpreadSheetTasks.FieldInfo nativeVal, char[] localBuffer, ref int position,
        bool firstTime, string[] headers, int columnIndex, char columnDelim, char escape,
        out DatabaseColumnType columnType, out int length)
    {
        columnType = DatabaseColumnType.noinfo;
        length = -1;
        Span<char> tempBuffer = stackalloc char[64];

        switch (nativeVal.type)
        {
            case ExcelDataType.Null:
                columnType = DatabaseColumnType.noinfo;
                break;

            case ExcelDataType.Int64:
                columnType = DatabaseColumnType.integer;
                nativeVal.int64Value.TryFormat(localBuffer.AsSpan(position), out int intLength, default, _nfi);
                position += intLength;
                break;

            case ExcelDataType.Double:
                ProcessDoubleValue(nativeVal.doubleValue, tempBuffer, localBuffer, ref position, out columnType, out length);
                break;

            case ExcelDataType.DateTime:
                columnType = DatabaseColumnType.timestamp;
                nativeVal.dtValue.TryFormat(localBuffer.AsSpan().Slice(position), out int dateLength, "yyyy-MM-dd HH:mm:ss");
                position += dateLength;
                break;

            case ExcelDataType.String:
                ProcessStringValue(nativeVal.strValue as string, firstTime, headers, columnIndex,
                    localBuffer, ref position, escape, columnDelim, out columnType, out length);
                break;

            case ExcelDataType.Boolean:
                localBuffer[position++] = (char)nativeVal.int64Value;
                columnType = DatabaseColumnType.boolean;
                break;

            case ExcelDataType.Error:
                columnType = DatabaseColumnType.noinfo;
                break;

            default:
                columnType = DatabaseColumnType.noinfo;
                break;
        }
    }

    private void ProcessDoubleValue(double value, Span<char> tempBuffer, char[] localBuffer, ref int position,
        out DatabaseColumnType columnType, out int length)
    {
        value.TryFormat(tempBuffer, out int written, "F6", _nfi);
        int dotIndex = tempBuffer.IndexOf('.');

        if (dotIndex == -1 && written >= 12)
        {
            columnType = DatabaseColumnType.nvarchar;
            length = written;
        }
        else
        {
            columnType = DatabaseColumnType.numeric;
            length = dotIndex + _minimumNumericPrecision;
        }

        tempBuffer.Slice(0, written).CopyTo(localBuffer.AsSpan().Slice(position));
        position += written;
    }

    private void ProcessStringValue(string stringValue, bool firstTime, string[] headers, int columnIndex,
        char[] localBuffer, ref int position, char escape, char columnDelim,
        out DatabaseColumnType columnType, out int length)
    {
        if (!firstTime)
        {
            columnType = DatabaseColumnType.nvarchar;
            length = stringValue.Length;
            string processedValue = ProcessStringForCsv(stringValue, escape, columnDelim);
            WriteStringToBuffer(processedValue, localBuffer, ref position);
        }
        else
        {
            headers[columnIndex] = stringValue.NormalizeName(_applicationSettingsContext.Config.KeyWordsListForColoring1);
            WriteStringToBuffer(headers[columnIndex], localBuffer, ref position);
            columnType = DatabaseColumnType.noinfo;
            length = -1;
        }
    }

    private void UpdateTypeStatistics(Dictionary<int, Dictionary<DatabaseColumnType, int[]>> typesCount,
        int columnIndex, DatabaseColumnType columnType, int length)
    {
        if (!typesCount.ContainsKey(columnIndex))
        {
            typesCount[columnIndex] = new Dictionary<DatabaseColumnType, int[]>();
        }
        if (!typesCount[columnIndex].ContainsKey(columnType))
        {
            typesCount[columnIndex][columnType] = new int[3];
        }

        typesCount[columnIndex][columnType][0]++; // count
        if (typesCount[columnIndex][columnType][1] < length)
        {
            typesCount[columnIndex][columnType][1] = length; // max length
        }
        typesCount[columnIndex][columnType][2] = _minimumNumericPrecision; // precision
    }

    #endregion

    #region Named Pipe Server Helpers

    private string GenerateServerName() => NetezzaPipeImportExecutor.CreatePipeName("pipe_sql");

    #endregion

    #region SQL Generation Helpers

    private string BuildCreateTableCommand(string tableName, string[] headers)
        => NetezzaImportSql.CreateRandomDistributionTable(tableName, headers);

    private string BuildExternalInsertCommand(string tableName, string serverName, string[] headers, string remoteSource)
    {
        char sepInExternal = _applicationSettingsContext.Config.SepInExternal[0];

        return NetezzaImportSql.InsertFromExternalPipe(tableName, serverName, headers) +
               @$"USING(
                REMOTESOURCE '{remoteSource}'
                DELIMITER '{sepInExternal}'
                RecordDelim '\n'
                SKIPROWS 1
                NULLVALUE ''
                ENCODING 'utf-8'
                ESCAPECHAR '\'
                CTRLCHARS TRUE
                LFINSTRING TRUE
                MAXERRORS {_applicationSettingsContext.Config.ExternalMAXERRORS}
                LOGDIR '{_applicationSettingsContext.ConfigDirectory}\\data\\'
                );";
    }

    #endregion

    #region Excel Writer Helpers

    private ExcelWriter CreateExcelWriter(string filePath, bool suppressSomeData = true)
    {
        ExcelWriter writer = filePath.EndsWith("xlsb", StringComparison.OrdinalIgnoreCase)
            ? new XlsbWriter(filePath)
            : new XlsxWriter(filePath);

        writer.SuppressYear1000Dates = suppressSomeData;
        return writer;
    }

    private void WriteExcelWithSqlSheet(ExcelWriter excelWriter, IDataReader reader, string sql, string tabName)
    {
        int sheetCounter = 0;
        do
        {
            if (reader.FieldCount > 0)
            {
                excelWriter.AddSheet($"{tabName}{++sheetCounter}");
                excelWriter.WriteSheet(reader);
            }
        } while (reader.NextResult());

        excelWriter.AddSheet("SQL", hidden: true);
        excelWriter.WriteSheet(StringExtension.Sqlparts(sql));
    }

    #endregion

    #region File Reader Helpers

    private static ExcelReaderAbstract CreateFileReader(string filePath)
    {
        if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("The .xls format is no longer supported. Use .xlsx or .xlsb instead.");

        if (filePath.EndsWith("xlsx", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith("xlsb", StringComparison.OrdinalIgnoreCase))
            return new XlsxOrXlsbReadOrEdit();
        return new CsvReader();
    }

    private static void DisposeReader(ExcelReaderAbstract reader)
    {
        try
        {
            reader?.Dispose();
        }
        catch (Exception exception)
        {
            Trace.WriteLine($"Import reader cleanup failed: {exception.GetType().Name}");
        }
    }

    private bool ShouldSkipFirstRow(bool firstTime)
    {
        while (_fileToImport is not CsvReader && firstTime && _fileToImport.GetValue(0) == null && SkipRows == 0)
        {
            _fileToImport.Read();
            return true;
        }
        return false;
    }

    private bool IsValidHeader(object val)
    {
        return Regex.IsMatch(val.ToString(), @"^[a-z]", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(val.ToString(), @"^""[a-z]", RegexOptions.IgnoreCase);
    }

    private void GenerateAutoHeaders(string[] headers, char[] localBuffer, ref int position, char columnDelim)
    {
        for (int j = 0; j < headers.Length; j++)
        {
            headers[j] = $"COLUMM_AUTONAME_{j + 1}";
            WriteStringToBuffer(headers[j], localBuffer, ref position);

            if (j < headers.Length - 1)
            {
                localBuffer[position++] = columnDelim;
            }
        }
        localBuffer[position++] = '\n';
    }

    #endregion

    // MAIN METHODS

    public void ReadAndMakeTextFileNewPart1(string filePath, string externalCsvPath, char columnDelim, IImportProgressForm f, bool onlyFirstTab = true, string PreferedName = "", long rowLimit = Int64.MaxValue - 10, List<string> tabs = null, Encoding encoding = null)
    {
        _fx = f;
        f?.AddRow("Opening...");

        _fileToImport = CreateFileReader(filePath);
        try
        {
            _fileToImport.Open(filePath, true, encoding: encoding);
            SheetNames = _fileToImport.GetSheetNames();
        }
        catch (Exception)
        {
            DisposeReader(_fileToImport);
        }
    }

    public void DisposeFile()
    {
        DisposeReader(_fileToImport);
    }

    public void ReadAndMakeTextFileNewPart2(string filePath, string externalCsvPath, char columnDelim, IImportProgressForm f, bool onlyFirstTab = true, string PreferedName = "", long rowLimit = Int64.MaxValue - 10, List<string> tabs = null)
    {
        try
        {
            f?.AddRow($"{_fileToImport.ResultsCount} sheets");

            foreach (var sheetName in SheetNames)
            {
                _fileToImport.ActualSheetName = sheetName;
                if (!string.IsNullOrWhiteSpace(PreferedName) && _fileToImport.ActualSheetName != PreferedName)
                {
                    continue;
                }
                if (tabs != null && !tabs.Contains(_fileToImport.ActualSheetName))
                {
                    continue;
                }

                f?.SetProgressBarValue(0);

                if (!filePath.EndsWith("csv", StringComparison.OrdinalIgnoreCase) && !filePath.EndsWith("txt", StringComparison.OrdinalIgnoreCase))
                {
                    _fileToImport.Read();
                }

                using StreamWriter sw = new StreamWriter($"{externalCsvPath}_{_fileToImport.ActualSheetName}", false, Encoding.UTF8, bufferSize: 65_536);
                var headersDic = new Dictionary<int, string>();
                int colNum = _fileToImport.FieldCount;
                int rowNum = _fileToImport.RowCount + 1;
                string[] headers = new string[colNum];
                TabsTablesColumns[_fileToImport.ActualSheetName] = ($"{externalCsvPath}_{_fileToImport.ActualSheetName}", headersDic, rowNum);

                Dictionary<int, Dictionary<DatabaseColumnType, int[]>> typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>();

                bool firstTime = true;
                char escape = '\\';
                int position = 0;
                int localBufferLen = 65_536;
                char[] localBuffer = new char[localBufferLen];

                f?.AddRow($"{_fileToImport.ActualSheetName}: rows {rowNum} cols {colNum}");
                f?.AddRow($"{_fileToImport.ActualSheetName}: analysing..");

                int toSkip = 0;
                while (toSkip++ < SkipRows)
                {
                    _fileToImport.Read();
                }

                long l = 1;
                while ((firstTime || _fileToImport.Read()) && l < rowLimit + 2)
                {
                    UpdateProgress(f, l++, rowNum);
                    ShouldSkipFirstRow(firstTime);

                    for (int i = 0; i < colNum; i++)
                    {
                        ref var nativeVal = ref _fileToImport.GetNativeValue(i);

                        if (firstTime)
                        {
                            Type typ = _fileToImport.GetFieldType(i);
                            Object val = _fileToImport.GetValue(i);
                            if (_fileToImport is CsvReader)
                            {
                                typ = typeof(string);
                                val = _fileToImport.GetName(i);
                            }

                            if (val is not string || string.IsNullOrWhiteSpace((string)val))
                            {
                                firstTime = false;
                                GenerateAutoHeaders(headers, localBuffer, ref position, columnDelim);
                                val = _fileToImport.GetValue(i);
                                typ = _fileToImport.GetFieldType(i);
                            }

                            if (i == 0 && !IsValidHeader(val))
                            {
                                firstTime = false;
                                GenerateAutoHeaders(headers, localBuffer, ref position, columnDelim);
                                val = _fileToImport.GetValue(i);
                                typ = _fileToImport.GetFieldType(i);
                            }
                        }

                        ProcessDataValue(ref nativeVal, localBuffer, ref position, firstTime, headers, i, columnDelim, escape, out DatabaseColumnType nz, out int len);

                        WriteDelimiterAndNewline(localBuffer, ref position, columnDelim, i, colNum);
                        UpdateTypeStatistics(typesCount, i, nz, len);
                    }

                    FlushBufferIfNeeded(sw, localBuffer, ref position);
                    firstTime = false;
                }

                if (position > 0)
                {
                    sw.Write(localBuffer.AsSpan().Slice(0, position));
                    position = 0;
                }

                sw.Close();
                localBuffer = null;

                f?.SetProgressBarValue(100);
                ChooseTypes(typesCount, headers);
                for (int i = 0; i < _fileToImport.FieldCount; i++)
                {
                    headersDic[i] = headers[i];
                }
            }
        }
        finally
        {
            _fileToImport.Dispose();
        }
    }

    public void ReadAndMakeTextFileNew(string filePath, string externalCsvPath, char columnDelim, IImportProgressForm f, bool onlyFirstTab = true, string PreferedName = "", long rowLimit = Int64.MaxValue - 10, List<string> tabs = null)
    {
        ReadAndMakeTextFileNewPart1(filePath, externalCsvPath, columnDelim, f, onlyFirstTab, PreferedName, rowLimit, tabs);
        ReadAndMakeTextFileNewPart2(filePath, externalCsvPath, columnDelim, f, onlyFirstTab, PreferedName, rowLimit, tabs);
    }


    public DataSet ReadFileAndMakeDataSet(string filePath, int skipRows, bool onlyFirst = true)
    {
        DataSet result = new DataSet();
        var reader = CreateFileReader(filePath);
        try
        {
            reader.Open(filePath);
            foreach (var sheetName in reader.GetSheetNames())
            {
                reader.ActualSheetName = sheetName;
                if (!filePath.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                }
                DataTable dt = new DataTable(sheetName);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dt.Columns.Add(reader.GetName(i), typeof(string));
                }
                object[] row = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(row);
                    dt.Rows.Add(row);
                }

                result.Tables.Add(dt);
                if (onlyFirst) break;
            }
        }
        finally
        {
            DisposeReader(reader);
        }

        return result;
    }

    private void startTask()
    {
        progressBarUp(0);
    }

    private void progressBarUp(int arg1)
    {
        _fx?.SetProgressBarValue(arg1 > 100 ? 100 : arg1);
    }

    /// <summary>Host adapter over <see cref="NetezzaPipeImportExecutor.ServeRawLinesAsync"/>.</summary>
    public void LinesPipeServer(string[] lines, string serverName, IImportProgressForm form)
    {
        int pos = Math.Max(10, lines.Length / 100);
        form?.SetProgressBarValue(0);

        _ = NetezzaPipeImportExecutor.ServeRawLinesAsync(
                EnumerateNonEmptyLines(lines),
                serverName,
                progress: i =>
                {
                    if (lines.Length > 0)
                        form?.SetProgressBarValue(Math.Min(100, (int)(100 * i / lines.Length)));
                },
                progressEvery: pos)
            .ContinueWith(
                _ => form?.AddRow("database processing...", (int)ProgressBarStyle.Marquee),
                TaskScheduler.Default);
    }

    /// <summary>
    /// Host adapter over <see cref="NetezzaPipeImportExecutor.ServeRawLinesAsync"/>.
    /// <paramref name="newline"/> is ignored — shared executor always emits LF (EXTERNAL RecordDelim '\n').
    /// </summary>
    public void FileStreamPipeServer(string path, string serverName, IImportProgressForm form, int RowCounts, string newline = "\r\n")
    {
        _ = newline; // retained for IImportExportTasks signature compatibility
        int pos = Math.Max(10, RowCounts > 0 && RowCounts != 123123124 ? RowCounts / 100 : 10_000);
        form?.SetProgressBarValue(0);

        _ = NetezzaPipeImportExecutor.ServeRawLinesAsync(
                ReadTrimmedFileLines(path),
                serverName,
                progress: i =>
                {
                    if (RowCounts != 123123124 && RowCounts > 0)
                        form?.SetProgressBarValue(Math.Min(100, (int)(100 * i / RowCounts)));
                    else if (i % 10_000 == 0)
                        form?.SetProgressBarValue(Math.Min(99, (int)(i / 10_000)));
                },
                progressEvery: pos)
            .ContinueWith(
                _ => form?.AddRow("database processing...", (int)ProgressBarStyle.Marquee),
                TaskScheduler.Default);
    }

    /// <summary>Host adapter over <see cref="NetezzaPipeImportExecutor.ServeDataReaderAsync"/>.</summary>
    public void DBReaderStreamPipeServer(DbDataReader rdr, string serverName, Action<int> act, int progressSize = 10_000)
    {
        char sepInExternal = _applicationSettingsContext.Config.SepInExternal[0];
        _ = NetezzaPipeImportExecutor.ServeDataReaderAsync(
            rdr,
            serverName,
            delimiter: sepInExternal,
            rowProgress: rows => act?.Invoke((int)Math.Min(rows, int.MaxValue)),
            progressEvery: progressSize <= 0 ? 10_000 : progressSize);
    }

    private static async IAsyncEnumerable<string> EnumerateNonEmptyLines(string[] lines)
    {
        foreach (string line in lines)
        {
            if (line.Length == 0)
                continue;
            yield return line;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<string> ReadTrimmedFileLines(string path)
    {
        using var reader = new StreamReader(path);
        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
        {
            if (line.Length == 0)
                continue;
            yield return line.Trim();
        }
    }

    public string[] GetHeaders(DbDataReader rdr, string selCon = null)
    {
        string[] headers = new string[rdr.FieldCount];
        var tableSchema = rdr.GetSchemaTable();

        int i = 0;
        foreach (DataRow row in tableSchema.Rows)
        {
            string name = row.ItemArray[0] as string;
            Int16 numericPrecision;
            int numericScale;
            Type t = null;
            bool allowNull = false;

            if (selCon == "Oracle")
            {
                t = row.ItemArray[11] as Type;
                allowNull = (bool)row.ItemArray[13];
                numericPrecision = (row.ItemArray[3] == DBNull.Value ? Convert.ToInt16(row.ItemArray[2]) : (Int16)row.ItemArray[3]);
                numericScale = row.ItemArray[4] == DBNull.Value ? (Int16)0 : (Int16)row.ItemArray[3];
                if (numericScale > 8)
                {
                    numericScale = 8;
                }
            }
            else if (selCon == "NZ")
            {
                t = row.ItemArray[11] as Type;
                allowNull = (bool)row.ItemArray[12];
                numericPrecision = (Int16)(Int32)row.ItemArray[3];
                numericScale = (Int16)(Int32)row.ItemArray[4];
            }
            else
            {
                t = row.ItemArray[5] as Type;
                allowNull = (bool)row.ItemArray[8];
                numericPrecision = (Int16)row.ItemArray[3];
                numericScale = (Int16)row.ItemArray[4];
            }

            switch (t.Name)
            {
                case "String":
                    headers[i++] = $"{name} NVARCHAR({(numericPrecision > 0 ? numericPrecision : "255")})" + (!allowNull ? " NOT NULL" : "");
                    break;
                case "Int32":
                    headers[i++] = $"{name} INTEGER" + (!allowNull ? " NOT NULL" : "");
                    break;
                case "Int64":
                    headers[i++] = $"{name} BIGINT" + (!allowNull ? " NOT NULL" : "");
                    break;
                case "Decimal":
                    headers[i++] = $"{name} NUMERIC({numericPrecision},{numericScale})" + (!allowNull ? " NOT NULL" : "");
                    break;
                case "Double":
                    headers[i++] = $"{name} DOUBLE" + (!allowNull ? " NOT NULL" : "");
                    break;
                case "DateTime":
                    headers[i++] = $"{name} DATE" + (!allowNull ? " NOT NULL" : "");
                    break;
                case "Boolean":
                    headers[i++] = $"{name} BOOL" + (!allowNull ? " NOT NULL" : "");
                    break;
                default:
                    headers[i++] = $"{name} NVARCHAR(255)" + (!allowNull ? " NOT NULL" : "");
                    break;
            }
        }

        return headers;
    }

    public void ChooseTypes(Dictionary<int, Dictionary<DatabaseColumnType, int[]>> typesCount, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].EndsWith("_#TEXT"))
            {
                headers[i] += $" NVARCHAR({_applicationSettingsContext.Config.DefaultNvarcharLength})";
                continue;
            }
            else if (headers[i].EndsWith("_#NUMERIC"))
            {
                headers[i] += " NUMERIC(20,8)";
                continue;
            }
            else if (headers[i].EndsWith("_#INTEGER"))
            {
                headers[i] += " INTEGER";
                continue;
            }
            else if (headers[i].EndsWith("_#DATE"))
            {
                headers[i] += " DATE";
                continue;
            }
            else if (headers[i].EndsWith("_#TIMESTAMP"))
            {
                headers[i] += " TIMESTAMP";
                continue;
            }

            if (!typesCount.ContainsKey(i))
            {
                typesCount[i] = new Dictionary<DatabaseColumnType, int[]>();
                typesCount[i][DatabaseColumnType.nvarchar] = new int[3];
                typesCount[i][DatabaseColumnType.nvarchar][0] = 1;
                typesCount[i][DatabaseColumnType.nvarchar][1] = _applicationSettingsContext.Config.DefaultNvarcharLength;
            }

            if (!typesCount.ContainsKey(i))
            {
                headers[i] += $" NVARCHAR({_applicationSettingsContext.Config.DefaultNvarcharLength})";
                continue;
            }

            var bestChoiceTemp = typesCount[i].Where(arg => arg.Key != DatabaseColumnType.noinfo);

            if (bestChoiceTemp == null)
            {
                headers[i] += $" NVARCHAR({_applicationSettingsContext.Config.DefaultNvarcharLength})";
                continue;
            }

            // most popular type is Winner
            var bestChoice = bestChoiceTemp.OrderByDescending(arg => (arg.Value)[0]).FirstOrDefault();
            bool containNumeric = typesCount[i].ContainsKey(DatabaseColumnType.numeric);
            bool containNvarchar = typesCount[i].ContainsKey(DatabaseColumnType.nvarchar);
            bool containInteger = typesCount[i].ContainsKey(DatabaseColumnType.integer);
            bool containTimestamp = typesCount[i].ContainsKey(DatabaseColumnType.timestamp);

            if (containNvarchar)
            {
                int p = typesCount[i][DatabaseColumnType.nvarchar][1];
                int l = 0;
                if (containNumeric)
                {
                    l = typesCount[i][DatabaseColumnType.numeric][1];
                    if (l > p)
                    {
                        p = l;
                    }
                }
                if (containInteger)
                {
                    l = typesCount[i][DatabaseColumnType.integer][1];
                    if (l > p)
                    {
                        p = l;
                    }
                }
                if ((typesCount[i].ContainsKey(DatabaseColumnType.timestamp) || typesCount[i].ContainsKey(DatabaseColumnType.date)) && p < 50)
                {
                    p = 50;
                }
                headers[i] += $" NVARCHAR({(p == 1 ? 1 : p + 5)})";
            }
            else if (containNumeric && containTimestamp)
            {
                headers[i] += $" NVARCHAR({_applicationSettingsContext.Config.DefaultNvarcharLength})";
            }
            else if (containNumeric)
            {
                int a = typesCount[i][DatabaseColumnType.numeric][1];
                int b = typesCount[i][DatabaseColumnType.numeric][2];
                if (containInteger && typesCount[i][DatabaseColumnType.integer][1] + b > a)
                {
                    a = typesCount[i][DatabaseColumnType.integer][1] + b; // in column : 1,2,5.1,10 then 
                }
                if (a < b + 5)
                {
                    a = b + 5;
                }
                if (a < 10)
                {
                    a = 10;
                }
                if (containInteger && a < b + 16)
                {
                    a = b + 16;
                }

                headers[i] += $" NUMERIC({(a > 38 ? 38 : a)},{(b > 10 ? 10 : b)})";
            }
            else if (containTimestamp && containInteger)
            {
                headers[i] += $" NVARCHAR(50)";
            }
            else
            {
                switch (bestChoice.Key)
                {
                    case DatabaseColumnType.integer:
                        headers[i] += " BIGINT";
                        break;
                    case DatabaseColumnType.nvarchar:
                        headers[i] += $" NVARCHAR({bestChoice.Value[1]})";
                        break;
                    case DatabaseColumnType.numeric:
                        headers[i] += $" NUMERIC({bestChoice.Value[1]},{bestChoice.Value[2]})";
                        break;
                    case DatabaseColumnType.date:
                        headers[i] += " DATE";
                        break;
                    case DatabaseColumnType.timestamp:
                        headers[i] += " TIMESTAMP";
                        break;
                    case DatabaseColumnType.boolean:
                        headers[i] += " BOOL";
                        break;
                    default:
                        headers[i] += $" NVARCHAR({_applicationSettingsContext.Config.DefaultNvarcharLength})";
                        break;
                }
            }
        }
    }

    public void MakeSilentXlsxExport(string ConnectionString, string sql, string filePath, Action<int> f = null, Action onCompress = null, ConnectionTypes connType = ConnectionTypes.odbc)
    {
        using var connection = CreateConnectionByType(connType, ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60 * 60;

        using var reader = command.ExecuteReader();
        using var excelWriter = CreateExcelWriter(filePath);

        excelWriter.On10k += f;
        excelWriter.OnCompress += onCompress;

        WriteExcelWithSqlSheet(excelWriter, reader, sql, "data");
    }

    public void MakeSilentCsvExport(string ConnectionString, string sql, string filePath, char sep = ';', bool useSytemNewline = true, Action<long> action = null, ConnectionTypes connType = ConnectionTypes.odbc)
    {
        using var connection = CreateConnectionByType(connType, ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60 * 60;

        using var reader = command.ExecuteReader();
        ExportCSVReader(Encoding.UTF8, reader, filePath, sep.ToString(), useSytemNewline, null, action, ms: false);
    }

    public void XlsxManyTabs(string filePath, IDataReader rdr, string sql, string tabName, bool suppresSomeData = true, Action<int> on10k = null, Action onCompress = null)
    {
        using var excelFile = CreateExcelWriter(filePath, suppresSomeData);
        excelFile.On10k += on10k;
        excelFile.OnCompress += onCompress;

        WriteExcelWithSqlSheet(excelFile, rdr, sql, tabName);
    }

    public void ExportXlsxReader(IDataReader rdr, string xlsxPath, string sql, IApplicationConfig config)
    {
        try
        {
            using var excelFile = CreateExcelWriter(xlsxPath);
            excelFile.AddSheet("Sheet");
            excelFile.WriteSheet(rdr);
            excelFile.AddSheet("SQL", hidden: true);
            excelFile.WriteSheet(StringExtension.Sqlparts(sql));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Excel export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ExportToExistingXlsxReader(IDataReader rdr, AdvancedExportOptions options)
    {
        using (XlsxOrXlsbReadOrEdit xlsx = new XlsxOrXlsbReadOrEdit())
        {
            xlsx.Open(options.Path, readSharedStrings: false, updateMode: true);
            string excelRange = xlsx.ReplaceSheetData(options.TabName, rdr, startingCellAdress: options.StartCell);

            if (!string.IsNullOrWhiteSpace(options.PivotTableName))
            {
                xlsx.ReplacePivotTableDim(options.PivotTableName, excelRange, doRefreshOnLoad: true);
            }
        }
    }

    public long ExportCSVReader(Encoding enc, IDataReader rdr, string csvPath, string colSep = ";", bool useSytemNewline = true
        , string NewLine = null, Action<long> action = null, bool ms = true, bool header = true)
    {
        if (!useSytemNewline && NewLine != null)
        {
            if (NewLine != "\r" && NewLine != "\n" && NewLine != "\r\n")
            {
                throw new ArgumentException("Newline must be one of \"\r\" \"\\n\" \"\\r\\n\"", nameof(NewLine));
            }
        }
        if (_applicationSettingsContext.Config.DecimalDelimInCsv != ".")
        {
            NumberFormatInfo numberFormatInfo = new NumberFormatInfo()
            {
                NumberDecimalSeparator = _applicationSettingsContext.Config.DecimalDelimInCsv
            };
            CultureInfo ci = new CultureInfo(CultureInfo.CurrentCulture.LCID);
            ci.NumberFormat = numberFormatInfo;
        }
        long writeRows = -1;
        try
        {
            if (enc.EncodingName != "Unicode (UTF-8)" && _applicationSettingsContext.Config.EncondingName.ToLower() != "utf-8")
            {
                if (int.TryParse(_applicationSettingsContext.Config.EncondingName, out int codePage))
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    enc = Encoding.GetEncoding(codePage);
                }
                else
                    enc = Encoding.GetEncoding(_applicationSettingsContext.Config.EncondingName);

                using var sw = new StreamWriter(new FileStream(csvPath, FileMode.Create, FileAccess.Write, FileShare.None, 16 * 4096, FileOptions.SequentialScan), enc);
                using var csvWriter = CsvDataWriter.Create(sw, new CsvDataWriterOptions()
                {
                    NewLine = NewLine ?? Environment.NewLine,
                    Delimiter = colSep[0],
                    WriteHeaders = header
                });
                writeRows = csvWriter.Write(new DBReaderWithMessages(rdr, action));
            }
            else
            {
                using var csvWriter = CsvDataWriter.Create(csvPath, new CsvDataWriterOptions()
                {
                    NewLine = NewLine ?? Environment.NewLine,
                    Delimiter = colSep[0],
                    WriteHeaders = header
                });
                writeRows = csvWriter.Write(new DBReaderWithMessages(rdr, action));
            }
        }
        catch (Exception ex)
        {
            if (ms)
            {
                MessageBox.Show(ex.Message, "CSV export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                throw;
            }
        }
        return writeRows;
    }

    public async Task<long> ExportCSVReaderFromDt(Encoding enc, DataTable dt, string csvPath, List<object[]> rows)
    {
        long returned = -1;
        using (IDataReader rdr = new ReaderFromList(dt, rows))
        {
            returned = await Task.Run(() => ExportCSVReader(System.Text.Encoding.UTF8, rdr, csvPath));
        }
        return returned;
    }

    public void ExportJsonReader(IDataReader rdr, string jsonPath)
    {
        try
        {
            using var f = new StreamWriter(jsonPath, false, Encoding.UTF8);
            TabularTextExporter.WriteJson(f, rdr);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "JSON export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public string ImportAction(IImportProgressForm form, KeyValuePair<string, (string path, Dictionary<int, string> headersDic, int RowsCount)> item
        , DbConnection dbConnection, IApplicationConfig config, string configDirecotry, string preferedName = null, bool importToExisting = false,
        string importDatabaseName = null, string importSchemaName = null)
    {
        string randName = StringExtension.RandomName(item.Key.NormalizeName(_applicationSettingsContext.Config.KeyWordsListForColoring1));

        if (preferedName != null)
        {
            randName = preferedName;
        }

        string externalPath = item.Value.path;
        char sepInExternal = config.SepInExternal[0];
        string[] headers = item.Value.headersDic.Values.ToArray();

        string serverName = GenerateServerName();

        FileStreamPipeServer(externalPath, serverName, form, item.Value.RowsCount, "\n");
        Thread.Sleep(100);

        DbCommand cmd = dbConnection.CreateCommand();

        if (!importToExisting)
        {
            string createCommand = BuildCreateTableCommand(randName, headers);
            cmd.CommandText = createCommand;
            cmd.CommandTimeout = config.CommandTimeout;
            if (form != null)
            {
                form.AddRow($"creating {randName}");
            }
            cmd.ExecuteNonQuery();
        }

        string REMOTESOURCE = dbConnection is NzConnection ? "dotnet" : "odbc";

        string insertCommand = NetezzaImportSql.InsertFromExternalPipe(randName, serverName, headers)
         + @$"USING(
                REMOTESOURCE '{REMOTESOURCE}'
                DELIMITER '{sepInExternal}'
                SKIPROWS 1
                NULLVALUE ''
                ENCODING 'utf-8'
                ESCAPECHAR '\'
                TIMESTYLE '24HOUR'
                CRinString True
                RecordDelim '\n'
                MAXERRORS {config.ExternalMAXERRORS}
                LOGDIR '{configDirecotry}\\data\\'
                );";

        form?.AddRow(@$"inserting into {randName} started");
        cmd.CommandText = insertCommand;
        cmd.ExecuteNonQuery();
        string qualifiedTable = randName;
        if (!string.IsNullOrWhiteSpace(importDatabaseName) && !string.IsNullOrWhiteSpace(importSchemaName))
        {
            qualifiedTable = $"{importDatabaseName}.{importSchemaName}.{randName}";
        }

        form.CompleteForNetezza(randName, configDirecotry, headers, importToExisting, qualifiedTable);

        File.Delete(externalPath);
        return randName;
    }

    private static readonly NumberStyles _style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowExponent;
    private static readonly CultureInfo _cultureUS = CultureInfo.CreateSpecificCulture("en-US");

    public DataTable GetDataTableFromClipboard(IDataObject clipboard, char escapechar, char sep, bool TypesFromFirstRow)
    {
        DataTable source = new DataTable();
        int actInd = -1;
        int cellNum = 0;
        int dataNum = 0;
        int colNum = 0;
        int rowNum = 0;
        int colNumber = 0;
        decimal decimalNumber = 0;
        object[] tempRow = null;
        DatabaseColumnType[] typesNames = null;
        XmlTextReader reader = new XmlTextReader((MemoryStream)clipboard.GetData("XML Spreadsheet"));

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Whitespace)
            {
                continue;
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                int actRow = rowNum;

                if (reader.Name == "Cell")
                {
                    cellNum++;
                    if (reader.HasAttributes)
                    {
                        string indS = reader.GetAttribute("ss:Index");
                        if (!String.IsNullOrEmpty(indS))
                        {
                            actInd = Int32.Parse(indS) - 1;// xml has indexes from 1
                        }
                        else
                        {
                            actInd = -1;
                        }
                    }
                    else
                    {
                        actInd = -1;
                    }
                }
                else if (reader.Name == "Data")
                {
                    dataNum++;
                    if (cellNum > dataNum)
                    {
                        for (int i = colNum; i < tempRow.Length && i < colNum + cellNum - dataNum; i++)
                        {
                            tempRow[i] = DBNull.Value;
                            source.Columns[i].DataType = typeof(string);
                            typesNames[i] = DatabaseColumnType.nvarchar;
                        }
                        colNum += (cellNum - dataNum); //cell wihout data situation =  <Cell />
                        cellNum = dataNum;
                    }

                    if (rowNum == 1 && reader.HasAttributes)
                    {
                        if (colNum == 0)
                        {
                            typesNames = new DatabaseColumnType[colNumber];
                        }

                        int cn = colNum;
                        if (actInd != -1 && actRow == rowNum)
                        {
                            cn = actInd;
                        }
                        for (int i = 0; i < actInd - colNum; i++)
                        {
                            source.Columns[colNum + i].DataType = typeof(string);
                            typesNames[colNum + i] = DatabaseColumnType.nvarchar;
                        }

                        string atr = (string)reader.GetAttribute(0);
                        switch (atr)
                        {
                            case "Number":
                                source.Columns[cn].DataType = typeof(decimal);
                                typesNames[cn] = DatabaseColumnType.numeric;
                                break;
                            case "DateTime":
                                source.Columns[cn].DataType = typeof(DateTime);
                                typesNames[cn] = DatabaseColumnType.timestamp;
                                break;
                            default:
                                source.Columns[cn].DataType = typeof(string);
                                typesNames[cn] = DatabaseColumnType.nvarchar;
                                break;
                        }
                    }

                    reader.Read();
                    string val = reader.Value;

                    if (rowNum == 0)
                    {
                        source.Columns[colNum++].ColumnName = $"{val}_{colNum}";
                    }
                    else
                    {
                        if (actInd != -1 && actRow == rowNum)
                        {
                            colNum = actInd;
                        }

                        if (colNum == 0)
                        {
                            if (tempRow == null)
                            {
                                tempRow = new object[colNumber];
                            }
                            else
                            {
                                source.Rows.Add(tempRow);
                            }
                        }

                        if (val == "")
                        {
                            tempRow[colNum] = DBNull.Value;
                        }
                        else
                        {
                            switch (typesNames[colNum])
                            {
                                case DatabaseColumnType.integer:
                                    if (Int64.TryParse(val, out var parsedInt64))
                                    {
                                        tempRow[colNum] = parsedInt64;
                                    }
                                    else
                                    {
                                        tempRow[colNum] = DBNull.Value;
                                    }
                                    break;
                                case DatabaseColumnType.numeric:
                                    if (Decimal.TryParse(val, _style, CultureInfo.CurrentCulture, out decimalNumber)
                                        || Decimal.TryParse(val, _style, _cultureUS, out decimalNumber))
                                    {
                                        tempRow[colNum] = decimalNumber;
                                    }
                                    else
                                    {
                                        tempRow[colNum] = DBNull.Value;
                                    }
                                    break;
                                case DatabaseColumnType.date:
                                case DatabaseColumnType.timestamp:
                                    tempRow[colNum] = DateTime.Parse(val);
                                    break;
                                case DatabaseColumnType.nvarchar:
                                case DatabaseColumnType.noinfo:
                                default:
                                    tempRow[colNum] = val;
                                    break;
                            }
                        }
                        colNum++;
                    }
                }
                else if (reader.Name == "Table")
                {
                    for (int i = 0; i < reader.AttributeCount; i++)
                    {
                        reader.MoveToAttribute(i);
                        if (reader.Name == "ss:ExpandedColumnCount")
                        {
                            colNumber = Int32.Parse(reader.Value);
                            for (int l1 = 0; l1 < colNumber; l1++)
                            {
                                source.Columns.Add();
                            }
                        }
                        else if (reader.Name == "ss:ExpandedRowCount")
                        {
                            //lines = new string[Int32.Parse(reader.Value)];
                        }
                    }
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Row")
            {
                cellNum = 0;
                dataNum = 0;
                rowNum++;
                colNum = 0;
            }
        }
        source.Rows.Add(tempRow);

        return source;
    }

    public async Task SaveAsXlsx(string xlsxPath, DataTable? dtExp = null, List<object[]>? rowsList = null, string? sql = null)
    {
        await Task.Run(() =>
        {
            using (IDataReader rdr = new ReaderFromList(dtExp, rowsList))
            {
                using var excelFile = CreateExcelWriter(xlsxPath);
                excelFile.AddSheet("Sheet");
                excelFile.WriteSheet(rdr, doAutofilter: true);

                if (sql is not null)
                {
                    excelFile.AddSheet("SQL", hidden: true);
                    excelFile.WriteSheet(StringExtension.Sqlparts(sql));
                }
            }
        });
    }

    public async Task ExportExcelAllTabsAsync(string xlsxPath, IEnumerable<(string title, DataTable dt, List<object[]> rows, string sql)> items)
    {
        if (items is null || !items.Any())
        {
            return;
        }
        try
        {
            await Task.Run(() =>
            {
                using var excelFile = CreateExcelWriter(xlsxPath);
                int i = 1;
                foreach (var myGrid in items)
                {
                    if (myGrid.dt is null)
                    {
                        continue;
                    }
                    excelFile.AddSheet(myGrid.title.NormalizeName([]));
                    using IDataReader rdr = new ReaderFromList(myGrid.dt, myGrid.rows);
                    excelFile.WriteSheet(rdr, doAutofilter: true);
                    excelFile.AddSheet($"SQL_{i}", hidden: true);
                    excelFile.WriteSheet(StringExtension.Sqlparts(myGrid.sql));
                    i++;
                }
            });
        }
        catch (Exception ex)
        {
            throw new ExcelExportException("Failed to export data to Excel", ex);
        }
    }

    // public static Regex rxImportDataSource = ImportDataSource();
    public static Regex rxImportXlsxTxt = ImportXlsxTxt();

    public void DoXlsxTxtImportFromCodeAsync(IApplicationSettingsContext applicationSettingsContext, string ConnectionString, string importComand, string configDirecotry, IApplicationConfig config, ISqlExecutionLog log, Stopwatch st, bool silent = false)
    {
        // importComand = ___imp: xlsxFile.xlsx/tabName -> tablename
        // importComand = ___imp: xlsxFile.txt/ -> tablename

        var rx1 = rxImportXlsxTxt.Match(importComand);

        if (rx1.Success != true)
        {
            return;
        }
        string filePath = rx1.Groups["filePath"].Value;
        string sheet = rx1.Groups["sheetName"].Value;
        string tableName = rx1.Groups["tableName"].Value;
        string preferedName = tableName.NormalizeName(applicationSettingsContext.Config.KeyWordsListForColoring1);
        string rowLimitS = rx1.Groups["rowLimit"].Value;
        long rowLimitL = Int64.MaxValue - 10;
        if (!string.IsNullOrWhiteSpace(rowLimitS))
        {
            rowLimitL = Int64.Parse(rowLimitS);
        }
        if (rowLimitL <= 200_000)
        {
            filePath = GetSmallerFile(filePath, rowLimitL, Environment.NewLine, '\\');
        }


        try
        {
            this.ReadAndMakeTextFileNew(filePath, $"{configDirecotry}\\data\\{Path.GetFileName(filePath)}" + "forImport", config.SepInExternal[0], null, onlyFirstTab: false, PreferedName: sheet, rowLimitL);
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                MessageBox.Show(ex.Message, "Import error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                throw;
            }
        }

        string name = "";
        try
        {
            using DbConnection odbc = new NzConnection(ConnectionString);

            odbc.Open();
            foreach (var item in this.TabsTablesColumns)
            {
                if (item.Key == sheet || this.TabsTablesColumns.Count == 1)
                {
                    name = this.ImportAction(null, item, odbc, config, configDirecotry, preferedName);
                    break;
                }
            }
            odbc.Close();
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                MessageBox.Show(ex.Message, "Import error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                throw;
            }
        }

        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"imported to {preferedName}", null);
    }

    private static string GetSmallerFile(string path, long linesNum, string newLineChar, char excapeChar)
    {
        if (newLineChar.Length != 1 && newLineChar.Length != 2)
        {
            throw new ArgumentException("Newline character length must be 1 or 2.", nameof(newLineChar));
        }

        string newPath = Path.GetTempPath() + Path.GetRandomFileName() + "txt";
        linesNum++;
        long numberOfNewlines = 0;
        int newlineLength = newLineChar.Length;
        char nl1 = newLineChar[0];
        char nl2 = '\0';
        if (newlineLength == 2)
        {
            nl2 = newLineChar[1];
        }

        char[] buffer = new char[65_536];
        var binaryReader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read));
        // seek the location to read:
        var baseStream = binaryReader.BaseStream;
        long len = baseStream.Length;

        StreamWriter sw = new StreamWriter(newPath);

        int j = 0;
        while (baseStream.Position < len && numberOfNewlines < linesNum)
        {
            binaryReader.BaseStream.Seek(baseStream.Position, SeekOrigin.Begin);
            //if (len - baseStream.Position < 65_536)
            //{
            for (int i = 0; i < 65_536; i++)
            {
                buffer[i] = '\0';
            }
            //}
            binaryReader.ReadBlock(buffer, 0, 65_536);

            int actNum = 0;
            for (int i = 0; i < 65_536; i++)
            {
                if (buffer[i] == excapeChar)
                {
                    i++;
                    continue;
                }

                if (newlineLength == 1 && buffer[i] == nl1)
                {
                    numberOfNewlines++;
                }
                else if (newlineLength == 2 && i < 65_535 && buffer[i] == nl1 && buffer[i + 1] == nl2)
                {
                    numberOfNewlines++;
                    i++;
                }
                if (numberOfNewlines == linesNum)
                {
                    actNum = i;
                    break;
                }
            }

            if (numberOfNewlines < linesNum)
            {
                sw.Write(buffer);
            }
            else if (numberOfNewlines == linesNum)
            {
                sw.Write(buffer, 0, actNum);
            }

            j++;
        }

        binaryReader.Close();
        sw.Close();
        return newPath;
    }

    //[GeneratedRegex(@"___imp(?<sourceType>OleDb|ODBC|DB2): (?<connString>.*)\/(?<tableSource>[a-z0-9_]*) -> (?<tableDest>[a-z0-9_]*)", RegexOptions.IgnoreCase, "pl-PL")]
    //private static partial Regex ImportDataSource();
    [GeneratedRegex(@"___imp(?<rowLimit>[0-9]*): (?<filePath>[-zżźćńółęąśa-z0-9\\:_\.\s]*\.(?<fileExt>[a-zA-Z0-9]+))\/(?<sheetName>[a-z0-9_zżźćńółęąś]*) -> (?<tableName>[a-z0-9_]*)", RegexOptions.IgnoreCase, "pl-PL")]
    private static partial Regex ImportXlsxTxt();
}
