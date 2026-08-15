using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AppBase.Data.MsSqlDb;

public sealed class MsSqlDb : GeneralDb
{
    public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.MsSqlDb;
    public MsSqlDb(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, IImportExportTasks importExportTasks, IGeneralDbService generalDbService) : base(databaseRuntimeContext, logger, importExportTasks, generalDbService)
    {
        top = true;
    }

    public DataTable _schemas;
    public DataTable _dtDatabases;


    public record SqlServerJobs
    {
        public string Name { get; init; }
        public int Enabled { get; init; }
        public string Description { get; init; }
        public DateTime Created { get; init; }
        public DateTime Modified { get; init; }
    }

    Dictionary<string, SqlServerJobs> _jobs = new Dictionary<string, SqlServerJobs>();
    public Dictionary<string, SqlServerJobs> Jobs => _jobs;

    /// <summary>
    /// Optional confirmation hook for an untrusted server certificate. When
    /// the login fails because the certificate chain is not trusted, the
    /// provider asks the hook and, on acceptance, retries once with
    /// TrustServerCertificate=True.
    /// </summary>
    public Func<Exception, bool>? ConfirmUntrustedCertificate { get; set; }

    public override void InitDb()
    {
        try
        {
            InitDbCore();
        }
        catch (Exception ex) when (IsUntrustedCertificateTrustError(ex))
        {
            if (ConnectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase)
                || ConfirmUntrustedCertificate is null
                || !ConfirmUntrustedCertificate(ex))
            {
                throw;
            }

            ConnectionString = ConnectionString.TrimEnd() + ";TrustServerCertificate=True;";
            InitDbCore();
        }
    }

    private static bool IsUntrustedCertificateTrustError(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            // Prefer the Win32/security error codes over localized message text.
            // Schannel surfaces SEC_E_CERT_UNTRUSTED (0x80090325) for the
            // "certificate chain was issued by an authority that is not trusted"
            // login failure; CERT_E_UNTRUSTED* cover the certificate-store
            // validation range (0x800B0109..0x800B0112). The native code may
            // surface either as the full HRESULT or as its low 16 bits.
            if (current is Win32Exception win32)
            {
                uint code = unchecked((uint)win32.NativeErrorCode);
                if (code is 0x80090325 or 0x800B0109 or 0x800B010B or 0x800B010D or 0x800B0112
                    || (code & 0xFFFF) is 0x0325 or 0x0109 or 0x010B or 0x010D or 0x0112)
                {
                    return true;
                }
            }

            // Fallback for providers that surface a managed message without
            // the Win32 error code. Matches the canonical English and Polish
            // untrusted-chain texts only.
            string message = current.Message ?? string.Empty;
            if (message.Contains("certificate chain", StringComparison.OrdinalIgnoreCase)
                || message.Contains("certyfikat", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void InitDbCore()
    {
        using (SqlConnection conn = new SqlConnection(ConnectionString))
        {
            conn.Open();
            _dtDatabases = conn.GetSchema("Databases");
            DatabaseList = new List<string>();
            //tables = conn.GetSchema("Tables"); // owner, table_name, type
            //views = conn.GetSchema("Views");
            //procedures = conn.GetSchema("Procedures");


            tables = new DataTable();
            views = new DataTable();
            procedures = new DataTable();
            _schemas = new DataTable();
            foreach (DataRow row in _dtDatabases.Rows)
            {
                DatabaseList.Add(row[0].ToString());

                try
                {
                    using (SqlCommand cmd = new SqlCommand($"SELECT * FROM {row[0]}.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn))
                    {
                        var reader = cmd.ExecuteReader();
                        tables.Load(reader);
                    }
                }
                catch (SqlException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Skipping inaccessible SQL Server table metadata (error {ex.Number}).");
                }
                try
                {
                    using (SqlCommand cmd = new SqlCommand($"SELECT * FROM {row[0]}.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'VIEW'", conn))
                    {
                        var reader = cmd.ExecuteReader();
                        views.Load(reader);
                    }
                }
                catch (SqlException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Skipping inaccessible SQL Server view metadata (error {ex.Number}).");
                }
                try
                {
                    using (SqlCommand cmd = new SqlCommand($"select * from {row[0]}.INFORMATION_SCHEMA.SCHEMATA", conn))
                    {
                        var reader = cmd.ExecuteReader();
                        _schemas.Load(reader);
                    }
                }
                catch (SqlException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Skipping inaccessible SQL Server schema metadata (error {ex.Number}).");
                }
                try
                {
                    using (SqlCommand cmd = new SqlCommand($"SELECT * FROM {row[0]}.INFORMATION_SCHEMA.ROUTINES", conn))
                    {
                        var reader = cmd.ExecuteReader();
                        procedures.Load(reader);
                    }
                }
                catch (SqlException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Skipping inaccessible SQL Server routine metadata (error {ex.Number}).");
                }
            }

            //synonyms = conn.GetSchema("Synonyms"); // owner, synonym name, table owner, table name, db link, origin_con_i

            _jobs.Clear();
            try
            {
                using (SqlCommand cmd = new SqlCommand($"SELECT J.job_id, J.name, J.enabled, J.description, J.date_created, J.date_modified FROM MSDB.DBO.SYSJOBS J", conn))
                {
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        _jobs[rdr.GetValue(0).ToString()] = new SqlServerJobs()
                        {
                            Name = rdr.GetString(1),
                            Enabled = rdr.GetByte(2),
                            Description = rdr.GetString(3),
                            Created = rdr.GetDateTime(4),
                            Modified = rdr.GetDateTime(5),
                        };
                    }
                }
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Trace.WriteLine($"Skipping inaccessible SQL Server Agent metadata (error {ex.Number}).");
            }

            DefaultDatabaseName = conn.Database;
            conn.Close();
        }
    }

    public override void ResetDynamicCollection()
    {
        ResetDynamicCollectionH();

        foreach (DataRow thisDb in _dtDatabases.Rows)
        {
            AutocompleteSuggestions.OneWord.Add(thisDb.ItemArray[0].ToString());
        }

        foreach (DataRow user in _schemas.Rows)
        {
            string db = user.ItemArray[0].ToString();
                AutocompleteSuggestions.TwoWords.Add($"{user.ItemArray[0]}.{user.ItemArray[1]}");
        }
        foreach (DataRow item in tables.Rows)//owner, name,type
        {
            string tabName = QuoteIfNeeded(item.ItemArray[2] as string);
                AutocompleteSuggestions.TreeWords.Add($"{item.ItemArray[0]}.{item.ItemArray[1]}.{tabName}");
        }

        foreach (DataRow item in views.Rows)
        {
            string tabName = QuoteIfNeeded(item.ItemArray[2] as string);
                AutocompleteSuggestions.TreeWords.Add($"{item.ItemArray[0]}.{item.ItemArray[1]}.{tabName}");
        }

        /*
        foreach (DataRow thisDb in dtDatabases.Rows)
        {
            string db = thisDb.ItemArray[0] as string;
            DynamicCollectionForGeneral.oneWord.Add(db);
            foreach (DataRow itx in schemas.Rows)
            {
                var schema = itx.ItemArray[1];

                DataRow[] tableCol;
                tableCol = tables?.Select($"TABLE_SCHEMA = '{schema}' AND TABLE_CATALOG = '{db}'");

                foreach (DataRow item in tableCol)//owner, name,type
                {
                    string tabName = item.ItemArray[2] as string;
                    if (!tabName.IsGoodName())
                    {
                        tabName = $"\"{tabName}\"";
                    }
                    DynamicCollectionForGeneral.TreeWords.Add($"{db}.{schema}.{tabName}");
                }

                DataRow[] viewCol;
                viewCol = views?.Select($"TABLE_SCHEMA = '{schema}' AND TABLE_CATALOG = '{db}'");

                foreach (DataRow item in viewCol)
                {
                    DynamicCollectionForGeneral.TreeWords.Add($"{db}.{schema}.{item.ItemArray[2]}");
                }
            }

        }
        */
    }

    /// <summary>
    /// SQL Server accepts unquoted identifiers matching /^[A-Za-z_][A-Za-z0-9_]*$/,
    /// including lowercase names. Mirrors the VSCode extension insert-text rule
    /// (identifierUtils.formatIdentifierForSql) with one addition: names that are
    /// reserved words (order, user, select, ...) are also emitted with double
    /// quotes, since unquoted reserved words are a T-SQL syntax error. Only the
    /// identifier pattern and the shared reserved-word list trigger quoting.
    /// </summary>
    private string QuoteIfNeeded(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (name[0] == '_' || char.IsAsciiLetter(name[0]))
        {
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_' || char.IsAsciiLetterOrDigit(c))
                {
                    continue;
                }

                return $"\"{name}\"";
            }

            return _generalDbService.ReservedWords?.Contains(name.ToUpperInvariant()) == true
                ? $"\"{name}\""
                : name;
        }

        return $"\"{name}\"";
    }

    protected override void AddToCache(string dbname, string schema, string tablename)
    {
        string SQL = @$"
                    SELECT 
                        S.COLUMN_NAME
                        , CASE WHEN S.DATA_TYPE IN('nvarchar', 'varchar', 'nchar') then S.DATA_TYPE + '(' + CAST(S.CHARACTER_OCTET_LENGTH AS VARCHAR) +')' 
                            WHEN DATA_TYPE IN('money','decimal','numeric') then S.DATA_TYPE + '(' + CAST(S.NUMERIC_PRECISION AS VARCHAR) + ',' + CAST(S.NUMERIC_SCALE AS VARCHAR) + ')'
                                                     ELSE S.DATA_TYPE
                                                     END 
                                                 + CASE WHEN S.IS_NULLABLE = 'YES' THEN '' ELSE ' NOT NULL' END
                                                 + CASE WHEN S.COLUMN_DEFAULT IS NOT NULL THEN ' DEFAULT ' + S.COLUMN_DEFAULT ELSE '' END
                                            AS TYPENAME
                        , case when col.CONSTRAINT_NAME is not null then 1 else -1 end 
                        , prop.value AS COLUMN_DESCRIPTION
                    FROM 
                        {dbname}.INFORMATION_SCHEMA.COLUMNS S
                        INNER JOIN {dbname}.sys.columns AS sc ON sc.object_id = object_id('{dbname}.'+S.table_schema + '.' +S.table_name) AND sc.NAME = S.COLUMN_NAME
                        LEFT JOIN {dbname}.INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col ON Col.COLUMN_NAME = S.COLUMN_NAME
                                    and Col.TABLE_SCHEMA ='dbo'
                                    and Col.TABLE_CATALOG = 'AdventureWorksDW2019'
                                    AND Col.TABLE_NAME = S.TABLE_NAME
                    LEFT JOIN {dbname}.sys.extended_properties prop ON prop.major_id = sc.object_id
                        AND prop.minor_id = sc.column_id
                        AND prop.NAME = 'MS_Description'
                    WHERE 
                        S.TABLE_NAME = '{tablename}'  
                        and S.TABLE_SCHEMA ='{schema}'
                        and S.TABLE_CATALOG = '{dbname}'
                    ORDER BY 
                        S.TABLE_CATALOG,S.TABLE_SCHEMA, S.ORDINAL_POSITION;";


        using (var conn = new SqlConnection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new SqlCommand(SQL, conn))
            {
                var rdr = cmd.ExecuteReader();

                List<string> ls = new List<string>();
                List<string> ls2 = new List<string>();
                List<short> ls3 = new List<short>();
                List<string> ls4 = new List<string>();
                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0));
                    ls2.Add(rdr.GetString(1));
                    var o = rdr.GetInt32(2);
                    //if (o == DBNull.Value)
                    //{
                    //    o = (short)-1;
                    //}
                    ls3.Add((short)o);

                    if (rdr.GetValue(3) != DBNull.Value)
                    {
                        ls4.Add(rdr.GetString(3));
                    }
                    else
                    {
                        ls4.Add(null);
                    }
                }
                columnsOfTables[dbname + "_" + schema + "\\" + tablename] = (ls.ToArray(), ls2.ToArray(), ls3.ToArray(), ls4.ToArray());
            }
            conn.Close();
        }
    }

    protected override void DbSpecificImportPart(string randName, DataTable source, int NotifyAfter, Action<string> progress, bool tableExists = false, IDataReader rdr = null)
    {
        using (var conn = new SqlConnection(ConnectionString))
        {
            conn.Open();
            if (!tableExists)
            {
                string[] headers = new string[source.Columns.Count];
                for (int i = 0; i < headers.Length; i++)
                {
                    string typeName = "varchar(255)";
                    if (source.Columns[i].DataType == typeof(int))
                    {
                        typeName = "integer";
                    }
                    else if (source.Columns[i].DataType == typeof(long))
                    {
                        typeName = "bigint";
                    }
                    else if (source.Columns[i].DataType == typeof(float) || source.Columns[i].DataType == typeof(double))
                    {
                        typeName = "float";
                    }
                    else if (source.Columns[i].DataType == typeof(decimal))
                    {
                        typeName = "numeric(20,6)";
                    }
                    else if (source.Columns[i].DataType == typeof(DateTime))
                    {
                        typeName = "datetime";
                    }
                    headers[i] = $"{source.Columns[i].ColumnName.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1)} {typeName}";
                }
                string SQL = $"CREATE TABLE {randName} ({String.Join(',', headers)})";

                using (SqlCommand cmd = new SqlCommand(SQL, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            SqlBulkCopy cpy = new SqlBulkCopy(conn);
            cpy.DestinationTableName = randName;
            cpy.NotifyAfter = NotifyAfter;
            cpy.SqlRowsCopied += Cpy_RowsCopied;

            if (rdr == null)
            {
                cpy.WriteToServer(source);
            }
            else
            {
                cpy.WriteToServer(rdr);
            }

            cpy.Close();
            conn.Close();
        }
    }

    private void Cpy_RowsCopied(object sender, SqlRowsCopiedEventArgs e)
    {
        ImportedSomeRows(e.RowsCopied.ToString("N0"));
    }

    public override string GetCreateTableText(string schemaTablename) //schema.Tablename
    {
        return $"sp_help '{schemaTablename}';";
    }

    public override string GetCreateViewText(string schemaTablename)
    {
        return $"sp_help '{schemaTablename}';";
    }

    public override string GetCreateTableText(string dbName, string schema, string tablename)
    {
        return $"sp_help '{dbName}.{schema}.{tablename}';";
    }

    public override string GetCreateViewText(string dbName, string schema, string viewName)
    {
        //string SQL = $"SELECT TEXT FROM SYSCAT.VIEWS WHERE VIEWSCHEMA = '{schema}' AND VIEWNAME = '{viewName}';";

        string sql = @$"SELECT definition
            FROM {dbName}.sys.sql_modules  
            WHERE object_id = OBJECT_ID('{dbName}.{schema}.{viewName}')";

        string viewText = "";
        using (var conn = GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = 30;
                viewText = cmd.ExecuteScalar() as string;
            }
            conn.Close();
        }

        return $"{viewText};";
    }


    public override DbConnection GetConnection()
    {
        return new SqlConnection(ConnectionString);
    }


    public override string GetCreatePorcedureText(string schema, string procName)
    {
        string sql = $"SELECT ROUTINE_DEFINITION FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA = '{schema}' AND SPECIFIC_NAME = '{procName}';";
        string procTxt = "";
        using (var conn = GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = 30;
                procTxt = cmd.ExecuteScalar() as string;
            }
            conn.Close();
        }

        return $"{procTxt};";
    }


    public override string SearchInViewsSource(string txtToSearch)
    {
        txtToSearch = Regex.Escape(txtToSearch);
        string sql = $@"SELECT 
    'Views'  AS ""Type""
    , TABLE_NAME as ""Name""
    , DB_NAME() AS ""Db""
    , '' AS ""DESC""
    , TABLE_SCHEMA as ""Schema""
FROM 
    INFORMATION_SCHEMA.VIEWS 
WHERE 
    (' ' + VIEW_DEFINITION + ' ') LIKE '%[^a-z]{txtToSearch}[^a-z]%';";

        return sql;
    }

    public override string SearchInProcedureSource(string txtToSearch)
    {
        txtToSearch = Regex.Escape(txtToSearch);
        string sql = $@"SELECT 
    'Procedures' AS ""Type""
    , SPECIFIC_NAME as ""Name""
    , DB_NAME() AS ""Db""
    , '' AS ""DESC""
    , ROUTINE_SCHEMA as ""Schema""
FROM 
    INFORMATION_SCHEMA.ROUTINES
WHERE 
    (' ' + ROUTINE_DEFINITION + ' ') LIKE '%[^a-z]{txtToSearch}[^a-z]%'";
        return sql;
    }

    public override DbConnection GetConnection(string databaseName, bool usePool = true)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return GetConnection();
        }

        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = databaseName
        };
        return new SqlConnection(builder.ConnectionString);
    }
}
