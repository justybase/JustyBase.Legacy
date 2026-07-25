using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace AppBase.Data.Oracle;

public sealed class Oracle : GeneralDb
{
    public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.Oracle;

    public DataTable _synonyms;
    public Oracle(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, IImportExportTasks importExportTasks, IGeneralDbService generalDbService) : base(databaseRuntimeContext, logger, importExportTasks, generalDbService)
    {
    }

    public DataTable _users;

    private readonly string _viewSql = @"SELECT OWNER, VIEW_NAME FROM SYS.ALL_VIEWS ORDER BY OWNER, VIEW_NAME";

    public override void InitDb()
    {
        using (OracleConnection conn = new OracleConnection(ConnectionString))
        {
            conn.Open();
            tables = conn.GetSchema("Tables"); // owner, table_name, type
            tables.Columns[0].ColumnName = "TABLE_SCHEMA";

            _synonyms = conn.GetSchema("Synonyms"); // owner, synonym name, table owner, table name, db link, origin_con_id

            using (var cmd = new OracleCommand(_viewSql, conn))
            {
                var rdr = cmd.ExecuteReader();
                views = new DataTable();
                views.Load(rdr);
            }
            //views = conn.GetSchema("Views");

            _users = conn.GetSchema("Users");//name, id/createdate
            procedures = conn.GetSchema("Procedures"); // owner, name, text length, text of view, ...
            DefaultDatabaseName = conn.DatabaseName;

            //using (OracleCommand cmd = new OracleCommand("select * from tabs", conn))
            //{
            //    var rdr = cmd.ExecuteReader();
            //    tables.Load(rdr);
            //}
            conn.Close();
        }
    }



    public override void ResetDynamicCollection()
    {
        ResetDynamicCollectionH();

        foreach (DataRow user in _users.Rows)
        {
            DynamicCollectionForGeneralHelpers.OneWord.Add(user.ItemArray[0] as string);
        }

        foreach (DataRow itx in _users.Rows)
        {
            var user = itx.ItemArray[0];

            DataRow[] tableCol;

            tableCol = tables?.Select($"TABLE_SCHEMA = '{user}'");

            foreach (DataRow item in tableCol)//owner, name,type
            {
                string tabName = item.ItemArray[1] as string;
                if (!tabName.IsGoodName())
                {
                    tabName = $"\"{tabName}\"";
                }
                DynamicCollectionForGeneralHelpers.TwoWords.Add($"{user}.{tabName}");
            }

            var synonymCol = _synonyms.Select($"OWNER = '{user}'");
            foreach (DataRow item in synonymCol)
            {
                DynamicCollectionForGeneralHelpers.TwoWords.Add($"{user}.{item.ItemArray[1]}");
            }

            DataRow[] viewCol;
            viewCol = views?.Select($"OWNER = '{user}'");

            foreach (DataRow item in viewCol)
            {
                DynamicCollectionForGeneralHelpers.TwoWords.Add($"{user}.{item.ItemArray[1]}");
            }
        }

    }

    protected override void AddToCache(string dbname, string schema, string tablename)
    {
        using (OracleConnection conn = new OracleConnection(ConnectionString))
        {
            conn.Open();

            var sql = @$"SELECT
                column_name, data_type || CASE WHEN NULLABLE = 'Y' THEN '' ELSE ' NOT NULL' END
                FROM ALL_TAB_COLUMNS
                WHERE OWNER = '{schema}'
                AND TABLE_NAME = '{tablename}'
                ORDER BY TABLE_NAME, column_id";

            //COMMENTS
            //SELECT OWNER, TABLE_NAME, COLUMN_NAME, COMMENTS FROM ALL_COL_COMMENTS WHERE TABLE_NAME = 'EMPLOYEES';

            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                List<string> ls = new List<string>();
                List<string> ls2 = new List<string>();
                List<short> ls3 = new List<short>();
                List<string> ls4 = new List<string>(); // ALL_COL_COMMENTS
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0));//colname
                    ls2.Add(rdr.GetString(1));//data_type
                    ls3.Add(-1);
                }
                columnsOfTables[dbname + "_" + schema + "\\" + tablename] = (ls.ToArray(), ls2.ToArray(), ls3.ToArray(), ls4.ToArray());
            }
            conn.Close();
        }
    }

    private string getDDLOracle(string schema, string tablename, string type)
    {
        string SQL = $"select dbms_metadata.get_ddl('{type}', '{tablename}', '{schema}') FROM DUAL";
        string txt = "problem";
        try
        {
            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand(SQL, conn))
                {
                    lock (_sync)
                    {
                        dbActiveCommands.Insert(0, cmd);
                    }
                    txt = cmd.ExecuteScalar() as string;
                    lock (_sync)
                    {
                        dbActiveCommands.Remove(cmd);
                    }
                }
                conn.Close();
            }
        }
        catch (Exception ex)
        {
            txt = ex.Message;
        }

        return txt;
    }

    public override string GetCreateTableText(string dbName, string schema, string tablename)
    {
        return getDDLOracle(schema, tablename, "TABLE");
    }

    public override string GetCreateViewText(string dbName, string schema, string viewName)
    {
        return getDDLOracle(schema, viewName, "VIEW");
    }

    public override string GetCreatePorcedureText(string schema, string procName)
    {
        return getDDLOracle(schema, procName, "PROCEDURE");
    }

    protected override void DbSpecificImportPart(string randName, DataTable source, int NotifyAfter, Action<string> progress, bool tableExists = false, IDataReader rdr = null)
    {
        using (OracleConnection conn = new OracleConnection(ConnectionString))
        {
            conn.Open();
            if (!tableExists)
            {
                string[] headers = new string[source.Columns.Count];
                for (int i = 0; i < headers.Length; i++)
                {
                    string typeName = "VARCHAR2(255)";
                    if (source.Columns[i].DataType == typeof(int))
                    {
                        typeName = "INTEGER";
                    }
                    else if (source.Columns[i].DataType == typeof(long))
                    {
                        typeName = "BIGINT";
                    }
                    else if (source.Columns[i].DataType == typeof(float) || source.Columns[i].DataType == typeof(double))
                    {
                        typeName = "DOUBLE PRECISION";
                    }
                    else if (source.Columns[i].DataType == typeof(decimal))
                    {
                        typeName = "NUMERIC(20,6)";
                    }
                    else if (source.Columns[i].DataType == typeof(DateTime))
                    {
                        typeName = "TIMESTAMP";
                    }
                    headers[i] = $"{source.Columns[i].ColumnName.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1)} {typeName}";
                }
                string SQL = $"CREATE TABLE {randName} ({String.Join(',', headers)})";

                using (OracleCommand cmd = new OracleCommand(SQL, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            using OracleBulkCopy cpy = new OracleBulkCopy(conn);
            cpy.BulkCopyTimeout = _databaseRuntimeContext.Config.CommandTimeout;
            cpy.NotifyAfter = (int)NotifyAfter;
            cpy.OracleRowsCopied += (o, e) => progress?.Invoke($"Copied {e.RowsCopied.ToString("N0")}");
            cpy.DestinationTableName = randName;
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


    public override DbConnection GetConnection()
    {
        return new OracleConnection(ConnectionString);
    }
    public override string SearchInViewsSource(string txtToSearch)
    {
        throw new NotImplementedException();
    }

    public override string SearchInProcedureSource(string txtToSearch)
    {
        throw new NotImplementedException();
    }

    public override DbConnection GetConnection(string databaseName, bool usePool = true)
    {
        throw new NotImplementedException();
    }
}
