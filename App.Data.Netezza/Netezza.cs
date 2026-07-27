using AppBase.Common;
using AppBase.Common.Configuration;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using JustyBase.NetezzaDriver;
using JustyBase.NetezzaCatalogSql;
using JustyBase.NetezzaDdl;
using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AppBase.Data;

public sealed class Netezza : GeneralDb, INetezza
{
    private readonly IDatabaseRuntimeCatalogWriter _catalogWriter;
    private readonly INetezzaHelperService _netezzaHelperService;
    private readonly SemaphoreSlim _schemaDownloadGate = new(1, 1);

    public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.Netezza;
    public Netezza(
        IDatabaseRuntimeContext databaseRuntimeContext,
        ILogger logger,
        IImportExportTasks importExportTasks,
        IGeneralDbService generalDbService,
        INetezzaHelperService netezzaHelperService)
        : base(databaseRuntimeContext, logger, importExportTasks, generalDbService)
    {
        _catalogWriter = databaseRuntimeContext as IDatabaseRuntimeCatalogWriter
            ?? throw new InvalidOperationException("Netezza requires the schema catalog write port.");
        _netezzaHelperService = netezzaHelperService
            ?? throw new ArgumentNullException(nameof(netezzaHelperService));
    }

    public static List<string> GetDatabaseList(int connectionTimeout, string server, string user, string port, string pass)
    {
        List<string> list = new List<string>();

        NzConnectionStringBuilder builder = new NzConnectionStringBuilder();
        builder.UserName = user;
        builder.Password = pass;
        builder.Port = int.Parse(port);
        builder.Host = server;
        builder.Database = "SYSTEM";
        builder.Timeout = connectionTimeout;

        try
        {
            using (var conn = new NzConnection(builder.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NzCommand(NetezzaHelpers.DATABASES, conn))
                {
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(2))
                        {
                            list.Add(string.Intern(rdr.GetString(2)));
                        }

                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }


        return list;
    }

    public List<string> GroupsList()
    {
        List<string> list = new List<string>();
        try
        {
            using (var conn = GetConnection() as NzConnection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = NetezzaHelpers.USER_GROUPS;
                    cmd.CommandTimeout = 30;
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0))
                        {
                            list.Add(rdr.GetString(0));
                        }

                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        return list;
    }

    public (List<string> owner, List<string> name, List<string> desc, List<int> id) GetFulides(string dbName, int idObj)
    {
        List<string> owner = new List<string>();
        List<string> name = new List<string>();
        List<string> desc = new List<string>();
        List<int> id = new List<int>();

        List<string> list = new List<string>();
        try
        {
            using (var conn = GetConnection() as NzConnection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = NetezzaHelpers.GetFulidesSql(dbName, idObj);
                    cmd.CommandTimeout = 30;
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string? tmpOwner = null;
                        if (!rdr.IsDBNull(0))
                        {
                            tmpOwner = rdr.GetString(0);
                        }
                        string? tmpName = null;
                        if (!rdr.IsDBNull(1))
                        {
                            tmpName = rdr.GetString(1);
                        }
                        string? tmpDesc = null;
                        if (!rdr.IsDBNull(2))
                        {
                            tmpDesc = rdr.GetString(2);
                        }
                        owner.Add(tmpOwner);
                        name.Add(tmpName);
                        desc.Add(tmpDesc);
                        id.Add(rdr.GetInt32(3));
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        return (owner, name, desc, id);
    }

    public Dictionary<int, List<(string keyName, char keyType, Int16 columnPosition, string columnName, int? refTableId, string? refColumnName, string? UPDT_TYPE, string? DEL_TYPE)>> keysInTables
    { get; init; } = new();

    public List<NetezzaColumnInfoRow> ColumnList { get; init; } = new();

    public Dictionary<string, List<NetezzaBasesTables>> BasesTablesList { get; init; } = new();

    public void ResetLists()
    {
        keysInTables.Clear();
        ColumnList.Clear();
        BasesTablesList.Clear();
    }


    private string MakeOneDb(string connectionName, string database)
    {
        try
        {
            // Timing markers removed from production code

            using NzConnection connTemp = GetConnection(database, usePool: false) as NzConnection;
            connTemp.Open();            
            


            // BAZY_TABELE_OPISY
            var cmDescNz = connTemp.CreateCommand();
            cmDescNz.CommandText = NetezzaHelpers.GetDescSql(database);
            using var rdDesc = cmDescNz.ExecuteReader();
            while (rdDesc.Read())
            {
                int tableId = (rdDesc.GetValue(0) as int?) ?? -1;
                string? tableDescription = null;
                if (!rdDesc.IsDBNull(1))
                {
                    tableDescription = rdDesc.GetString(1);
                }
                _catalogWriter.SetTableDescription(connectionName, database, tableId, tableDescription);
            }

            //KEYS_IN_TABLES
            var cmdKeysNZ = connTemp.CreateCommand();
            cmdKeysNZ.CommandText = NetezzaHelpers.TABLE_KEYS_NZ_SQL;
            using var rdKeys = cmdKeysNZ.ExecuteReader();

            var res1 = keysInTables;

            lock (res1)
            {
                while (rdKeys.Read())
                {
                    if (rdKeys.IsDBNull(4))
                    {
                        continue;
                    }
                    string colName = rdKeys.GetString(4);
                    if (!colName.IsGoodName())
                    {
                        colName = StringExtension.QuoteNameIfNeeded(colName);
                    }
                    string pKcolName = null;

                    if (!rdKeys.IsDBNull(6))
                    {
                        pKcolName = rdKeys.GetString(6) as string;
                    }

                    if (pKcolName is not null && !pKcolName.IsGoodName())
                    {
                        pKcolName = StringExtension.QuoteNameIfNeeded(pKcolName);
                    }

                    int tableId = rdKeys.GetInt32(0);
                    string? keyName = null;
                    if (!rdKeys.IsDBNull(1))
                        keyName = rdKeys.GetString(1);

                    char keyType = rdKeys.GetString(2)[0];
                    Int16 columnPosition = rdKeys.GetInt16(3);
                    string? columnName = colName;
                    int? refTableId = rdKeys.GetValue(5) as int?;
                    string column2 = pKcolName;

                    string? UpdateTypeText = null;
                    if (!rdKeys.IsDBNull(7))
                    {
                        UpdateTypeText = rdKeys.GetString(7);
                    }
                    string? DeleteTypeText = null;
                    if (!rdKeys.IsDBNull(8))
                    {
                        DeleteTypeText = rdKeys.GetString(8);
                    }
                    if (string.IsNullOrEmpty(UpdateTypeText))
                    {
                        UpdateTypeText = "NO ACTION";
                    }
                    if (string.IsNullOrEmpty(DeleteTypeText))
                    {
                        DeleteTypeText = "NO ACTION";
                    }

                    if (!res1.ContainsKey(tableId))
                    {
                        res1[tableId] = new();
                    }
                    res1[tableId].Add((keyName, keyType, columnPosition, columnName, refTableId, column2, UpdateTypeText, DeleteTypeText));
                }
            }


            DbCommand cmdColumnsNZ = connTemp.CreateCommand();
            cmdColumnsNZ.CommandText = NetezzaHelpers.OBJECT_COLUMNS_NZ_SQL_OF_DB(database);

            using var rdColumns = cmdColumnsNZ.ExecuteReader();

            lock (ColumnList)
            {
                //ColumnList[database] = new List<NetezzaColumnInfoRow>();
                while (rdColumns.Read())
                {
                    string colName = rdColumns.GetString(3);
                    if (_generalDbService.ReservedWords.Contains(colName) || !colName.IsGoodName())
                    {
                        colName = StringExtension.QuoteNameIfNeeded(colName);
                    }

                    var COLUMN_NUMBER = (UInt16)rdColumns.GetInt16(0);
                    var TABLE_ID = rdColumns.GetInt32(1);
                    var DATABASE_ID = rdColumns.GetInt32(2);
                    var COLUMN_NAME = colName;
                    string? COLUMN_DESCRIPTION = null;
                    if (!rdColumns.IsDBNull(4))
                        COLUMN_DESCRIPTION = rdColumns.GetString(4);

                    var DATA_TYPE = rdColumns.GetString(5);
                    var IS_NULLABLE = rdColumns.GetBoolean(6);
                    var DISTSEQNO = rdColumns.GetValue(7) as sbyte?;
                    var ORGSEQNO = rdColumns.GetValue(8) as sbyte?;

                    string COLDEFAULT = null;
                    if (!rdColumns.IsDBNull(9))
                        COLDEFAULT = rdColumns.GetString(9);

                    if (string.IsNullOrEmpty(COLDEFAULT))
                    {
                        COLDEFAULT = null;
                    }

                    ColumnList.Add(new NetezzaColumnInfoRow()
                    {
                        COLUMN_NUMBER = COLUMN_NUMBER,
                        TABLE_ID = TABLE_ID,
                        DATABASE_ID = DATABASE_ID,
                        COLUMN_NAME = COLUMN_NAME,
                        COLUMN_DESCRIPTION = COLUMN_DESCRIPTION,
                        DATA_TYPE = DATA_TYPE,
                        IS_NULLABLE = IS_NULLABLE,
                        DISTSEQNO = DISTSEQNO,
                        ORGSEQNO = ORGSEQNO,
                        COLDEFAULT = COLDEFAULT
                    });
                }
            }



        }
        catch (Exception)
        {
            throw;
        }


        return database;
    }
    public Dictionary<int, string> DatabaseIdToName { get; set; } = [];

    public HashSet<string> system_names_set = [];
    public async Task<bool> DownloadSchemaNetezza(string connectionName, NetezzaRefreshMode netezzaRefresh, List<string> dbsToRefresh, bool loadSources = false,
        Action showInUiExtra = null)
    {
        // Serialize schema downloads on this provider instance — overlapping refreshes
        // mutate shared ColumnList / DatabaseIdToName / caches and throw
        // "Collection was modified; enumeration operation may not execute."
        await _schemaDownloadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await DownloadSchemaNetezzaCore(connectionName, netezzaRefresh, dbsToRefresh, loadSources, showInUiExtra).ConfigureAwait(false);
        }
        finally
        {
            _schemaDownloadGate.Release();
        }
    }

    private async Task<bool> DownloadSchemaNetezzaCore(string connectionName, NetezzaRefreshMode netezzaRefresh, List<string> dbsToRefresh, bool loadSources = false,
        Action showInUiExtra = null)
    {
        _netezzaHelperService.SqliteInProgress = true;
        bool returnValue = true;
        try
        {
            bool res = true;

            string defaultDatabase = "";
            List<string> bases = new List<string>();

            await Task.Run(() =>
            {
                DbCommand cm1;
                DbDataReader rd1;

                using NzConnection netezzaConnection = GetConnection() as NzConnection;
                netezzaConnection.Open();
                lock (string.Intern(connectionName))
                {
                    _netezzaHelperService.ServerVersion = netezzaConnection.ServerVersion;
                    defaultDatabase = netezzaConnection.Database;

                    if (!BasesTablesList.ContainsKey(connectionName))
                    {
                        BasesTablesList[connectionName] = new List<NetezzaBasesTables>();
                    }
                    var actualBasesTablesList = BasesTablesList[connectionName];

                    //BAZY
                    string databasesQuery = NetezzaHelpers.DATABASES;
                    cm1 = netezzaConnection.CreateCommand() as NzCommand;
                    cm1.CommandText = databasesQuery;

                    rd1 = cm1.ExecuteReader();
                    DatabaseIdToName.Clear();

                    _catalogWriter.ClearDatabaseConnection(connectionName);

                    while (rd1.Read())
                    {
                        int id = rd1.GetInt32(0);
                        int schemaId = rd1.GetInt32(1);

                        string databaseName = rd1.GetString(2);
                        string databaseOwner = rd1.GetString(3);
                        if (!databaseOwner.IsGoodName())
                        {
                            databaseOwner = StringExtension.QuoteNameIfNeeded(databaseOwner);
                        }
                        DatabaseIdToName[id] = databaseName;

                        int databaseId = id;
                        //string databaseName = databaseName;
                        //string databaseOwner = ownerB;
                        string schemaName = rd1.GetString(4);

                        _catalogWriter.SetDatabase(connectionName, databaseId, new DatabaseInfo(schemaId, databaseName, databaseOwner, schemaName));
                        _catalogWriter.EnsureBaseTableConnection(connectionName, databaseId);
                        bases.Add(databaseName);
                    }
                    var databaseSnapshot = _catalogWriter.GetDatabaseSnapshot();

                    DatabasesCount = bases.Count;

                    showInUiExtra?.Invoke();

                    if (DatabasesCount > 0)
                    {
                        _databaseRuntimeContext.Config.CachedDatabaseDictionary = databaseSnapshot
                            .ToDictionary(pair => pair.Key, pair => new Dictionary<int, DatabaseInfo>(pair.Value));
                    }
     

                    rd1.Close();

                    string databaseTablesQuery = "";
                    bool ownerMode = !NetezzaHelpers.SchemasOn(netezzaConnection);

                    databaseTablesQuery = NetezzaHelpers.DatabaseTablesSql(defaultDatabase, ownerMode: ownerMode, noDescMode: true);

                    cm1 = netezzaConnection.CreateCommand();
                    cm1.CommandText = databaseTablesQuery;
                    rd1 = cm1.ExecuteReader();

                    system_names_set.Clear();
                    do
                    {
                        while (rd1.Read())
                        {
                            int tableId = rd1.GetInt32(0);
                            int databaseId = rd1.GetInt32(1);
                            string tableName = rd1.GetString(2);
                            string tableSchema = null;
                            if (!rd1.IsDBNull(4))
                            {
                                tableSchema = rd1.GetString(4);
                            }
                            string tableObjectOwner = null;
                            if (!rd1.IsDBNull(5))
                            {
                                tableObjectOwner = rd1.GetString(5);
                            }

                            string kind = rd1.GetString(6);
                            if (databaseId == 0)
                            {
                                databaseId = 1;
                                system_names_set.Add(tableName);
                            }
                            if (!tableName.IsGoodName())
                            {
                                tableName = StringExtension.QuoteNameIfNeeded(tableName);
                            }
                           
                            if (string.IsNullOrEmpty(tableSchema))
                                tableSchema = "ADMIN";
                            if (string.IsNullOrEmpty(tableObjectOwner))
                                tableObjectOwner = "ADMIN";

                            string treeKey = ownerMode ? tableObjectOwner : tableSchema;
                            tableSchema = StringExtension.QuoteNameIfNeeded(tableSchema);
                            tableObjectOwner = StringExtension.QuoteNameIfNeeded(tableObjectOwner);
                            treeKey = StringExtension.QuoteNameIfNeeded(treeKey);

                            actualBasesTablesList.Add(new NetezzaBasesTables()
                            {
                                TABLE_ID = tableId,
                                DATABASE_ID = databaseId,
                                TABLE_NAME = tableName,
                                OWNER_NAME = treeKey,
                                SCHEMA_NAME = tableSchema,
                                OBJECT_OWNER_NAME = tableObjectOwner,
                                OBJECT_TYPE = kind
                            });

                        }
                    } while (rd1.NextResult());

                }
            });
            if (!res)
            {
                _netezzaHelperService.SqliteInProgress = false;
                return res;
            }

            List<string> basesXXX = netezzaRefresh switch
            {
                NetezzaRefreshMode.partialOnlyTables => dbsToRefresh ?? new List<string>(),
                NetezzaRefreshMode.partial => new List<string> { defaultDatabase },
                _ => bases
            };
            ColumnList.Clear();
            showInUiExtra?.Invoke();

            await Task.Run(() =>
            {
                Parallel.ForEach(basesXXX, new ParallelOptions { MaxDegreeOfParallelism = _databaseRuntimeContext.Config.MaxSchemaParallelism }, (database) =>
                {
                    MakeOneDb(connectionName, database);
                });
            });

            if (loadSources)
            {
                await LoadSourceTextCache();
            }

            // attaching DB's
            AttachedDbsToSchema.Clear();

            //final integrate and cleanup (main connection)
            for (int i = 0; i < basesXXX.Count; i++)
            {
                string databaseName = basesXXX[i];
                RegisterAttachedDb(databaseName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            returnValue = false;
        }
        finally
        {
            _netezzaHelperService.SqliteInProgress = false;
        }



        return returnValue;
    }

    public async Task<bool> DownloadOneDb(string connectionName, string databaseName)
    {
        SetDbInProgress(databaseName);
        _netezzaHelperService.SqliteInProgress = true;
        try
        {
            await Task.Run(() =>
            {
                MakeOneDb(connectionName, databaseName);
                RegisterAttachedDb(databaseName);
            });
        }
        catch (Exception)
        {
            _netezzaHelperService.SqliteInProgress = false;
            throw;
        }
        finally
        {
            RemoveDbFromProgress(databaseName);
            _netezzaHelperService.SqliteInProgress = false;
        }
        return true;
    }

    public Dictionary<string, DateTime> AttachedDbsToSchema { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _attachedDbsInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsDbInProgress(string db)
    {
        bool result = false;

        lock (_attachedDbsInProgress)
        {
            result = _attachedDbsInProgress.Contains(db);
        }

        return result;
    }

    private void SetDbInProgress(string db)
    {
        lock (_attachedDbsInProgress)
        {
            _attachedDbsInProgress.Add(db);
        }
    }

    private void RemoveDbFromProgress(string db)
    {
        lock (_attachedDbsInProgress)
        {
            _attachedDbsInProgress.Remove(db);
        }
    }

    public int DatabasesCount { get; set; }

    private void RegisterAttachedDb(string databaseName)
    {
        lock (ConnectionName)
        {
            SetDbInProgress(databaseName);
            AttachedDbsToSchema[databaseName] = DateTime.Now;
            RemoveDbFromProgress(databaseName);
        }
    }

    public override List<string> GetTablesOfSchema(string schema)
    {
        List<string> tabs = new List<string>();

        using (DbConnection dbConnection = GetConnection())
        {
            dbConnection.Open();
            string sql = NetezzaSystemSql.AllNonSystemTables;
            using (var cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText = sql;
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    tabs.Add(rdr.GetString(0));
                }
            }
        }
        return tabs;
    }

    public override void ResetDynamicCollection()
    {
        throw new NotImplementedException();
    }

    protected override void AddToCache(string dbname, string schema, string tablename)
    {
        throw new NotImplementedException();
    }

    private DatabaseColumnType GetTypedValue(string[] headers, Dictionary<int, Dictionary<DatabaseColumnType, int[]>> typesCount, string[] row, int j, bool doTrim = true, bool isBoolean = false)
    {
        DatabaseColumnType nz;
        string val;
        if (isBoolean)
        {
            nz = DatabaseColumnType.boolean;
            val = row[j];
        }
        else
        {
            val = _generalDbService.PrepareValue(out nz, row[j], typeAdn: false, textQualifier: "", doTrim: doTrim);
        }


        if (nz == DatabaseColumnType.integer && row[j].Trim().Length == 11 && headers[j].Contains("PESEL", StringComparison.OrdinalIgnoreCase))
        {
            nz = DatabaseColumnType.nvarchar;
            val = row[j];
        }
        if (!typesCount.ContainsKey(j))
        {
            typesCount[j] = [];
        }
        if (!typesCount[j].ContainsKey(nz))
        {
            typesCount[j][nz] = new int[3];
        }
        if (nz == DatabaseColumnType.numeric)
        {
            int dotPossition = val.IndexOf('.');
            if (dotPossition == -1)
            {
                dotPossition = val.Length;
            }

            if (typesCount[j][nz][1] < dotPossition + _generalDbService.MinimumNumericPrecision)
            {
                typesCount[j][nz][1] = dotPossition + _generalDbService.MinimumNumericPrecision;
            }
            typesCount[j][nz][2] = _generalDbService.MinimumNumericPrecision;
        }

        if ((nz == DatabaseColumnType.nvarchar || nz == DatabaseColumnType.integer) && typesCount[j][nz][1] < val.Length)
        {
            typesCount[j][nz][1] = val.Length > 0 ? val.Length : 1;
        }

        typesCount[j][nz][0]++;
        row[j] = val;
        return nz;
    }

    public override async Task PerformImportXmlAsync(IDataObject clipboard, char escapechar, char sep, IImportProgressForm f, string db)
    {
        f.AddRow("Gathering data from clipboard...");
        var config = _databaseRuntimeContext.Config;
        var configDirecotry = _databaseRuntimeContext.ConfigDirectory;
        string tabName = "";
        await Task.Run(() =>
        {
            XmlTextReader reader = new XmlTextReader((MemoryStream)clipboard.GetData("XML Spreadsheet"));
            f.AddRow("Data types analysing..");

            string[] lines = null;
            string[] headers = null;
            string[] row = null;
            int colNum = 0;
            int rowNum = 0;
            string randName = StringExtension.RandomName("IMPORTED_");
            string nl = Environment.NewLine;

            Dictionary<int, Dictionary<DatabaseColumnType, int[]>> typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>();
            int actInd = -1;
            int cellNum = 0;
            int dataNum = 0;
            while (reader.Read())//reader.MoveToNextAttribute() ||
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
                            colNum += (cellNum - dataNum); //cell wihout data situation =  <Cell />
                            cellNum = dataNum;
                        }
                        string typeTxt = reader.GetAttribute("ss:Type");
                        if (typeTxt is not null && typeTxt == "Boolean")
                        {

                        }

                        reader.Read();
                        string val = reader.Value;
                        if (val.Contains(escapechar))
                        {
                            val = val.Replace(escapechar.ToString(), $"{escapechar}{escapechar}");
                        }
                        if (val.Contains(sep))
                        {
                            val = val.Replace(sep.ToString(), $"{escapechar}{sep}");
                        }
                        if (val.Contains('\n'))
                        {
                            val = val.Replace("\n", $"{escapechar}\n");
                        }
                        if (val.Contains('\r'))
                        {
                            val = val.Replace("\r", "");
                        }

                        if (rowNum == 0)
                        {
                            row[colNum++] = val;
                        }
                        else
                        {
                            if (actInd != -1 && actRow == rowNum)
                            {
                                colNum = actInd;
                            }
                            row[colNum] = val;
                            if (typeTxt is not null && typeTxt == "Boolean")
                            {
                                GetTypedValue(headers, typesCount, row, colNum, doTrim: false, true);
                            }
                            else
                            {
                                GetTypedValue(headers, typesCount, row, colNum, doTrim: false);
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
                                row = new string[Int32.Parse(reader.Value)];
                                headers = new string[Int32.Parse(reader.Value)];
                            }
                            else if (reader.Name == "ss:ExpandedRowCount")
                            {
                                lines = new string[Int32.Parse(reader.Value)];
                            }
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Row")
                {

                    cellNum = 0;
                    dataNum = 0;
                    lines[rowNum++] = String.Join(sep, row);
                    row = new string[row.Length];
                    colNum = 0;
                    if (rowNum == 1)//headers
                    {
                        headers = lines[0].Trim().Split(sep).Select(arg => arg.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1).Trim()).ToArray();
                        StringExtension.RemoveDuplicates(headers);
                    }

                    if (rowNum % 5000 == 0 || rowNum == lines.Length - 1)
                    {
                        f.SetProgressBarValue((int)(100 * rowNum) / (lines.Length));
                    }
                }
            }

            f.SetProgressBarValue(100);
            _importExportTasks.ChooseTypes(typesCount, headers);

            string serverName = $"pipe_sql_{Random.Shared.Next(0, 9999)}";
            f?.AddRow($"starting LinesPipeServer");
            f?.AddRow(NetezzaSystemSql.GetLoadProgress(lines.Length));


            _importExportTasks.LinesPipeServer(lines, serverName, f);
            Thread.Sleep(100);

            using DbConnection dbConnection = GetConnection(db);
            dbConnection.Open();

            var cmd = dbConnection.CreateCommand();
            cmd.CommandText = NetezzaImportSql.CreateRandomDistributionTable(randName, headers);

            cmd.CommandTimeout = config.CommandTimeout;

            f.AddRow($"creating {randName}");
            cmd.ExecuteNonQuery();
            cmd.CommandText = NetezzaImportSql.InsertFromExternalPipe(randName, serverName, headers);
            string sep2 = (sep == '\t' ? "\\t" : sep.ToString());

            cmd.CommandText += @$"USING(
                    REMOTESOURCE 'DOTNET'
                    DELIMITER '{sep2}'
                    SKIPROWS 1
                    NULLVALUE ''
                    ENCODING 'utf-8'
                    ESCAPECHAR '{escapechar}'
                    --QUOTEDVALUE 'DOUBLE' 
                    TIMESTYLE '24HOUR'
                    MAXERRORS {config.ExternalMAXERRORS}
                    LOGDIR '{configDirecotry}\\data\\'
                );";
            f?.AddRow($"inserting into {randName} started");

            cmd.ExecuteNonQuery();
            string importSchema = ResolveImportSchemaName(db, dbConnection);
            string qualifiedTableName = BuildQualifiedImportTableName(db, importSchema, randName);
            dbConnection.Close();

            f.CompleteForNetezza(randName, configDirecotry, headers, false, qualifiedTableName);

            tabName = randName;

        });
    }

    public override async Task PerformImportFromText(char escapechar, char sep, IImportProgressForm f, string db, string SelectedConnectionName)
    {
        string nl = Environment.NewLine;
        var config = _databaseRuntimeContext.Config;
        var configDirecotry = _databaseRuntimeContext.ConfigDirectory;
        var random = Random.Shared;

        string randName = StringExtension.RandomName("IMPORTED_");
        string clip = "";
        string tabName = "";
        string connectionName = SelectedConnectionName;
        try
        {
            string[] headers = default;
            string path = $"{configDirecotry}\\data\\temp_{random.Next(1, 1000)}.dat";
            string? importSchema = null;
            bool importSucceeded = false;

            f?.AddRow("Gathering data from clipboard...");
            await Task.Run(() =>
            {
                clip = Clipboard.GetText();
                if (string.IsNullOrEmpty(clip))
                {
                    _logger.Log("nothing in clipboard");
                    return;
                }
                if (clip.EndsWith(Environment.NewLine))
                {
                    clip = clip.Substring(0, clip.Length - Environment.NewLine.Length);
                }

                f?.AddRow("Data types analysing..");

                if (clip.Contains(escapechar))
                {
                    clip = clip.Replace(escapechar.ToString(), $"{escapechar}{escapechar}");
                }

                string[] lines = _generalDbService.ClipToLines(_databaseRuntimeContext.Config.PasteAsExternalSep[0], ref clip, escapechar);

                if (!config.UseSpecialSeparatorMode)
                {
                    headers = lines[0].Trim().Split(sep).Select(arg => arg.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1).Trim()).ToArray();
                }
                else
                {
                    headers = Regex.Split(lines[0], config.SpecialSeparator).Select(arg => arg.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1).Trim()).ToArray();
                }

                StringExtension.RemoveDuplicates(headers);
                Dictionary<int, Dictionary<DatabaseColumnType, int[]>> typesCount = new Dictionary<int, Dictionary<DatabaseColumnType, int[]>>();

                lines[0] = String.Join(sep, headers.Select(arg => arg.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1).Trim()));
                DatabaseColumnType nz;
                string[] v1 = new string[headers.Length];
                for (int i = 1; i < lines.Length; i++)
                {
                    if (!config.UseSpecialSeparatorMode)
                    {
                        StringExtension.SplitExcelData(lines[i], sep, v1, escapechar);
                    }
                    else
                    {
                        v1 = Regex.Split(lines[i], config.SpecialSeparator);
                        if (v1.Length != headers.Length)
                        {
                            _logger.Log($"header and {i + 1} row dont match");
                            break;
                        }
                    }
                    if (lines[i] == "")
                    {
                        continue;
                    }

                    for (int j = 0; j < v1.Length; j++)
                    {
                        nz = GetTypedValue(headers, typesCount, v1, j, doTrim: true);
                    }
                    lines[i] = String.Join(sep, v1);

                    if (i % 5000 == 0 || i == lines.Length - 1)
                    {
                        f.SetProgressBarValue((int)(100 * (i + 1)) / (lines.Length));
                    }
                }

                f.SetProgressBarValue(100);


                _importExportTasks.ChooseTypes(typesCount, headers);
                //ImportTasks.RepairDateTimestampMix(typesCount, lines);

                string serverName = $"pipe_sql_{random.Next(0, 9999)}";
                f.AddRow($"starting LinesPipeServer");
                f.AddRow(NetezzaSystemSql.GetLoadProgress(lines.Length));

                _importExportTasks.LinesPipeServer(lines, serverName, f);
                Thread.Sleep(100);

                using DbConnection dbConnection = GetConnection(db);
                dbConnection.Open();
                importSchema = ResolveImportSchemaName(db, dbConnection);

                using DbCommand cmd = dbConnection.CreateCommand();
                cmd.CommandText = NetezzaImportSql.CreateRandomDistributionTable(randName, headers);

                f.AddRow($"creating {randName}");
                cmd.ExecuteNonQuery();

                string REMOTESOURCE = "DOTNET";

                cmd.CommandText = NetezzaImportSql.InsertFromExternalPipe(randName, serverName, headers);
                string sep2 = (sep == '\t' ? "\\t" : sep.ToString());
                cmd.CommandText += @$"USING(
                    REMOTESOURCE '{REMOTESOURCE}'
                    DELIMITER '{sep2}'
                    SKIPROWS 1
                    NULLVALUE ''
                    ENCODING 'utf-8'
                    ESCAPECHAR '{escapechar}'
                    QUOTEDVALUE 'DOUBLE' 
                    TIMESTYLE '24HOUR'
                    MAXERRORS {config.ExternalMAXERRORS}
                    LOGDIR '{configDirecotry}\\data\\'
                );";


                f.AddRow($"inserting into {randName} started");
                cmd.ExecuteNonQuery();
                tabName = randName;
                importSucceeded = true;
            });

            if (importSucceeded && headers is not null)
            {
                f.CompleteForNetezza(randName, configDirecotry, headers, false,
                    BuildQualifiedImportTableName(db, importSchema, randName));
            }

        }
        catch (Exception ex)
        {
            _logger.LogError("Error during import from text", ex);
        }
    }

    public override void DoCsvOrXlsxExport(string runCommand, ISqlExecutionLog log, Stopwatch st)
    {
        var r = _databaseRuntimeContext.RxExportCsvXlsx.Match(runCommand);
        string mode = "xlsx";
        ConnectionTypes connType = ConnectionTypes.dotnet;

        string conString = _generalDbService.ConnectionStringForNz(_databaseRuntimeContext.Config.ConnectionTimeout, ConnectionName);
        string filePath = r.Groups["filePath"].Value;

        if (runCommand.StartsWith("___expCsv"))
        {
            mode = "csv";
        }

        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"started {filePath}", null);

        try
        {
            if (mode == "xlsx")
            {
                _importExportTasks.MakeSilentXlsxExport(conString, r.Groups["sql"].Value, filePath,
                    (o) =>
                    {
                        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"tranfered {o} rows", null);
                    },
                    () =>
                    {
                        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"packing started", null);
                    }
                    , connType: connType);
            }
            else if (mode == "csv")
            {
                _importExportTasks.MakeSilentCsvExport(conString, r.Groups["sql"].Value, filePath, _databaseRuntimeContext.Config.SepInExportedCsv[0], false,
                    (o) =>
                    {
                        log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"tranfered {o} rows", null);
                    }
                    , connType: connType);
            }
            log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"finished {filePath}", null);

        }
        catch (Exception ex)
        {
            log?.AppendErrorEntry(DateTime.Now, st.Elapsed.TotalSeconds, ex.Message, null);
            if (log?.View.Parent is ISuccesfullTab successTab)
            {
                void MarkFailed() => successTab.IsSuccess = false;
                if (log.View.InvokeRequired)
                    log.View.Invoke(MarkFailed);
                else
                    MarkFailed();
            }
            log?.AppendEntry(DateTime.Now, st.Elapsed.TotalSeconds, $"failed {filePath}", null);
        }
    }

    public override async Task ImportFromFile(Func<string, Encoding> getEncoding,
        Func<int, string> getName,
        Func<string[], List<string>> getTabs,
        IImportExportTasks imp, string filePath, IImportProgressForm f, string db, List<string> tableName, List<string> tabs, int skipRows = 0, bool silent = false)
    {
        Encoding? encoding = null;
        if (f is not null && (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
        {
            encoding = getEncoding?.Invoke(filePath);
        }

        var configDirecotry = _databaseRuntimeContext.ConfigDirectory;
        var config = _databaseRuntimeContext.Config;
        if (tabs != null && tabs.Count == 1 && tabs[0] == "")
        {
            tabs = null;
        }

        // This task should determine the column list.
        try
        {
            //imp.ColumnToCheck = 1;
            imp.SkipRows = skipRows;
            await Task.Run(() =>
            {
                imp.ReadAndMakeTextFileNewPart1(filePath, $"{configDirecotry}\\data\\{Path.GetFileName(filePath)}" + "forImport", config.SepInExternal[0], f, onlyFirstTab: false, null, Int64.MaxValue - 10, tabs, encoding: encoding);
            });
            if (imp.SheetNames is null)
            {
                return;
            }


            bool doCancel = false;
            if (imp.SheetNames.Length > 0 && f is not null && tabs is null)
            {

                var tmpTabs = getTabs?.Invoke(imp.SheetNames);

                if (tmpTabs != null && tmpTabs.Count > 0)
                {
                    tabs = tmpTabs;
                }
                else
                {
                    doCancel = true;
                }

            }
            if (doCancel)
            {
                imp.DisposeFile();
                f?.Close();
                return;
            }

            await Task.Run(() =>
            imp.ReadAndMakeTextFileNewPart2(filePath, $"{configDirecotry}\\data\\{Path.GetFileName(filePath)}" + "forImport",
            config.SepInExternal[0], f, onlyFirstTab: false, null, Int64.MaxValue - 10, tabs));


            int num = imp.TabsTablesColumns.Count;
            if (tabs is not null)
            {
                num = tabs.Count;
            }
            f?.AddRow($"{num} files for external table");


            if (f is not null && tabs is null)
            {
                tabs = new List<string> { imp.SheetNames[0] };
            }

        }
        catch (Exception ex)
        {
            if (!silent)
            {
                if (filePath.EndsWith("csv", StringComparison.OrdinalIgnoreCase) &&
                    ex is ArgumentOutOfRangeException)
                {
                    _logger.LogError("Error during import from csv", ex);
                    f?.Close();
                }
                else
                {
                    _logger.LogError("Error during import from file", ex);
                }
            }
            else
            {
                throw new Exception(ex.Message);
            }

            return;
        }

        try
        {
            string name = null;
            bool existingTable = false;
            if (!silent && config.ImportExisting && imp.TabsTablesColumns.Count == 1)
            {
                var res = _logger.LogYesNo("import to existing table? ");
                if (res)
                {
                    string key = imp.TabsTablesColumns.Keys.ToArray()[0];
                    int colNum = imp.TabsTablesColumns[key].headersDic.Count;

                    existingTable = true;
                    name = getName(colNum);
                    if (name == null)
                    {
                        _logger.Log("No table selected for import");
                        return;
                    }
                }
            }

            await Task.Run(() =>
            {                
                using var dbConnection = GetConnection(db);
                dbConnection.Open();
                string importSchema = ResolveImportSchemaName(db, dbConnection);
                int i = 0;
                foreach (var item in imp.TabsTablesColumns)
                {
                    if (tableName != null && tableName.Count == imp.TabsTablesColumns.Count)
                    {
                        if (tabs != null && tableName != null)
                        {
                            name = tableName[tabs.IndexOf(item.Key)];
                        }
                        else
                        {
                            name = tableName[i++];
                        }

                    }
                    _importExportTasks.ImportAction(f, item, dbConnection, config, configDirecotry, name, existingTable, db, importSchema);
                }
                //dbConnection.Close();

            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during import from file", ex);
        }
    }


    protected override void DbSpecificImportPart(string randName, DataTable source, int NotifyAfter, Action<string> progress, bool tableExists = false, IDataReader rdr = null)
    {
        char sepInExternal = _databaseRuntimeContext.Config.SepInExternal[0];
        string nl = Environment.NewLine;
        DbCommand cmd;
        string createCmd = "";

        DbDataReader rdrSource = (DbDataReader)rdr;

        string serverName = $"pipe_sql_{Random.Shared.Next(0, 9999)}";

        _importExportTasks.DBReaderStreamPipeServer(rdrSource, serverName, (o) => progress?.Invoke(o.ToString()));

        Thread.Sleep(100);

        using DbConnection dbConnection = GetConnection(null);
        dbConnection.Open();


        if (!tableExists)
        {
            string[] headers;

            if (rdrSource is NzDataReader)
            {
                headers = _importExportTasks.GetHeaders(rdrSource, "NZ");
            }
            else
            {
                headers = _importExportTasks.GetHeaders(rdrSource, null);
            }

            createCmd = NetezzaImportSql.CreateRandomDistributionTable(randName, headers);

            cmd = dbConnection.CreateCommand();
            cmd.CommandText = createCmd;
            cmd.ExecuteNonQuery();
            cmd.CommandText = NetezzaImportSql.InsertFromExternalPipe(randName, serverName, headers);
        }
        else
        {
            cmd = dbConnection.CreateCommand();
            cmd.CommandText = "";
            cmd.CommandText = NetezzaImportSql.InsertSameAsFromExternalPipe(randName, serverName);
        }

        string REMOTESOURCE = "DOTNET";


        cmd.CommandText += @$"
USING(
                REMOTESOURCE '{REMOTESOURCE}'
                DELIMITER '{sepInExternal}'
                RecordDelim '\n'
                SKIPROWS 1
                NULLVALUE ''
                ENCODING 'utf-8'
                ESCAPECHAR '\'
                --QUOTEDVALUE 'DOUBLE' 
                --TIMESTYLE '24HOUR'
                CTRLCHARS TRUE
                LFINSTRING TRUE
                MAXERRORS {_databaseRuntimeContext.Config.ExternalMAXERRORS}
                LOGDIR '{_databaseRuntimeContext.ConfigDirectory}\\data\\'
                );";
        cmd.ExecuteNonQuery();
        dbConnection.Close();
    }

    public override DbConnection GetConnection()
    {
        return new NzConnection(ConnectionString);
    }

    private string? ResolveImportSchemaName(string databaseName, DbConnection? connection)
    {
        if (_databaseRuntimeContext.DatabaseDictionary.TryGetValue(ConnectionName, out var databases))
        {
            DatabaseInfo? match = databases.Values.FirstOrDefault(database =>
                string.Equals(database.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                if (connection is NzConnection netezzaConnection && NetezzaHelpers.SchemasOn(netezzaConnection)
                    && !string.IsNullOrWhiteSpace(match.SchemaName))
                {
                    return match.SchemaName;
                }

                if (!string.IsNullOrWhiteSpace(match.DatabaseOwner))
                {
                    return match.DatabaseOwner;
                }

                if (!string.IsNullOrWhiteSpace(match.SchemaName))
                {
                    return match.SchemaName;
                }
            }
        }

        // Do not invent ADMIN — CREATE TABLE uses the session default schema; a wrong
        // three-part name would point CompleteForNetezza SQL at a non-existent object.
        return null;
    }

    private static string? BuildQualifiedImportTableName(string databaseName, string? schemaName, string tableName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return null;
        }

        return $"{databaseName}.{schemaName}.{tableName}";
    }

    public override DbConnection GetConnection(string databaseName, bool usePool = true)
    {
        return new NzConnection(ConnectionString, databaseName, _databaseRuntimeContext.Config.CommandTimeout);
    }


    public override string SearchInViewsSource(string txtToSearch)
    {
        throw new NotImplementedException();
    }

    public override string SearchInProcedureSource(string txtToSearch)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, List<(string name, string database, string DEFINITION, string DESCRIPTION)>> ViewCache { get; set; } = [];
    public Dictionary<string, List<(string name, string database, string DEFINITION, string DESCRIPTION)>> ProcCache { get; set; } // not initialized on purpose !!! 

    public Dictionary<string, List<(string name, string database, string extobjname, string DESCRIPTION)>> ExternalCache { get; set; } = [];

    public Dictionary<string, List<(string name, string database, string refobjname, string DESCRIPTION)>> SynonymCache { get; set; } = [];

    /// <summary>
    /// procedures, externals, synonyms, views
    /// </summary>
    /// <returns></returns>
    public async Task LoadSourceTextCache()
    {
        ProcCache = [];
        ProcCache.Clear();
        ExternalCache.Clear();
        SynonymCache.Clear();
        ViewCache.Clear();
        await Task.Run(() =>
        {
            try
            {
                Parallel.ForEach(DatabaseIdToName.ToArray(), new ParallelOptions { MaxDegreeOfParallelism = _databaseRuntimeContext.Config.MaxSchemaParallelism }, dbX =>
                {
                    string db = dbX.Value;
                    using var conn = GetConnection(db, usePool: false) as NzConnection;
                    if (conn.State == ConnectionState.Open)
                    {
                        throw new Exception("Connection should not be open!");
                    }
                    conn.Open();

                    var schemas = NetezzaHelpers.SchemasOn(conn);
                    //procedures
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = NetezzaHelpers.ProcSql(db);
                        using var rdr = cmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            string key = schemas ? rdr.GetString(0) : rdr.GetString(1);
                            string name = rdr.GetString(2);
                            string definition = rdr.GetString(3);
                            string desc = rdr.GetValue(4).ToString();
                            Monitor.Enter(ProcCache);
                            if (!ProcCache.TryGetValue(key, out List<(string name, string database, string DEFINITION, string DESCRIPTION)> value))
                            {
                                value = [];
                                ProcCache[key] = value;
                            }

                            value.Add((name, db, definition, desc));
                            Monitor.Exit(ProcCache);
                        }
                    }
                    //externals
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = NetezzaHelpers.ExternalSql(db);
                        using var rdr = cmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            string key = schemas ? rdr.GetString(0) : rdr.GetString(1);
                            string name = rdr.GetString(2);
                            string definition = rdr.GetString(3);
                            string desc = rdr.GetValue(4).ToString();
                            Monitor.Enter(ExternalCache);
                            if (!ExternalCache.TryGetValue(key, out List<(string name, string database, string extobjname, string DESCRIPTION)> value))
                            {
                                value = [];
                                ExternalCache[key] = value;
                            }

                            value.Add((name, db, definition, desc));
                            Monitor.Exit(ExternalCache);
                        }
                    }
                    //synonyms
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = NetezzaHelpers.SynonymSql(db);
                        using var rdr = cmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            string key = schemas ? rdr.GetString(0) : rdr.GetString(1);
                            string name = rdr.GetString(2);
                            string definition = rdr.GetString(3);
                            string desc = rdr.GetValue(4).ToString();
                            Monitor.Enter(SynonymCache);
                            if (!SynonymCache.TryGetValue(key, out List<(string name, string database, string refobjname, string DESCRIPTION)> value))
                            {
                                value = [];
                                SynonymCache[key] = value;
                            }

                            value.Add((name, db, definition, desc));
                            Monitor.Exit(SynonymCache);
                        }
                    }
                    //views
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = NetezzaHelpers.ViewSql(db);
                        using var rdr = cmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            string key = schemas ? rdr.GetString(0) : rdr.GetString(1);
                            string name = rdr.GetString(2);
                            string definition = rdr.GetString(3);
                            string desc = rdr.GetValue(4).ToString();
                            Monitor.Enter(ViewCache);
                            if (!ViewCache.TryGetValue(key, out List<(string name, string database, string DEFINITION, string DESCRIPTION)> value))
                            {
                                value = [];
                                ViewCache[key] = value;
                            }

                            value.Add((name, db, definition, desc));
                            Monitor.Exit(ViewCache);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message);
            }
        });
    }
}
