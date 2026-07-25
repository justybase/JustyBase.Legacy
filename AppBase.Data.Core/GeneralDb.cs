using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Common.Models;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace AppBase.Data.Core;

public abstract partial class GeneralDb : IGeneralDb
{
    protected readonly IDatabaseRuntimeContext _databaseRuntimeContext;
    protected readonly ILogger _logger;
    protected readonly IImportExportTasks _importExportTasks;
    protected readonly IGeneralDbService _generalDbService;
    public required Color LogErrorStdColor { get; set; }

    public GeneralDb(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, IImportExportTasks importExportTasks, IGeneralDbService generalDbService)
    {
        _databaseRuntimeContext = databaseRuntimeContext;
        _logger = logger;
        _importExportTasks = importExportTasks;
        _generalDbService = generalDbService;
    }


    public string ConnectionName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DefaultDatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<string> DatabaseList { get; set; } = [];
    public DataTable tables = new();
    public DataTable views = new();
    public DataTable procedures = new();

    public Dictionary<string, Dictionary<string, TypeInDatabase>> objectInSchema { get; set; } = new Dictionary<string, Dictionary<string, TypeInDatabase>>(StringComparer.OrdinalIgnoreCase);

    protected Dictionary<string, (string[] colsNames, string[] colTypes, short[] pkSeq, string[] remarks)> columnsOfTables = new Dictionary<string, (string[], string[], short[], string[] remarks)>(StringComparer.OrdinalIgnoreCase);

    protected Dictionary<string, (string[] indNames, string moreInfo)> indexesOfTable = new();

    protected Dictionary<string, (string[] partNames, string moreInfo)> partitionsOfTable = new();

    protected Dictionary<string, (string[] partNames, string moreInfo)> constraintsOfTable = new();

    protected Dictionary<string, (string[] partNames, string moreInfo)> triggersOfTable = new();

    public string GetSqlAddCode(string objectType, string db, string schema, string parentObject)
    {
        switch (objectType)
        {
            case "column":
                return GetColumn(db, schema, parentObject);
            case "constraint":
                return GetConstraint(db, schema, parentObject);
            case "index":
                return GetIndex(db, schema, parentObject);
            case "partition":
                return GetPartition(db, schema, parentObject);
            case "trigger":
                return GetTrigger(db, schema, parentObject);
            default:
                break;
        }
        return $"code for {objectType}";
    }

    protected virtual string GetColumn(string db, string schema, string parentObject)
    {
        return $"ALTER TABLE {schema}.{parentObject} ADD COLUMN <COLUMN_NAME> INT NOT NULL DEFAULT 0";
    }
    protected virtual string GetConstraint(string db, string schema, string parentObject)
    {
        return "getConstraint code";
    }
    protected virtual string GetIndex(string db, string schema, string parentObject)
    {
        return "getIndex code";
    }
    protected virtual string GetPartition(string db, string schema, string parentObject)
    {
        return "getPartition code";
    }
    protected virtual string GetTrigger(string db, string schema, string parentObject)
    {
        return "getTrigger code";
    }


    public virtual void InitDb() { }

    public bool _initSchemaInProgress = false;

    public bool GetInitSchemaInProgress { get => _initSchemaInProgress; }
    abstract protected void AddToCache(string dbName, string schema, string tablename);

    virtual protected void AddToindexCache(string dbName, string schema, string tablename)
    {
        indexesOfTable[dbName + "_" + schema + "\\" + tablename] = (new string[] { "to do" }, "info");
    }

    virtual protected void AddToPartitionCache(string dbName, string schema, string tablename)
    {
        partitionsOfTable[dbName + "_" + schema + "\\" + tablename] = (new string[] { "to do" }, "info");
    }

    virtual protected void AddToConstraintsCache(string dbName, string schema, string tablename)
    {
        constraintsOfTable[dbName + "_" + schema + "\\" + tablename] = (new string[] { "to do" }, "info");
    }
    virtual protected void AddToTriggersCache(string dbName, string schema, string tablename)
    {
        triggersOfTable[dbName + "_" + schema + "\\" + tablename] = (new string[] { "to do" }, "info");
    }

    public string[] GetIndexes(string dbName, string schema, string tablename)
    {
        try
        {
            if (!indexesOfTable.ContainsKey(dbName + "_" + schema + "\\" + tablename))
            {
                AddToindexCache(dbName, schema, tablename);
            }
            return indexesOfTable[dbName + "_" + schema + "\\" + tablename].indNames;
        }
        catch (Exception)
        {
            return new string[] { "problem" };
        }
    }

    public string[] GetPartitions(string dbName, string schema, string tablename)
    {
        try
        {
            if (!partitionsOfTable.ContainsKey(dbName + "_" + schema + "\\" + tablename))
            {
                AddToPartitionCache(dbName, schema, tablename);
            }
            return partitionsOfTable[dbName + "_" + schema + "\\" + tablename].partNames;
        }
        catch (Exception)
        {
            return new string[] { "problem" };
        }
    }

    public string[] GetConstraints(string dbName, string schema, string tablename)
    {
        try
        {
            if (!constraintsOfTable.ContainsKey(dbName + "_" + schema + "\\" + tablename))
            {
                AddToConstraintsCache(dbName, schema, tablename);
            }
            return constraintsOfTable[dbName + "_" + schema + "\\" + tablename].partNames;
        }
        catch (Exception)
        {
            return new string[] { "problem" };
        }
    }

    public string[] GetTriggers(string dbName, string schema, string tablename)
    {
        try
        {
            if (!triggersOfTable.ContainsKey(dbName + "_" + schema + "\\" + tablename))
            {
                AddToTriggersCache(dbName, schema, tablename);
            }
            return triggersOfTable[dbName + "_" + schema + "\\" + tablename].partNames;
        }
        catch (Exception)
        {
            return new string[] { "problem" };
        }
    }

    public void AddToCacheStandard(string dbName, string schema, string tablename)
    {
        string sql = @$"
                    SELECT 
                   column_name,
                   data_type,
                   case when character_maximum_length is not null
                        then character_maximum_length
                        else numeric_precision end as max_length,
                   is_nullable,
                   column_default as default_value
            from information_schema.columns
            where 
                table_schema not in ('information_schema', 'pg_catalog')
                and table_schema = '{schema}'
                and table_name = '{tablename}'
            order by table_schema, 
                     table_name,
                     ordinal_position";

        using (var conn = GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = _databaseRuntimeContext.Config.CommandTimeout;
                var rdr = cmd.ExecuteReader();

                List<string> ls = new List<string>();
                List<string> ls2 = new List<string>();
                List<short> ls3 = new List<short>();
                List<string> ls4 = new List<string>();
                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0));
                    ls2.Add(rdr.GetString(1));
                    ls3.Add(-1);
                }
                columnsOfTables[dbName + "_" + schema + "\\" + tablename] = (ls.ToArray(), ls2.ToArray(), ls3.ToArray(), ls4.ToArray());
            }
        }

    }

    public string GetViewCodeStandard(string schema, string tablename)
    {
        string sql = @$"
                    select view_definition from information_schema.views wher
            where 
                table_schema = '{schema}'
                and table_name = '{tablename}'";

        string viewCode = "";
        using (var conn = GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = _databaseRuntimeContext.Config.CommandTimeout;
                var scalar = cmd.ExecuteScalar();
                viewCode = scalar?.ToString() ?? string.Empty;
            }
        }

        return $"CREATE VIEW {tablename} AS {viewCode};";

    }

    protected void ResetDynamicCollectionH()
    {
        DynamicCollectionForGeneralHelpers.OneWord.Clear();
        DynamicCollectionForGeneralHelpers.TwoWords.Clear();
        DynamicCollectionForGeneralHelpers.TreeWords.Clear();
    }
    abstract public void ResetDynamicCollection();

    public virtual void Eksport(string sql, string filePath, string fileType)
    {
        using (var conn = GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                lock (_sync)
                {
                    dbActiveCommands.Insert(0, cmd);
                }
                cmd.CommandText = sql;
                cmd.CommandTimeout = _databaseRuntimeContext.Config.CommandTimeout;

                using var rdr = cmd.ExecuteReader();
                switch (fileType)
                {
                    case "xlsx":
                        _importExportTasks.XlsxManyTabs(filePath, rdr, sql, "sheet", true, null, null);
                        break;
                    case "csv":
                        _importExportTasks.ExportCSVReader(Encoding.UTF8, rdr, filePath, "|", ms: false);
                        break;
                    default:
                        break;
                }
                lock (_sync)
                {
                    dbActiveCommands.Remove(cmd);
                }
            }
        }
    }

    public virtual async Task ImportFromFile(Func<string, Encoding> getEncoding,
        Func<int, string> getName,
        Func<string[], List<string>> getTabs,
        IImportExportTasks imp, string filePath, IImportProgressForm f, string db, List<string> tableName, List<string> tabs, int skipRows = 0, bool silent = false)
    {
        await PerformImportFromFileAsync(imp, filePath, f, tableName, tabs, skipRows);
    }
    public int NotifyAfter { get; set; }


    protected List<DbCommand> dbActiveCommands = [];
    protected Lock _sync = new Lock();

    public async Task AbortAsync(object o)
    {
        for (int i = 0; i < dbActiveCommands.Count; i++)
        {
            var item = dbActiveCommands[i];
            if (o is not null && o is string str && str != "x" && str != item.CommandText)
            {
                continue;
            }
            try
            {
                await Task.Run(() => item.Cancel());
            }
            catch (Exception)
            {
                //throw;
            }

            lock (_sync)
            {
                dbActiveCommands.Remove(item);
                i--;
            }
        }
    }

    public (string[], string[], short[], string[]) GetColumnsEx(string dbName, string schema, string tablename)
    {
        try
        {
            if (!columnsOfTables.ContainsKey(dbName + "_" + schema + "\\" + tablename))
            {
                AddToCache(dbName, schema, tablename);
            }
            return (columnsOfTables[dbName + "_" + schema + "\\" + tablename].colsNames
                , columnsOfTables[dbName + "_" + schema + "\\" + tablename].colTypes,
                columnsOfTables[dbName + "_" + schema + "\\" + tablename].pkSeq,
                columnsOfTables[dbName + "_" + schema + "\\" + tablename].remarks
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(dbName + "_" + schema + "\\" + tablename + " getColumnsEx error: " + ex.Message, ex);
            return (new string[] { "fool" }, new string[] { "fool" }, new short[] { -1 }, new string[] { "fool" });
        }
    }

    public string[] GetColumns(string dbName, string schema, string tablename)
    {
        try
        {
            if (!columnsOfTables.ContainsKey(dbName + "_" + schema + "\\" + tablename))
            {
                AddToCache(dbName, schema, tablename);
            }
            return columnsOfTables[dbName + "_" + schema + "\\" + tablename].Item1;
        }
        catch (Exception)
        {
            return new string[] { "fool" };
        }
    }

    public virtual string GetCreateTableText(string dbName, string schema, string tablename)
    {
        return "not implemented yet";
    }

    public virtual List<string> GetTablesOfSchema(string schema)
    {
        List<string> tabs = new List<string>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            foreach (DataRow row in tables.Select($"TABLE_SCHEMA = '{schema}'"))
            {
                tabs.Add(row["TABLE_NAME"].ToString() ?? string.Empty);
            }
        }
        else
        {
            foreach (DataRow row in tables.Rows)
            {
                tabs.Add(row["TABLE_SCHEMA"].ToString() + "." + row["TABLE_NAME"].ToString());
            }
        }

        return tabs;
    }

    public string GetCreateAllTablesText(string schema)
    {
        var tables = GetTablesOfSchema(schema);
        StringBuilder sb = new StringBuilder();
        foreach (var tab in tables)
        {
            sb.Append(GetCreateTableText(DefaultDatabaseName, schema, tab));
        }
        return sb.ToString();
    }


    public virtual string GetCreateViewText(string dbName, string schema, string viewName)
    {
        return "not implemented yet";
    }

    public virtual string GetCreatePorcedureText(string schema, string viewName)
    {
        return "not implemented yet";
    }

    public virtual string GetCreateTableText(string schemaTablename) //schema.Tablename
    {
        int id = schemaTablename.FirstDot();
        string schema = schemaTablename.Substring(0, id);
        string tablename = schemaTablename.Substring(id + 1);
        return GetCreateTableText(DefaultDatabaseName, schema, tablename);
    }

    public virtual string GetCreateViewText(string schemaTablename) //schema.Tablename
    {
        int id = schemaTablename.FirstDot();

        string schema = schemaTablename.Substring(0, id);
        string tablename = schemaTablename.Substring(id + 1);
        return GetCreateViewText(DefaultDatabaseName, schema, tablename);

    }

    public async Task<string> GetCreateProcedureText(string schemaTablename) //schema.Tablename
    {
        int id = schemaTablename.FirstDot();
        string schema = schemaTablename.Substring(0, id);
        string tablename = schemaTablename.Substring(id + 1);

        return await Task.Run(() => GetCreatePorcedureText(schema, tablename));
    }

    protected bool top = false;
    public virtual async Task PerformImportXmlAsync(IDataObject clipboard, char escapechar, char sep, IImportProgressForm f, string db)
    {

        f?.AddRow("Gathering data from clipboard...");

        await Task.Run(() =>
        {
            f?.AddRow("Data types analysing..", (int)ProgressBarStyle.Marquee);

            string randName = StringExtension.RandomName("IMP_");
            DataTable source = _importExportTasks.GetDataTableFromClipboard(clipboard, escapechar, sep, true);

            f?.SetProgressBarValue(100, (int)ProgressBarStyle.Continuous);

            try
            {
                f?.AddRow("Importing..", (int)ProgressBarStyle.Marquee);

                DbSpecificImportPart(randName, source, 10_000,
                    o =>
                    {
                        int rowNum = f?.AddRow(o) ?? -1;
                        f?.AddRow(o);
                        if (o.StartsWith("ERROR"))
                        {
                            f?.SetColor(rowNum, LogErrorStdColor);
                        }
                        f?.SetFirstDisplayedScrollingRowIndex(rowNum);
                    });
                f?.CompleteForGeneral(randName, top);
            }
            catch (Exception ex)
            {
                _logger.LogError(randName + " import error: " + ex.Message, ex);
            }
        });
    }

    private async Task PerformImportFromFileAsync(IImportExportTasks imp, string filename, IImportProgressForm f, List<string> tableName, List<string> tabs, int skipRows)
    {
        await Task.Run(() =>
        {

            try
            {

                f?.AddRow("Gathering data from file...");
                f?.SetProgressBarValue(0);
                var ds = imp.ReadFileAndMakeDataSet(filename, skipRows, onlyFirst: false);

                f?.SetProgressBarValue(100);

                List<string> randNames = new List<string>();
                int n = ds.Tables.Count;
                if (tableName != null && tableName.Count < n)
                {
                    n = tableName.Count;
                }

                for (int i = 0; i < n; i++)
                {
                    if (tabs != null && !tabs.Contains(ds.Tables[i].TableName))
                    {
                        continue;
                    }

                    f?.AddRow($"importing...{ds.Tables[i].TableName}", (int)ProgressBarStyle.Marquee);

                    string randName;
                    if (tableName != null)
                    {
                        randName = tableName[i];
                    }
                    else
                    {
                        randName = "IMP_" + StringExtension.RandomName(ds.Tables[i].TableName.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1));
                    }
                    randNames.Add(randName);

                    DbSpecificImportPart(randName, ds.Tables[i], 5_000, o =>
                    {
                        int rowNum = f?.AddRow(o) ?? -1;
                        if (o.StartsWith("ERROR"))
                        {
                            if (rowNum >= 0)
                            {
                                f?.SetColor(rowNum, LogErrorStdColor);
                            }
                        }
                        if (rowNum >= 0)
                        {
                            f?.SetFirstDisplayedScrollingRowIndex(rowNum);
                        }
                    });
                }
                f?.CompleteForGeneral(randNames, top);
            }
            catch (Exception ex)
            {
                _logger.LogError("Import from file error: " + ex.Message, ex);
            }
        });
    }

    protected virtual void DbSpecificImportPart(string randName, DataTable source, int NotifyAfter, Action<string> progress, bool tableExists = false, IDataReader? rdr = null)
    {
        _logger.Log($"dbSpecificImportPart not implemented for {this.GetType().Name} for {randName}");
    }

    public virtual async Task PerformImportFromText(char escapechar, char sep, IImportProgressForm f, string db, string SelectedConnectionName)
    {
        _logger.Log("not implemented");
        await Task.CompletedTask;
    }

    public virtual void RunSqlNoResults(string sql)
    {
        using (var conn = GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = _databaseRuntimeContext.Config.CommandTimeout;
                cmd.ExecuteNonQuery();
            }
        }
    }

    public virtual (DbConnection conn, string res) RunScalarSql(string sql)
    {
        var conn = GetConnection();
        string res;
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.CommandTimeout = _databaseRuntimeContext.Config.CommandTimeout;
            res = cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }
        return (conn, res);
    }

    public bool ImportNotifyEventAdded { get; set; }

    public abstract DatabaseTypeEnum DatabaseType { get; }

    public event Action<string>? OnImportNotify;
    public event Action<string>? NoticeEvent;

    protected void RaiseNotice(string message)
    {
        NoticeEvent?.Invoke(message);
    }

    public void ImportedSomeRows(string rows)
    {
        OnImportNotify?.Invoke("rows copied " + rows);
    }

    public void ResetDbName(string connectionName, string dbName)
    {
        if (_generalDbService.DriverName(connectionName) == "MsSqlStd")
        {
            ConnectionString = _generalDbService.ConnectionStringForMsSql(connectionName, dbName);
        }
        else if (_generalDbService.DriverName(connectionName) == "MsSqlTrusted")
        {
            ConnectionString = _generalDbService.ConnectionStringForMsSqlTrusted(connectionName, dbName);
        }
    }


    public virtual void DoCsvOrXlsxExport(string runCommand, ISqlExecutionLog log, Stopwatch st)
    {
        var r = _databaseRuntimeContext.RxExportCsvXlsx.Match(runCommand);
        string sql = r.Groups["sql"].Value;
        string filePath = r.Groups["filePath"].Value;

        string mode = "xlsx";
        if (runCommand.StartsWith("___expCsv"))
        {
            mode = "csv";
        }

        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"started {r.Groups["filePath"].Value}", DBNull.Value);

        using (var conn = GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = _databaseRuntimeContext.Config.CommandTimeout;
                using (var rdr = cmd.ExecuteReader())
                {
                    if (mode == "csv")
                    {

                        _importExportTasks.ExportCSVReader(Encoding.UTF8, rdr, filePath, _databaseRuntimeContext.Config.SepInExportedCsv.ToString(), false, null,
                            (o) =>
                            {
                                log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"tranfered {o} rows", DBNull.Value);
                            }
                            , ms: false);
                    }
                    else if (mode == "xlsx")
                    {
                        _importExportTasks.XlsxManyTabs(filePath, rdr, sql, "data", true,
                            (o) =>
                            {
                                log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"tranfered {o} rows", DBNull.Value);
                            }
                            , () =>
                            {
                                log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"packing started", DBNull.Value);
                            }
                        );
                    }
                }

            }
        }
        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"finished {r.Groups["filePath"].Value}", DBNull.Value);
    }

    public virtual (DbDataReader, DbConnection) GetDbDataReader(string sql)
    {
        var conn = GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = _databaseRuntimeContext.Config.CommandTimeout;
        var rdr = cmd.ExecuteReader();
        return (rdr, conn);
    }


    public void DoCsvAdvanced(string runCommand, AdvancedExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Encod);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Delimiter);
        var val = GetDbDataReader(runCommand);
        var rdr = val.Item1;
        var con = val.Item2;
        try
        {
            _importExportTasks.ExportCSVReader(options.Encod, rdr, options.Path, options.Delimiter, useSytemNewline: false, NewLine: options.Linedelimiter, action: null, ms: true, options.Header);
        }
        finally
        {
            rdr.Dispose();
            con.Dispose();
        }
    }

    public void DoXlsxAdvanced(string runCommand, AdvancedExportOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);
        var val = GetDbDataReader(runCommand);
        var rdr = val.Item1;
        var con = val.Item2;
        try
        {
            if (!string.IsNullOrWhiteSpace(options.TabName))
            {
                _importExportTasks.ExportToExistingXlsxReader(rdr, options);
            }
            else
            {
                _importExportTasks.ExportXlsxReader(rdr, options.Path, runCommand, _databaseRuntimeContext.Config);
            }
        }
        finally
        {
            rdr.Dispose();
            con.Dispose();
        }
    }
    public abstract string SearchInViewsSource(string txtToSearch);
    public abstract string SearchInProcedureSource(string txtToSearch);
    public abstract DbConnection GetConnection(string databaseName, bool usePool = true);
    public abstract DbConnection GetConnection();

    public virtual Task<string> GetCreateAliasTextAsync(string schemaTablename)
    {
        throw new NotImplementedException();
    }

    public virtual Task<string> GetCreateSynonymTextAsync(string schemaTablename)
    {
        throw new NotImplementedException();
    }

    public virtual Task<(string, string)> GetAliasDataAsync(string schema, string aliasName)
    {
        throw new NotImplementedException();
    }

    public virtual Task<string[]> GetLinkedServerTablesAsync(string linkedServerName)
    {
        throw new NotImplementedException();
    }

    public virtual void BlobQuery(string sql)
    {
        throw new NotImplementedException();
    }

    public virtual Task<(string, string)> GetSynonymDataAsync(string schema, string aliasName)
    {
        throw new NotImplementedException();
    }
}
