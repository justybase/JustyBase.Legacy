using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace AppBase.Data.Postgres;

public sealed class Postgres : GeneralDb
{
    public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.Postgres;
    public Postgres(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, IImportExportTasks importExportTasks, IGeneralDbService generalDbService) : base(databaseRuntimeContext, logger, importExportTasks, generalDbService)
    {

    }

    public DataTable _users = new DataTable();
    public DataTable _sequences = new DataTable();

    protected override void DbSpecificImportPart(string randName, DataTable source, int NotifyAfter, Action<string> progress, bool tableExists = false, IDataReader rdr = null)
    {
        using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
        {
            conn.Open();
            if (!tableExists)
            {
                var transaction = conn.BeginTransaction();

                string[] headers = new string[source.Columns.Count];
                string[] rawheaders = new string[source.Columns.Count];
                string[] types = new string[source.Columns.Count];
                for (int i = 0; i < headers.Length; i++)
                {
                    string typeName = "VARCHAR(255)";
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
                    rawheaders[i] = source.Columns[i].ColumnName.NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1);
                    types[i] = typeName;
                }

                string SQL = $"CREATE TABLE {randName} ({String.Join(',', headers)})";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.ExecuteNonQuery();
                }

                using (var writer = conn.BeginTextImport($"COPY {randName} ({String.Join(',', rawheaders)}) FROM STDIN"))
                {
                    int n = source.Rows.Count;
                    int m = source.Columns.Count;
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < m; j++)
                        {
                            var item = source.Rows[i][j];
                            if (item != DBNull.Value)
                            {
                                if (types[j] == "DOUBLE PRECISION" || types[j].StartsWith("NUMERIC"))
                                {
                                    writer.Write(item.ToString().Replace(',', '.'));
                                }
                                else if (types[j] == "TIMESTAMP")
                                {
                                    writer.Write(((DateTime)item).ToString("yyyy-MM-ddTHH:mm:ssZ"));
                                }
                                else
                                {
                                    writer.Write(item);
                                }
                            }

                            if (j < m - 1)
                            {
                                writer.Write('\t');
                            }
                        }

                        writer.Write('\n');
                    }
                }
                transaction.Commit();
            }
            conn.Close();
        }
    }

    public DataTable dbs = new DataTable();
    public override void InitDb()
    {
        using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
        {
            conn.Open();
            dbs = conn.GetSchema("Databases");
            string sql = //"SELECT table_schema, table_name,table_type FROM INFORMATION_SCHEMA.TABLES ORDER BY 1,3,2;" +
                @"SELECT n.nspname AS table_schema
                     , c.relname AS table_name
                     , CASE c.relkind
                         WHEN 'v' THEN 'VIEW'
                         WHEN 'p' THEN 'BASE TABLE'
                         WHEN 'r' THEN 'BASE TABLE'
                         ELSE 'unknown table type'
                       END AS table_type
                FROM   pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                WHERE
                not c.relispartition
                and c.relkind in ('v', 'p', 'r')
                ORDER BY 1,3,2; " +
                "SELECT routine_schema,routine_name,routine_type,data_type,routine_body,routine_definition,external_language, parameter_style FROM information_schema.routines R;" +
                "select s.sequence_schema, s.sequence_name from information_schema.sequences s";

            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                var rdr = cmd.ExecuteReader();

                tables = new DataTable();
                tables.Columns.Add("table_schema");
                tables.Columns.Add("tableName");

                _users = new DataTable();
                _users.Columns.Add("table_schema");

                views = new DataTable();
                views.Columns.Add("table_schema");
                views.Columns.Add("viewname");

                Dictionary<string, string> schamaDic = new Dictionary<string, string>();
                while (rdr.Read())
                {
                    string el = rdr.GetString(0);
                    if (!schamaDic.ContainsKey(el))
                    {
                        schamaDic[el] = el;
                    }
                    string tableName = rdr.GetString(1);
                    string tableType = rdr.GetString(2);

                    if (tableType == "VIEW")
                    {
                        views.Rows.Add(new string[] { el, tableName });
                    }
                    else if (tableType == "BASE TABLE")
                    {
                        tables.Rows.Add(new string[] { el, tableName });
                    }
                }

                rdr.NextResult();
                procedures = new DataTable();
                procedures.Load(rdr);

                //rdr.NextResult();
                _sequences.Load(rdr);

                foreach (var item in schamaDic.Keys)
                {
                    _users.Rows.Add(new string[] { item });
                }
            }
            DefaultDatabaseName = conn.Database;
            conn.Close();
        }
    }


    public override void ResetDynamicCollection()
    {
        ResetDynamicCollectionH();
        if (_users == null)
        {
            throw new Exception("problem with users - posgres - resetDynamicCollection");
        }
        if (_users.Columns.Count <= 2)
        {
            return;
        }
        foreach (DataRow user in _users.Rows)
        {
            AutocompleteSuggestions.OneWord.Add(user.ItemArray[1] as string);
        }

        foreach (DataRow itx in _users.Rows)
        {
            var user = itx.ItemArray[1];

            DataRow[] tableCol;
            tableCol = tables?.Select($"TABLE_SCHEMA = '{user}' AND TABLE_CATALOG = '{DefaultDatabaseName}'");

            foreach (DataRow item in tableCol)//owner, name,type
            {
                string tabName = item.ItemArray[2] as string;
                if (!tabName.IsGoodName())
                {
                    tabName = $"\"{tabName}\"";
                }
                AutocompleteSuggestions.TwoWords.Add($"{user}.{tabName}");
            }

            DataRow[] viewCol;
            viewCol = views?.Select($"TABLE_SCHEMA = '{user}' AND TABLE_CATALOG = '{DefaultDatabaseName}'");

            foreach (DataRow item in viewCol)
            {
                AutocompleteSuggestions.TwoWords.Add($"{user}.{item.ItemArray[2]}");
            }
        }
    }

    protected override void AddToCache(string dbname, string schema, string tablename)
    {
        AddToCacheStandard(dbname, schema, tablename);
    }

    public override DbConnection GetConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }


    public Dictionary<String, List<String>> indexes;
    const string indexesSql = @"SELECT
                        tablename
                        , indexname
                        , indexdef
                        , i.tablespace
                    FROM
                        pg_indexes i
                        LEFT JOIN information_schema.table_constraints PK on
                            PK.constraint_type = 'PRIMARY KEY'
                            AND i.indexname = pk.constraint_name
                    WHERE 
                        PK.constraint_type is null    
                    ORDER BY 1,2";
    private string getIndexes(string tablename)
    {
        if (indexes == null)
        {
            indexes = new Dictionary<string, List<String>>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(indexesSql, conn))
                {
                    var rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            if (!indexes.ContainsKey(rdr.GetString(0)))
                            {
                                indexes[rdr.GetString(0)] = new List<string>();
                            }
                            indexes[rdr.GetString(0)].Add(rdr.GetString(2));
                        }
                    }
                }
            }
        }

        if (!indexes.ContainsKey(tablename))
        {
            return "";
        }
        return String.Join(";" + Environment.NewLine, indexes[tablename]) + ";";
    }


    protected override void AddToPartitionCache(string dbName, string schema, string tablename)
    {
        string partitonSqlTxt = partitonSql1(schema, tablename);

        List<string> partitions = new List<string>();
        using (var conn = new NpgsqlConnection(ConnectionString))
        {
            conn.Open();

            using (var cmd = new NpgsqlCommand(partitonSqlTxt, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    partitions.Add(rdr.GetString(1));
                }
            }
        }
        partitionsOfTable[dbName + "_" + schema + "\\" + tablename] = (partitions.ToArray(), "info");
    }


    private static string partitonSql1(string schema, string tablename)
    {
        return $@"
select 
    'CREATE TABLE IF NOT EXISTS ' || pt.relname || ' PARTITION OF ' || base_tb.relname || ' ' || pg_get_expr(pt.relpartbound, pt.oid, true) || ';' AS PART
, pt.relname
from pg_class base_tb 
join pg_inherits i on i.inhparent = base_tb.oid 
join pg_class pt on pt.oid = i.inhrelid
where 
  base_tb.oid = '{schema}.{tablename}'::regclass;";
    }

    public override string GetCreateTableText(string dbName, string schema, string tablename)
    {

        string res = $"CREATE TABLE {schema}.{tablename}{Environment.NewLine}({Environment.NewLine}    ";
        string sql = @$"SELECT 
                            column_name
                            , data_type
                            , character_maximum_length
                            , is_nullable
                            , column_default
                            , collation_name
                            , numeric_precision
                            , numeric_scale
                            , numeric_precision_radix
                        FROM information_schema.columns 
                        WHERE 
                            table_name = '{tablename}'
                            and table_schema = '{schema}'
                        ORDER BY 
                            ordinal_position";
        string sqlConstraints = $@"    SELECT 
                            C1.constraint_name
                            , C1.constraint_type
                            , C1.enforced
                            , string_agg(C2.column_name, ',')
                            , C3.constraint_schema
                            , C3.match_option
                            , C3.update_rule
                            , C3.delete_rule
                            , C2.table_name
                            , C2.table_schema
                            , X1.colsForFk
                            , C4.check_clause
                        FROM 
                            information_schema.table_constraints C1
                            JOIN information_schema.constraint_column_usage C2 ON C2.constraint_name = C1.constraint_name 
                            LEFT JOIN information_schema.referential_constraints C3 ON C3.constraint_name = C1.constraint_name
                            LEFT JOIN 
                            (
                            SELECT 
                                a.constraint_name
                                , string_agg(column_name, ',') as colsForFk
                            FROM 
                                information_schema.key_column_usage a
                            GROUP BY 
                                1
                            ) 
                            X1 ON X1.constraint_name = C1.constraint_name 
                            LEFT JOIN information_schema.check_constraints C4 ON
                                C4.constraint_name = C1.constraint_name 
                        WHERE 
                             C1.table_schema = '{schema}' and C1.table_name = '{tablename}'
                        GROUP BY
                            1,2,3,5,6,7,8,9,10,11,12
                        ORDER BY 
                            C1.constraint_type DESC";

        string sqlTriggers = $@"select
                'CREATE TRIGGER ' || trigger_name || ' ' || action_timing || '
                ' ||
                string_agg(event_manipulation, ' OR ') ||'
                 ON ' ||event_object_schema||'.'||event_object_table ||
                ' FOR EACH ROW ' || action_statement

                FROM information_schema.triggers
                WHERE 
                 event_object_schema = '{schema}' and event_object_table = '{tablename}'
                 group by trigger_name, action_timing, event_object_schema , event_object_table, action_statement";

        sql = sql + ";" + sqlConstraints + ";" + sqlTriggers;
        List<string> triggers = new();
        List<string> arr = new();
        using (var conn = new NpgsqlConnection(ConnectionString))
        {
            conn.Open();

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string dataType = rdr.GetString(1);
                    var len = rdr.GetValue(2);
                    var prec = rdr.GetValue(6);
                    var scale = rdr.GetValue(7);
                    string lenD = "";
                    string collation = "";
                    if (len != DBNull.Value)
                    {
                        lenD = $"({len})";
                        var collation_name = rdr.GetValue(5);
                        if (collation_name != DBNull.Value)
                        {
                            collation = " to do from information_schema.collations";
                        }
                        else
                        {
                            collation = " COLLATE pg_catalog.\"default\"";
                        }
                    }
                    else if (prec != DBNull.Value)
                    {
                        lenD = $"({prec},{scale})";
                    }

                    string isNull = rdr.GetString(3);
                    if (isNull == "NO")
                    {
                        isNull = " NOT NULL";
                    }
                    else
                    {
                        isNull = "";
                    }

                    var colDef = rdr.GetValue(4);
                    string colDefString = "";
                    if (colDef != DBNull.Value)
                    {
                        colDefString = $" DEFAULT {colDef}";
                    }
                    arr.Add(rdr.GetString(0) + " " + dataType + lenD + collation + isNull + colDefString);
                }

                rdr.NextResult();
                List<string> cnsArr = new();

                while (rdr.Read())
                {
                    string keyType = rdr.GetString(1);
                    if (keyType == "FOREIGN KEY")
                    {
                        string matchOpt = rdr.GetString(5);
                        matchOpt = (matchOpt == "NONE" ? "SIMPLE" : matchOpt);
                        cnsArr.Add("CONSTRAINT " + rdr.GetString(0) + " " + keyType + $"({rdr.GetString(10)}) REFERENCES {rdr.GetString(9)}.{rdr.GetString(8)}({rdr.GetString(3)})" +
                            $" MATCH {matchOpt}" + Environment.NewLine +
                            $"        ON UPDATE {rdr.GetString(6)}{Environment.NewLine}        ON DELETE {rdr.GetString(7)}");
                        //add = " FK to do REFERENCES, march, on update on delete information_schema.referential_constraints ";
                    }
                    else if (keyType == "PRIMARY KEY")
                    {
                        cnsArr.Add("CONSTRAINT " + rdr.GetString(0) + " " + keyType + $"({rdr.GetString(10)})");
                    }
                    else
                    {
                        cnsArr.Add("CONSTRAINT " + rdr.GetString(0) + " " + keyType + $" {rdr.GetString(11)}");
                    }
                }
                arr.AddRange(cnsArr);
                rdr.NextResult();
                while (rdr.Read())
                {
                    triggers.Add(rdr.GetString(0) + ";");
                }

            }
        }


        string partitonSqlTxt = partitonSql1(schema, tablename);

        List<string> partitions = new List<string>();
        using (var conn = new NpgsqlConnection(ConnectionString))
        {
            conn.Open();

            using (var cmd = new NpgsqlCommand(partitonSqlTxt, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    partitions.Add(rdr.GetString(0));
                }
            }
        }

        sql = $@"
select 
    par.relnamespace::regnamespace::text as schema, 
    par.relname as table_name, 
    partnatts as num_columns,
    column_index,
    col.column_name,
    pt.partition_strategy
from   
    (select
         partrelid,
         partnatts,
         case partstrat 
              when 'l' then 'list' 
              when 'r' then 'range' end as partition_strategy,
         unnest(partattrs) column_index
     from
         pg_partitioned_table) pt 
join   
    pg_class par 
on     
    par.oid = pt.partrelid
join
    information_schema.columns col
on  
    col.table_schema = par.relnamespace::regnamespace::text
    and col.table_name = par.relname
    and ordinal_position = pt.column_index
WHERE 
    par.relnamespace::regnamespace::text = '{schema}'
    AND par.relname = '{tablename}'
ORDER BY 
    column_index ASC;";

        string partitonInfo = "";
        List<string> partColumns = new List<string>();
        using (var conn = new NpgsqlConnection(ConnectionString))
        {
            conn.Open();

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (string.IsNullOrWhiteSpace(partitonInfo))
                    {
                        partitonInfo = " PARTITION BY " + rdr.GetString(5) + " ";
                    }

                    partColumns.Add(rdr.GetString(4));
                }
            }
        }
        if (partitonInfo != "")
        {
            partitonInfo += "(" + String.Join(",", partColumns) + ")";
        }


        return res + String.Join("," + Environment.NewLine + "    ", arr) + Environment.NewLine + ")" +
            $"{partitonInfo};"
            + Environment.NewLine + getIndexes(tablename)
            + Environment.NewLine
            + String.Join(Environment.NewLine, triggers)
            + Environment.NewLine
            + String.Join(Environment.NewLine, partitions)
            + Environment.NewLine
            ;
    }

    public override string GetCreateViewText(string dbName, string schema, string viewName)
    {
        return GetViewCodeStandard(schema, viewName);
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
