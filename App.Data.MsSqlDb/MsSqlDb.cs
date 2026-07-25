using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using Microsoft.Data.SqlClient;
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

    public override void InitDb()
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
            DynamicCollectionForGeneralHelpers.OneWord.Add(thisDb.ItemArray[0].ToString());
        }

        foreach (DataRow user in _schemas.Rows)
        {
            string db = user.ItemArray[0].ToString();
            DynamicCollectionForGeneralHelpers.TwoWords.Add($"{user.ItemArray[0]}.{user.ItemArray[1]}");
        }
        foreach (DataRow item in tables.Rows)//owner, name,type
        {
            string tabName = item.ItemArray[2] as string;
            if (!tabName.IsGoodName())
            {
                tabName = $"\"{tabName}\"";
            }
            DynamicCollectionForGeneralHelpers.TreeWords.Add($"{item.ItemArray[0]}.{item.ItemArray[1]}.{tabName}");
        }

        foreach (DataRow item in views.Rows)
        {
            string tabName = item.ItemArray[2] as string;
            if (!tabName.IsGoodName())
            {
                tabName = $"\"{tabName}\"";
            }
            DynamicCollectionForGeneralHelpers.TreeWords.Add($"{item.ItemArray[0]}.{item.ItemArray[1]}.{tabName}");
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
        throw new NotImplementedException();
    }
}
