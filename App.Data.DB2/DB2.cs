using AppBase.Common;
using AppBase.Common.Enums;
using AppBase.Common.Interfaces;
using AppBase.Data.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;
using AppBase.Data.Core.Models;
using IBM.Data.Db2;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;


namespace App.Data.DB2;

public sealed class DB2 : GeneralDb, IDb2MetadataCatalog
{
    public override DatabaseTypeEnum DatabaseType => DatabaseTypeEnum.DB2;

    public readonly DataTable _linkedServersDt = new DataTable();
    public readonly DataTable _linkedServersOptionsDt = new DataTable();
    public readonly DataTable _wrappersDt = new DataTable();
    public readonly DataTable _wrappersOptionsDt = new DataTable();
    public readonly DataTable _passthruDt = new DataTable();

    public readonly DataTable _userMapingsDt = new DataTable();
    private readonly string _linkedServersSql = @"SELECT WRAPNAME,SERVERNAME,SERVERTYPE,SERVERVERSION,REMARKS FROM SYSCAT.SERVERS ORDER BY SERVERNAME ASC";
    private readonly string _linkedServersOptionsSql = @"SELECT SERVERNAME,OPTION,SETTING FROM SYSCAT.SERVEROPTIONS  ORDER BY SERVERNAME,OPTION";
    private readonly string _passthruSql = @"SELECT GRANTOR,GRANTORTYPE,GRANTEE,GRANTEETYPE,SERVERNAME FROM SYSCAT.PASSTHRUAUTH ORDER BY SERVERNAME,GRANTEE,GRANTOR";

    private readonly string _wrappers = @"
SELECT 
    WRAPNAME
    , WRAPTYPE
    , WRAPVERSION
    , LIBRARY
    , REMARKS
FROM 
    SYSCAT.WRAPPERS
ORDER BY 
    WRAPNAME
";

    private readonly string wrapperOptions = @"SELECT WRAPNAME,OPTION,SETTING  FROM SYSCAT.WRAPOPTIONS ORDER BY WRAPNAME,OPTION,SETTING";


    private readonly string _userMapings = @"
SELECT 
     AUTHID
    , AUTHIDTYPE
    , SERVERNAME
    , OPTION
    , SETTING
FROM 
    SYSCAT.USEROPTIONS 
ORDER BY
    SERVERNAME, AUTHID, OPTION
";

    public long _bytesSize = 0;

    public DataTable _schemas;
    public DataTable _synonyms;
    public DataTable _nicknames;
    public DataTable _aliases;
    public DataTable functions = new();

    public IReadOnlyList<Db2CatalogObject> Db2CatalogObjects { get; private set; } = [];

    private readonly Dictionary<int, (string PROVIDER_TYPE_NAME, string CREATE_PARAMS, string SQL_TYPE_NAME)> _dataTypes = new();

    public DB2(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, IImportExportTasks importExportTasks, IGeneralDbService generalDbService) : base(databaseRuntimeContext, logger, importExportTasks, generalDbService)
    {

    }


    public static List<string> GetDatabaseList(int connectionTimeout, string server, string user, string port, string pass)
        => GetDatabaseList(connectionTimeout, server, user, port, pass, "SAMPLE");

    public static List<string> GetDatabaseList(
        int connectionTimeout,
        string server,
        string user,
        string port,
        string pass,
        string databaseName)
    {
        DB2ConnectionStringBuilder builder = new DB2ConnectionStringBuilder();
        // IBM.Data.Db2 expects the endpoint as Server=host:port; Port is not
        // a valid standalone connection-string keyword for this provider.
        builder.Add(
            "Server",
            string.IsNullOrWhiteSpace(port) ? server : $"{server}:{port}");
        builder.Add("Database", string.IsNullOrWhiteSpace(databaseName) ? "SAMPLE" : databaseName);
        builder.Add("UID", user);
        builder.Add("PWD", pass);

        List<string> list = new List<string>();
        try
        {
            using (var conn = new DB2Connection(builder.ConnectionString))
            {

                conn.Open();
                using (var cmd = new DB2Command("SELECT DISTINCT DB_NAME FROM TABLE (MON_GET_MEMORY_SET ('DATABASE', NULL, -2))", conn))
                {
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        list.Add(rdr.GetString(0));
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



    public override void InitDb()
    {
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();

            var dt = conn.GetSchema("DataTypes");

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                _dataTypes[(int)dt.Rows[i]["PROVIDER_TYPE"]] = (dt.Rows[i]["PROVIDER_TYPE_NAME"].ToString(), dt.Rows[i]["CREATE_PARAMS"].ToString(), dt.Rows[i]["SQL_TYPE_NAME"].ToString());
            }

            // IBM's GetSchema column names differ between driver versions.
            // Use the same SYSCAT projections as the VS Code DB2 provider and
            // keep the legacy TABLE_SCHEMA/TABLE_NAME column contract.
            _schemas = LoadCatalogTable(conn, @"
                SELECT RTRIM(SCHEMANAME) AS TABLE_SCHEMA
                FROM SYSCAT.SCHEMATA
                ORDER BY SCHEMANAME
                WITH UR");
            tables = LoadCatalogTable(conn, @"
                SELECT CURRENT SERVER AS TABLE_CATALOG,
                       RTRIM(TABSCHEMA) AS TABLE_SCHEMA,
                       RTRIM(TABNAME) AS TABLE_NAME,
                       'TABLE' AS TABLE_TYPE,
                       COALESCE(REMARKS, '') AS REMARKS
                FROM SYSCAT.TABLES
                WHERE TYPE = 'T'
                ORDER BY TABSCHEMA, TABNAME
                WITH UR");
            views = LoadCatalogTable(conn, @"
                SELECT CURRENT SERVER AS TABLE_CATALOG,
                       RTRIM(TABSCHEMA) AS TABLE_SCHEMA,
                       RTRIM(TABNAME) AS TABLE_NAME,
                       'VIEW' AS TABLE_TYPE,
                       COALESCE(REMARKS, '') AS REMARKS
                FROM SYSCAT.TABLES
                WHERE TYPE = 'V'
                ORDER BY TABSCHEMA, TABNAME
                WITH UR");
            _synonyms = LoadCatalogTable(conn, @"
                SELECT CURRENT SERVER AS TABLE_CATALOG,
                       RTRIM(TABSCHEMA) AS TABLE_SCHEMA,
                       RTRIM(TABNAME) AS TABLE_NAME,
                       'SYNONYM' AS TABLE_TYPE,
                       COALESCE(REMARKS, '') AS REMARKS
                FROM SYSCAT.TABLES
                WHERE TYPE = 'S'
                ORDER BY TABSCHEMA, TABNAME
                WITH UR");
            _nicknames = LoadCatalogTable(conn, @"
                SELECT CURRENT SERVER AS TABLE_CATALOG,
                       RTRIM(TABSCHEMA) AS TABLE_SCHEMA,
                       RTRIM(TABNAME) AS TABLE_NAME,
                       'NICKNAME' AS TABLE_TYPE,
                       COALESCE(REMARKS, '') AS REMARKS
                FROM SYSCAT.TABLES
                WHERE TYPE = 'N'
                ORDER BY TABSCHEMA, TABNAME
                WITH UR");
            _aliases = LoadCatalogTable(conn, @"
                SELECT CURRENT SERVER AS TABLE_CATALOG,
                       RTRIM(TABSCHEMA) AS TABLE_SCHEMA,
                       RTRIM(TABNAME) AS TABLE_NAME,
                       'ALIAS' AS TABLE_TYPE,
                       COALESCE(REMARKS, '') AS REMARKS
                FROM SYSCAT.TABLES
                WHERE TYPE = 'A'
                ORDER BY TABSCHEMA, TABNAME
                WITH UR");
            procedures = LoadCatalogTable(conn, @"
                SELECT CURRENT SERVER AS PROCEDURE_CATALOG,
                       RTRIM(ROUTINESCHEMA) AS PROCEDURE_SCHEMA,
                       RTRIM(ROUTINENAME) AS PROCEDURE_NAME,
                       'PROCEDURE' AS PROCEDURE_TYPE,
                       COALESCE(REMARKS, '') AS REMARKS
                FROM SYSCAT.ROUTINES
                WHERE ROUTINETYPE = 'P'
                ORDER BY ROUTINESCHEMA, ROUTINENAME
                WITH UR");
            functions = LoadCatalogTable(conn, @"
                SELECT CURRENT SERVER AS PROCEDURE_CATALOG,
                       RTRIM(ROUTINESCHEMA) AS PROCEDURE_SCHEMA,
                       RTRIM(ROUTINENAME) AS PROCEDURE_NAME,
                       'FUNCTION' AS PROCEDURE_TYPE,
                       COALESCE(REMARKS, '') AS REMARKS
                FROM SYSCAT.ROUTINES
                WHERE ROUTINETYPE = 'F'
                ORDER BY ROUTINESCHEMA, ROUTINENAME
                WITH UR");

            // Keep the completion catalog in sync with the provider-neutral
            // DB2 snapshot consumed by the MVVM schema explorer.
            RebuildObjectInSchema();

            using (var cmd = new DB2Command("SELECT CURRENT SERVER FROM SYSIBM.SYSDUMMY1", conn))
            {
                DefaultDatabaseName = cmd.ExecuteScalar() as string;
                cmd.CommandText = @"
                    SELECT sum(nvl(t.fpages,0) * ts.PAGESIZE) FROM SYSCAT.TABLES t
                    join SYSCAT.TABLESPACES ts on t.TBSPACEID = ts.TBSPACEID
                    WHERE t.fpages > 0 and t.base_tabname is null
                    ";
                cmd.CommandTimeout = 10;
                try
                {
                    _bytesSize = (long)cmd.ExecuteScalar();
                }
                catch (Exception)
                {
                    _bytesSize = 0;
                    //mainWindow?.Invoke(() => MessageBox.Show(e.Message,"Error"));
                }

                cmd.CommandText = _linkedServersSql;
                var rdr = cmd.ExecuteReader();
                _linkedServersDt.Clear();
                _linkedServersDt.Load(rdr);

                cmd.CommandText = _linkedServersOptionsSql;
                rdr = cmd.ExecuteReader();
                _linkedServersOptionsDt.Clear();
                _linkedServersOptionsDt.Load(rdr);


                cmd.CommandText = _wrappers;
                rdr = cmd.ExecuteReader();
                _wrappersDt.Clear();
                _wrappersDt.Load(rdr);


                cmd.CommandText = wrapperOptions;
                rdr = cmd.ExecuteReader();
                _wrappersOptionsDt.Clear();
                _wrappersOptionsDt.Load(rdr);

                cmd.CommandText = _userMapings;
                rdr = cmd.ExecuteReader();
                _userMapingsDt.Clear();
                _userMapingsDt.Load(rdr);

                cmd.CommandText = _passthruSql;
                rdr = cmd.ExecuteReader();
                _passthruDt.Clear();
                _passthruDt.Load(rdr);
            }

            Db2CatalogObjects = BuildDb2CatalogObjects();
            conn.Close();
        }
    }



    protected override string GetColumn(string db, string schema, string parentObject)
    {
        return @$"ALTER TABLE {StringExtension.QuoteNameIfNeeded(schema)}.{StringExtension.QuoteNameIfNeeded(parentObject)} ADD COLUMN <COLUMN_NAME> INT NOT NULL DEFAULT 0;
--https://www.ibm.com/docs/en/db2/11.5?topic=properties-adding-dropping-columns";
    }
    protected override string GetConstraint(string db, string schema, string parentObject)
    {
        return @$"ALTER TABLE {StringExtension.QuoteNameIfNeeded(schema)}.{StringExtension.QuoteNameIfNeeded(parentObject)} ADD CONSTRAINT <NAME> CHECK (<EXPRESSION>);
--https://www.ibm.com/docs/en/db2/11.1?topic=constraints-creating-modifying";
    }

    protected override string GetIndex(string db, string schema, string parentObject)
    {
        return @$"CREATE UNIQUE INDEX UNIQUE_{parentObject} ON {StringExtension.QuoteNameIfNeeded(schema)}.{StringExtension.QuoteNameIfNeeded(parentObject)} (<COLUMNS>);
--https://www.ibm.com/docs/en/db2/11.5?topic=statements-create-index";
    }

    protected override string GetPartition(string db, string schema, string parentObject)
    {
        return @$"--https://www.ibm.com/docs/en/db2/11.5?topic=tables-creating-partitioned";
    }
    protected override string GetTrigger(string db, string schema, string parentObject)
    {
        return @$"--https://www.ibm.com/docs/en/db2/11.5?topic=objects-triggers";
    }

    protected override void AddToCache(string dbname, string schema, string tablename)
    {
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new DB2Command("SELECT 1 FROM SYSIBM.SYSDUMMY1", conn))
            {
                cmd.CommandText =
                    @$"
                    SELECT
                        COLNAME
                        , CASE WHEN UPPER(TYPENAME) IN('BINARY','VARBINARY','BLOB', 'CLOB','DBCLOB','CHARACTER', 'VARCHAR','GRAPHIC','VARGRAPHIC') then TYPENAME || '(' || LENGTH ||')'
                               WHEN UPPER(TYPENAME) IN('DECIMAL') then TYPENAME || '(' || LENGTH || ',' || SCALE || ')'
                                 ELSE TYPENAME
                                 END 
                            || CASE WHEN NULLS = 'Y' THEN '' ELSE ' NOT NULL' END
                            || CASE WHEN DEFAULT IS NOT NULL THEN ' DEFAULT ' || DEFAULT ELSE '' END
                        AS TYPENAME
                        , KEYSEQ AS ISPK
                        , REMARKS
                     FROM syscat.columns
                    WHERE TABSCHEMA = '{schema}'
                    AND TABNAME = '{tablename}'
                    AND RANDDISTKEY = 'N' -- HIDDEN, GENERATED
                    ORDER BY COLNO
                    ";

                var rdr = cmd.ExecuteReader();

                List<string> ls = new List<string>();
                List<string> ls2 = new List<string>();
                List<short> ls3 = new List<short>();
                List<string> ls4 = new List<string>();
                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0));
                    ls2.Add(rdr.GetString(1));
                    var o = rdr.GetValue(2);
                    if (o == DBNull.Value)
                    {
                        o = (short)-1;
                    }
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

    protected override void AddToindexCache(string dbName, string schema, string tablename)
    {

        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new DB2Command("SELECT 1 FROM SYSIBM.SYSDUMMY1", conn))
            {
                cmd.CommandText =
                    @$"
SELECT DISTINCT INDNAME /*INDSCHEMA , INDNAME, COLNAMES,
                     uniquerule,
                    case indextype 
                        when 'BLOK' then 'Block index'
                        when 'CLUS' then 'Clustering index'
                        when 'DIM' then 'Dimension block index'
                        when 'REG' then 'Regular index'
                        when 'XPTH' then 'XML path index'
                        when 'XRGN' then 'XML region index'
                        when 'XVIL' then 'Index over XML column (logical)'
                        when 'XVIP' then 'Index over XML column (physical)'
                    end as index_type
                    , COMPRESSION*/
                    FROM syscat.indexes  WHERE indschema not like 'SYS%' AND TABSCHEMA = '{schema}' AND TABNAME = '{tablename}' WITH UR;";


                var rdr = cmd.ExecuteReader();

                List<string> ls = new List<string>();
                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0));
                }
                indexesOfTable[dbName + "_" + schema + "\\" + tablename] = (ls.ToArray(), "info");
            }
            conn.Close();
        }
    }

    protected override void AddToPartitionCache(string dbName, string schema, string tablename)
    {
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new DB2Command("SELECT 1 FROM SYSIBM.SYSDUMMY1", conn))
            {
                cmd.CommandText =
                    @$"SELECT P.DATAPARTITIONNAME,P.LOWVALUE,P.HIGHVALUE,P.LOWINCLUSIVE,P.HIGHINCLUSIVE, S.TBSPACE
, P.TABSCHEMA,  P.TABNAME
FROM SYSCAT.DATAPARTITIONS P 
LEFT JOIN syscat.tablespaces S ON S.TBSPACEID = P.TBSPACEID
JOIN SYSCAT.DATAPARTITIONEXPRESSION DP1 ON 
    DP1.TABSCHEMA = '{schema}' AND DP1.TABNAME = '{tablename}' 
WHERE P.TABSCHEMA = '{schema}' AND P.TABNAME = '{tablename}' ORDER BY SEQNO 
WITH UR; ";


                var rdr = cmd.ExecuteReader();

                List<string> ls = new List<string>();
                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0) + " LOWVALUE:" + rdr.GetValue(1).ToString() + "HIGHVALUE:" + rdr.GetValue(2).ToString());
                }
                partitionsOfTable[dbName + "_" + schema + "\\" + tablename] = (ls.ToArray(), "info");
            }
            conn.Close();
        }
    }

    protected override void AddToTriggersCache(string dbName, string schema, string tablename)
    {
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new DB2Command(GetTriggersSql(schema, tablename), conn))
            {
                var rdr = cmd.ExecuteReader();

                List<string> ls = new List<string>();
                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0) + " - " + rdr.GetString(3) + " " + rdr.GetString(4));
                }
                triggersOfTable[dbName + "_" + schema + "\\" + tablename] = (ls.ToArray(), "info");
            }
            conn.Close();
        }
    }

    protected override void AddToConstraintsCache(string dbName, string schema, string tablename)
    {
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            List<string> ls = new List<string>();
            using (var cmd = new DB2Command(GetChecksSql(schema, tablename), conn))
            {
                var rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0) + " - " + rdr.GetString(1));
                }
            }
            using (var cmd = new DB2Command(GetConstraints2Sql(schema, tablename), conn))
            {
                var rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    ls.Add(rdr.GetString(0) + " type:" + rdr.GetString(1) + " enforced:" + rdr.GetString(2));
                }
            }

            // to do another constraints
            constraintsOfTable[dbName + "_" + schema + "\\" + tablename] = (ls.ToArray(), "info");
            conn.Close();
        }
    }

    private void Conn_InfoMessage(object sender, DB2InfoMessageEventArgs e)
    {
        List<string> arr = new List<string>();
        for (int i = 0; i < e.Errors.Count; i++)
        {
            arr.Add(e.Errors[i].Message);
        }
        RaiseNotice(String.Join(Environment.NewLine, arr));
    }

    public override void ResetDynamicCollection()
    {
        ResetDynamicCollectionH();

        foreach (DataRow item in _schemas.Rows)
        {
            AutocompleteSuggestions.OneWord.Add(item.ItemArray[0] as string);
        }

        foreach (DataRow item in _schemas.Rows)
        {
            string schema = (item.ItemArray[0] as string);
            DataRow[] tableCol;
            tableCol = tables?.Select($"TABLE_SCHEMA = '{schema}'");

            foreach (DataRow item2 in tableCol)
            {
                string schemaName = item2.ItemArray[2] as string;
                //QuoteNameIfNeeded(ref schemaName);


                AutocompleteSuggestions.TwoWords.Add($"{schema}.{schemaName}");
            }

            var synonymCol = _synonyms.Select($"TABLE_SCHEMA = '{schema}'");
            foreach (DataRow item2 in synonymCol)
            {
                AutocompleteSuggestions.TwoWords.Add($"{schema}.{item2.ItemArray[2]}");
            }

            DataRow[] viewCol;
            viewCol = views?.Select($"TABLE_SCHEMA = '{schema}'");

            foreach (DataRow item2 in viewCol)
            {
                AutocompleteSuggestions.TwoWords.Add($"{schema}.{item2.ItemArray[2]}");
            }
        }
    }

    //        SELECT* FROM SYSCAT.KEYCOLUSE

    //SELECT r.tabname
    //     , r.tabschema
    //     , r.constname
    //     , r.reftabschema
    //     , r.reftabname
    //     , r.fk_colnames
    //     , r.pk_colnames
    //     , r.deleterule //R, C, N 
    //     , r.updaterule
    //     , kcu.colname
    //     , kcu.colseq
    //  FROM syscat.references r
    //      , syscat.keycoluse kcu
    // WHERE r.tabschema = 'TEST'
    //   AND kcu.constname = r.constname
    //   AND kcu.tabschema = r.tabschema
    //   AND kcu.tabname   = r.tabname
    // ORDER BY r.tabschema
    //        , r.constname
    //        , kcu.colseq
    //WITH UR

    //SELECT c.tabname
    //     , c.type
    //     , c.constname
    //     , kcu.colname
    //     , kcu.colseq
    //  FROM syscat.tabconst c
    //       , syscat.keycoluse kcu
    // WHERE c.tabschema = 'TEST'
    //   AND c.type = 'P'
    //   AND kcu.constname = c.constname
    //   AND kcu.tabschema = c.tabschema
    //   AND kcu.tabname   = c.tabname
    // ORDER BY c.constname
    //        , kcu.colseq
    //WITH UR


    //SELECT i.tabname
    //     , i.indname
    //     , i.indschema
    //     , i.indextype
    //     , i.uniquerule
    //     , ico.colname
    //     , ico.colseq
    //     , ico.colorder
    //     , ico.virtual
    //     , ico.text
    //  FROM syscat.indexes i
    //     , syscat.indexcoluse ico
    // WHERE i.indschema = 'TEST'
    //   AND i.tabschema = i.indschema 
    //   AND ico.indschema = i.indschema 
    //   AND ico.indname = i.indname
    //   AND UPPER(ico.virtual) != 'Y'
    // ORDER BY i.indname
    //        , ico.colseq
    // WITH UR
    //SELECT c.constname
    //     , c.tabname
    //     , c.type
    //     , c.text
    //     , ck.colname
    //     , ck.usage
    //  FROM syscat.checks c
    //     , syscat.colchecks  ck
    // WHERE c.tabschema = 'TEST'
    //   AND ck.constname = c.constname
    //   AND ck.tabschema = c.tabschema
    //   AND ck.tabname   = c.tabname
    //WITH UR

    //        SELECT p.tabname,
    //    p.datapartitionkeyseq,
    //    p.datapartitionexpression,
    //    p.nullsfirst
    //FROM syscat.datapartitionexpression AS p
    //WHERE
    //    p.tabschema = 'TEST'
    //ORDER BY
    //    p.tabname,
    //    p.datapartitionkeyseq
    //WITH UR
    //SELECT p.tabname,
    //      p.datapartitionname,
    //      tbs.tbspace,
    //      tbsl.tbspace AS tbspace_long,
    //      tbsi.tbspace AS tbspace_index,
    //      p.seqno,
    //      p.lowinclusive,
    //      p.lowvalue,
    //      p.highinclusive,
    //      p.highvalue
    //FROM syscat.datapartitions AS p
    //LEFT JOIN syscat.tablespaces tbs ON tbs.tbspaceid = p.tbspaceid
    //LEFT JOIN syscat.tablespaces tbsl ON tbsl.tbspaceid = p.long_tbspaceid
    //LEFT JOIN syscat.tablespaces tbsi ON tbsi.tbspaceid = p.index_tbspaceid
    //WHERE
    //    p.tabschema = 'TEST'
    //    AND TRIM(lowvalue) != ''
    //    AND TRIM(highvalue) != ''
    //ORDER BY
    //    p.tabname,
    //    p.seqno
    //WITH UR


    //CREATE TABLE TEST.FOO
    //(
    //    A INTEGER
    //)
    //ORGANIZE BY ROW
    //COMPRESS NO
    //PARTITION BY RANGE(A NULLS LAST)
    //(PARTITION PART0    STARTING FROM 1 INCLUSIVE ENDING AT 21 EXCLUSIVE IN USERSPACE1,
    // PARTITION PART1    STARTING FROM 21 INCLUSIVE ENDING AT 41 EXCLUSIVE IN USERSPACE1,
    // PARTITION PART2    STARTING FROM 41 INCLUSIVE ENDING AT 61 EXCLUSIVE IN USERSPACE1,
    // PARTITION PART3    STARTING FROM 61 INCLUSIVE ENDING AT 81 EXCLUSIVE IN USERSPACE1,
    // PARTITION PART4    STARTING FROM 81 INCLUSIVE ENDING AT 100 INCLUSIVE IN USERSPACE1
    //);

    // SELECT* FROM SYSCAT.DATAPARTITIONEXPRESSION WHERE TABNAME = 'FOO';
    // SELECT* FROM SYSCAT.DATAPARTITIONS WHERE TABNAME = 'FOO';
    // SELECT* FROM SYSCAT.INDEXPARTITIONS;


    private static string GetTriggersSql(string schema, string tablename)
    {
        return $@"select 
                                    trigname as trigger_name,
                                    tabschema , 
                                    tabname , 
                                    case trigtime 
                                         when 'B' then 'before'
                                         when 'A' then 'after'
                                         when 'I' then 'instead of' 
                                    end as activation,
                                    rtrim(case when eventupdate ='Y' then  'update ' else '' end 
                                          concat 
                                          case when eventdelete ='Y' then  'delete ' else '' end
                                          concat
                                          case when eventinsert ='Y' then  'insert ' else '' end)
                                    as event,   
                                    case when ENABLED = 'N' then 'disabled'
                                    else 'active' end as status,
                                    text as definition
                                from syscat.triggers t
                                where tabschema = '{schema}'
                                      AND  TABNAME = '{tablename}'
                                order by trigname";
    }

    private static string GetChecksSql(string schema, string tablename)
    {
        return $@"select con.CONSTNAME, con.text, tcu.ENFORCED, tcu.TRUSTED
                        from 
                            syscat.checks con
                            join syscat.tabconst tcu on tcu.CONSTNAME = con.CONSTNAME and tcu.TABSCHEMA= '{schema}' and con.tabname = '{tablename}'
                        where
                            con.TABSCHEMA = '{schema}'
                            and con.tabname = '{tablename}'
                    ";
    }

    private static string GetConstraints1Sql(string schema, string tablename)
    {
        return $@"SELECT c.CONSTNAME, kcu.colname, c.remarks, c.ENFORCED, c.TRUSTED
                              FROM syscat.tabconst c
                                   , syscat.keycoluse kcu
                               WHERE c.tabschema = '{schema}'
                                     and c.TABNAME = '{tablename}'
                                    AND c.type = 'P'
                               AND kcu.constname = c.constname
                               AND kcu.tabschema = c.tabschema
                               AND kcu.tabname   = c.tabname
                             ORDER BY c.constname
                                    , kcu.colseq
                            WITH UR
                            ;";
    }

    private static string GetConstraints2Sql(string schema, string tablename)
    {
        return @$"SELECT CONSTNAME,TYPE,ENFORCED,REMARKS FROM SYSCAT.TABCONST 
WHERE TABSCHEMA = '{schema}'  AND TABNAME = '{tablename}' 
ORDER BY  CONSTNAME,TYPE,ENFORCED,REMARKS
WITH UR";
    }


    public override string GetCreateTableText(string dbName, string schema, string tableName)
    {
        string SQL = $"SELECT TABLEORG ,TBSPACE, COMPRESSION, keycolumns, keyindexid, keyunique, checkcount, PARTITION_MODE, REMARKS,PROPERTY FROM SYSCAT.TABLES WHERE TABNAME = '{tableName}' AND TABSCHEMA = '{schema}';";
        //0         1       2           3           4           5           6           7               8
        string organize;
        string tbSpace = null;
        string compression;
        short keycolumns;
        string remarks = null;
        string pk = "????";
        string pkComment = null;
        List<string> pkCols = new List<string>();
        string pkEnforced = "";
        string pkTrusted = "";
        string distributeInfo = "-- DISTIBUTE BY ... ";
        string partitionInfo = "";
        string PARTITION_MODE = "";//7
        string PROPERTY = ""; //9

        Dictionary<string, List<string>> fkCols = new Dictionary<string, List<string>>();
        Dictionary<string, string> fkComments = new Dictionary<string, string>();
        Dictionary<string, string> fkEnforced = new Dictionary<string, string>();
        Dictionary<string, string> fkTrusted = new Dictionary<string, string>();

        Dictionary<string, string> constraints = new Dictionary<string, string>();
        Dictionary<string, string> checks = new Dictionary<string, string>();
        Dictionary<string, string> checksEnforced = new Dictionary<string, string>();
        Dictionary<string, string> checksTrusted = new Dictionary<string, string>();

        List<string> indexes = new List<string>();
        List<string> triggers = new List<string>();

        string clearDbName = StringExtension.QuoteNameIfNeeded(dbName);
        string clearSchema = StringExtension.QuoteNameIfNeeded(schema);
        string clearTableName = StringExtension.QuoteNameIfNeeded(tableName);

        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = SQL;
                var rdr = cmd.ExecuteReader();
                rdr.Read();
                organize = rdr.GetString(0) == "R" ? "ROW" : "COLUMN";
                var tmp = rdr.GetValue(1);
                if (tmp != DBNull.Value && tmp is string str)
                {
                    tbSpace = str;
                }
                compression = rdr.GetString(2) == "N" ? "NO" : "YES";
                keycolumns = rdr.GetInt16(3);
                if (rdr.GetValue(8) != DBNull.Value)
                {
                    remarks = rdr.GetString(8);
                }
                PARTITION_MODE = rdr.GetString(7).Trim();
                PROPERTY = rdr.GetString(9).Trim();
                if (string.IsNullOrWhiteSpace(PARTITION_MODE))
                {
                    distributeInfo = "";
                }
                else if (PARTITION_MODE == "H" && PROPERTY == "Y")
                {
                    distributeInfo = $"DISTRIBUTE BY RANDOM";
                }
                else if (PARTITION_MODE == "H")
                {
                    List<string> listOfHash = new();

                    string distSql = $@"SELECT colname from syscat.columns 
                            where TABSCHEMA = '{schema}' and TABNAME = '{tableName}' and partkeyseq !=0
                            order by partkeyseq with ur";

                    using (var cmd2 = new DB2Command(distSql, conn))
                    {
                        using var rdrDist = cmd2.ExecuteReader();
                        while (rdrDist.Read())
                        {
                            listOfHash.Add(rdrDist.GetString(0));
                        }
                    }

                    distributeInfo = $"DISTRIBUTE BY HASH({String.Join(',', listOfHash)})";
                }
                rdr.Close();
            }

            if (keycolumns >= 1)
            {
                SQL = GetConstraints1Sql(schema, tableName);

                using (var cmd = new DB2Command(SQL, conn))
                {
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        pk = rdr.GetString(0);
                        pkComment = rdr.GetValue(2) == DBNull.Value ? null : rdr.GetString(2);
                        pkCols.Add(rdr.GetString(1));

                        pkEnforced = rdr.GetString(3);
                        pkTrusted = rdr.GetString(4);

                        pkEnforced = pkEnforced switch
                        {
                            "Y" => " ENFORCED",
                            "N" => " NOT ENFORCED",
                            _ => "",
                        };
                        pkTrusted = pkTrusted switch
                        {
                            "Y" => " TRUSTED",
                            "N" => " NOT TRUSTED",
                            _ => "",
                        };
                    }

                    rdr.Close();
                }
            }

            SQL = $@"SELECT c.constname
                             , kcu.colname
                             , c.remarks
                             , c.ENFORCED
                             , c.TRUSTED
                          FROM syscat.tabconst c
                               , syscat.keycoluse kcu
                         WHERE c.tabschema = '{schema}'
                               and c.TABNAME = '{tableName}'
                               AND c.type = 'F'
                           AND kcu.constname = c.constname
                           AND kcu.tabschema = c.tabschema
                           AND kcu.tabname   = c.tabname
                         ORDER BY c.constname
                                , kcu.colseq
                        WITH UR";
            using (var cmd = new DB2Command(SQL, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string constname = rdr.GetString(0);
                    string colname = rdr.GetString(1);
                    string remark = rdr.GetValue(2) == DBNull.Value ? null : rdr.GetString(2);

                    if (!fkCols.ContainsKey(constname))
                    {
                        fkCols[constname] = new List<string>();
                    }
                    fkCols[constname].Add(colname);
                    fkComments[constname] = remark;

                    string fkEnforcedVal = rdr.GetString(3);
                    string fkTrustedVal = rdr.GetString(4);

                    fkEnforced[constname] = fkEnforcedVal switch
                    {
                        "Y" => " ENFORCED",
                        "N" => " NOT ENFORCED",
                        _ => "",
                    };
                    fkTrusted[constname] = fkTrustedVal switch
                    {
                        "Y" => " TRUSTED",
                        "N" => " NOT TRUSTED",
                        _ => "",
                    };
                }
                rdr.Close();
            }


            SQL = $@"SELECT
                        r.CONSTNAME,REFTABSCHEMA,REFTABNAME, PK_COLNAMES, DELETERULE, UPDATERULE
                    FROM
                        syscat.references r
                    WHERE
                        R.TABSCHEMA = '{schema}'
                         AND r.tabname = '{tableName}'";

            using (var cmd = new DB2Command(SQL, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string constname = rdr.GetString(0);
                    string REFTABSCHEMA = rdr.GetString(1).Trim();
                    //QuoteNameIfNeeded(ref REFTABSCHEMA);

                    string REFTABNAME = rdr.GetString(2);
                    string PK_COLNAMES = rdr.GetString(3);
                    string DELETERULEpom = rdr.GetString(4);
                    string UPDATERULE = rdr.GetString(5);
                    string deleterule = DELETERULEpom switch
                    {
                        "R" => "ON DELETE RESTRICT",
                        "C" => "ON DELETE CASCADE",
                        "N" => "ON DELETE SET NULL",
                        _ => $"ON DELETE {UPDATERULE} ???",
                    };
                    constraints[constname] = $"REFERENCES {StringExtension.QuoteNameIfNeeded(REFTABSCHEMA)}.{StringExtension.QuoteNameIfNeeded(REFTABNAME)}({Regex.Replace(PK_COLNAMES.Trim(), @"\s{3,}", ",")}) {deleterule}";
                }
                rdr.Close();
            }

            SQL = GetChecksSql(schema, tableName);

            using (var cmd = new DB2Command(SQL, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string constname = rdr.GetString(0);
                    checks[constname] = rdr.GetString(1);

                    string fkEnforcedVal = rdr.GetString(2);
                    string fkTrustedVal = rdr.GetString(3);

                    fkEnforcedVal = fkEnforcedVal switch
                    {
                        "Y" => " ENFORCED",
                        "N" => " NOT ENFORCED",
                        _ => "",
                    };
                    fkTrustedVal = fkTrustedVal switch
                    {
                        "Y" => " TRUSTED",
                        "N" => " NOT TRUSTED",
                        _ => "",
                    };
                    checksEnforced[constname] = fkEnforcedVal;
                    checksTrusted[constname] = fkTrustedVal;
                }
            }
            //  ase uniquerule
            //    when 'P' then 'Primary key'
            //    when 'U' then 'Unique'
            //    when 'D' then 'Nonunique'
            //end as type,

            SQL = $@"SELECT INDSCHEMA, INDNAME, COLNAMES,
                     uniquerule,
                    case indextype 
                        when 'BLOK' then 'Block index'
                        when 'CLUS' then 'Clustering index'
                        when 'DIM' then 'Dimension block index'
                        when 'REG' then 'Regular index'
                        when 'XPTH' then 'XML path index'
                        when 'XRGN' then 'XML region index'
                        when 'XVIL' then 'Index over XML column (logical)'
                        when 'XVIP' then 'Index over XML column (physical)'
                    end as index_type
                    , COMPRESSION
                    FROM syscat.indexes  WHERE indschema not like 'SYS%' AND TABSCHEMA = '{schema}' AND TABNAME = '{tableName}' WITH UR;";
            using (var cmd = new DB2Command(SQL, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string indshema = rdr.GetString(0).Trim();
                    string indName = rdr.GetString(1);
                    string compressIndexText = "";

                    //QuoteNameIfNeeded(ref indshema);


                    var indexColsList = rdr.GetString(2).Split('+');
                    for (int i = 0; i < indexColsList.Length; i++)
                    {
                        indexColsList[i] = StringExtension.QuoteNameIfNeeded(indexColsList[i]);
                    }
                    ArraySegment<string> indexArraySegment;
                    if (string.IsNullOrWhiteSpace(indexColsList[0]))
                    {
                        indexArraySegment = new ArraySegment<string>(indexColsList, 1, indexColsList.Length - 1);
                    }
                    else
                    {
                        indexArraySegment = new ArraySegment<string>(indexColsList, 0, indexColsList.Length);
                    }

                    string uniquerule = rdr.GetString(3);
                    if (uniquerule == "U")
                    {
                        uniquerule = " UNIQUE";
                    }
                    else
                    {
                        uniquerule = "";
                    }
                    var tmp = rdr.GetValue(5);
                    if (tmp != DBNull.Value && tmp is string str && str == "Y")
                    {
                        compressIndexText = " COMPRESS YES";
                    }

                    indexes.Add($"CREATE{uniquerule} INDEX {StringExtension.QuoteNameIfNeeded(indshema)}.{StringExtension.QuoteNameIfNeeded(indName)} ON {clearSchema}.{clearTableName}({String.Join(',', (IEnumerable<string>)indexArraySegment)}){compressIndexText};");
                }
            }

            SQL = GetTriggersSql(schema, tableName);

            using (var cmd = new DB2Command(SQL, conn))
            {
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    triggers.Add(rdr.GetString(6));
                }
            }


            SQL = "SELECT DATAPARTITIONEXPRESSION, NULLSFIRST FROM SYSCAT.DATAPARTITIONEXPRESSION " +
                $"WHERE TABSCHEMA = '{schema}' AND TABNAME = '{tableName}' order by DATAPARTITIONKEYSEQ;";
            List<string> ls = new List<string>();
            using (var cmd = new DB2Command(SQL, conn))
            {
                var rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    var temp = rdr.GetValue(1);
                    string nullInfo = "";
                    if (temp != DBNull.Value && temp is string str && str == "N")
                    {
                        nullInfo = " NULLS LAST";
                    }
                    ls.Add(rdr.GetString(0) + nullInfo);
                }
            }
            if (ls.Count > 0)
            {
                partitionInfo = $"PARTITION BY RANGE({String.Join(',', ls)}){Environment.NewLine}";

                SQL = $@"SELECT P.DATAPARTITIONNAME,P.LOWVALUE,P.HIGHVALUE,P.LOWINCLUSIVE,P.HIGHINCLUSIVE, S.TBSPACE
                        FROM SYSCAT.DATAPARTITIONS P 
                        LEFT JOIN syscat.tablespaces S ON S.TBSPACEID = P.TBSPACEID
                        WHERE P.TABSCHEMA = '{schema}' AND P.TABNAME = '{tableName}' ORDER BY SEQNO 
                        WITH UR;";

                bool multiKey = false;
                if (ls.Count > 1)
                {
                    multiKey = true;
                }

                ls.Clear();
                using (var cmd = new DB2Command(SQL, conn))
                {
                    var rdr = cmd.ExecuteReader();
                    //int i = 0;
                    while (rdr.Read())
                    {
                        string DATAPARTITIONNAME = rdr.GetString(0);
                        string LOWVALUE = multiKey ? $"({rdr.GetValue(1)})" : $"{rdr.GetValue(1)}";
                        string HIGHVALUE = multiKey ? $"({rdr.GetValue(2)})" : $"{rdr.GetValue(2)}";
                        string LOWINCLUSIVE = rdr.GetString(3) == "Y" ? "INCLUSIVE" : "EXCLUSIVE";
                        string HIGHINCLUSIVE = rdr.GetString(4) == "Y" ? "INCLUSIVE" : "EXCLUSIVE";
                        string TBSPACE = rdr.GetString(5);
                        //i++;
                        ls.Add($"PARTITION {DATAPARTITIONNAME} STARTING FROM {LOWVALUE} {LOWINCLUSIVE} ENDING AT {HIGHVALUE} {HIGHINCLUSIVE} IN {TBSPACE}");
                    }
                    partitionInfo += $"({String.Join($",{Environment.NewLine}", ls)})";
                }
            }
            conn.Close();
        }


        StringBuilder sb = new StringBuilder();
        var columnsOfTable = GetColumnsEx("", schema, tableName);

        string[] columnsWithTypes = new string[columnsOfTable.Item1.Length];
        for (int i = 0; i < columnsWithTypes.Length; i++)
        {
            columnsWithTypes[i] = StringExtension.QuoteNameIfNeeded(columnsOfTable.Item1[i]) + " " + columnsOfTable.Item2[i];
        }

        sb.AppendLine($"CREATE TABLE {clearSchema}.{clearTableName}");
        sb.AppendLine("(");
        sb.Append("    ");
        sb.AppendLine(String.Join($",{Environment.NewLine}    ", columnsWithTypes));
        sb.AppendLine(")");
        if (tbSpace != null)
        {
            sb.AppendLine($"ORGANIZE BY {organize} IN {tbSpace}"); // SELECT XXX = TABLEORG , YYY = TBSPACE, ZZZ = COMPRESSION FROM SYSCAT.TABLES T WHERE T.TABNAME = 'EMPLOYEE'
        }
        else
        {
            sb.AppendLine($"ORGANIZE BY {organize}");
        }
        if (!string.IsNullOrWhiteSpace(distributeInfo))
        {
            sb.AppendLine(distributeInfo);
        }
        sb.AppendLine($"COMPRESS {compression}{Environment.NewLine}{partitionInfo};");

        //PK
        if (keycolumns >= 1)
        {
            string clearPk = StringExtension.QuoteNameIfNeeded(pk);
            sb.AppendLine($"ALTER TABLE {clearSchema}.{clearTableName} ADD CONSTRAINT {clearPk} PRIMARY KEY({String.Join(",", pkCols)}){pkEnforced}{pkTrusted};");
            if (pkComment is not null)
            {
                sb.AppendLine($"COMMENT ON CONSTRAINT {clearSchema}.{clearTableName}.{clearPk}  IS '{pkComment.Replace("'", "''")}';");
            }
        }
        if (remarks != null)
        {
            sb.AppendLine($"COMMENT ON TABLE {clearSchema}.{clearTableName} IS '{remarks.Replace("'", "'")}';");
        }
        // COMMENT ON CONSTRAINT TEST.EMPLOYEE.RED IS 'DDDD';


        foreach (var item in fkCols)
        {
            sb.AppendLine($"ALTER TABLE {clearSchema}.{clearTableName} ADD CONSTRAINT {item.Key} FOREIGN KEY({String.Join(",", item.Value)}) {constraints[item.Key]}{fkEnforced[item.Key]}{fkTrusted[item.Key]};");

            if (fkComments[item.Key] != null)
            {
                sb.AppendLine($"COMMENT ON CONSTRAINT {clearSchema}.{clearTableName}.{item.Key}  IS '{fkComments[item.Key].Replace("'", "''")}';");
            }
        }

        foreach (var item in checks.Keys)
        {
            sb.AppendLine($"ALTER TABLE {clearSchema}.{clearTableName} ADD CONSTRAINT {item} CHECK({checks[item]}){checksEnforced[item]}{checksTrusted[item]};");
        }

        for (int i = 0; i < columnsOfTable.Item1.Length; i++)
        {
            if (columnsOfTable.Item4[i] != null)
            {
                sb.AppendLine($"COMMENT ON COLUMN {clearSchema}.{clearTableName}.{columnsOfTable.Item1[i]} IS '{columnsOfTable.Item4[i].Replace("'", "'")}';");
            }
        }

        foreach (var item in indexes)
        {
            sb.AppendLine(item);
        }

        if (triggers.Count > 0)
        {
            sb.AppendLine("--REGION TRIGGERS");
        }
        foreach (var item in triggers)
        {
            sb.AppendLine(item);
        }
        if (triggers.Count > 0)
        {
            sb.AppendLine("--ENDREGION");
        }

        //COMMENT ON COLUMN TEST.EMPLOYEE.EDLEVEL     IS 'highest grade level passed in school'
        // NOT NULL + 
        //Unique
        //Primary key + 
        //Foreign Key
        //Check
        //Informational
        return sb.ToString();
    }

    public override string GetCreateViewText(string dbName, string schema, string viewName)
    {
        string SQL = $"SELECT TEXT FROM SYSCAT.VIEWS WHERE VIEWSCHEMA = '{schema}' AND VIEWNAME = '{viewName}';";
        string viewText = "";
        string schemaBefore = "";
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = SQL;
                viewText = cmd.ExecuteScalar() as string;
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = schemaSql;
                schemaBefore = cmd.ExecuteScalar() as string;
            }
            conn.Close();
        }

        return $@"SET SCHEMA {StringExtension.QuoteNameIfNeeded(schema)};
{viewText};
SET SCHEMA {StringExtension.QuoteNameIfNeeded(schemaBefore)};
";
    }

    private readonly string schemaSql = "SELECT CURRENT_SCHEMA FROM SYSIBM.SYSDUMMY1";
    public override string GetCreatePorcedureText(string schema, string procName)
    {
        string procedureBodySql = $"SELECT TEXT FROM SYSCAT.PROCEDURES P WHERE P.PROCSCHEMA = '{schema}' AND P.PROCNAME = '{procName}';";
        string procTxt = "";
        string schemaBefore = "";
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            using (var cmd = new DB2Command(procedureBodySql, conn))
            {
                procTxt = cmd.ExecuteScalar() as string;
            }
            using (var cmd = new DB2Command(schemaSql, conn))
            {
                schemaBefore = cmd.ExecuteScalar() as string;
            }
            conn.Close();
        }

        return $@"SET SCHEMA {StringExtension.QuoteNameIfNeeded(schema)};
{procTxt};
SET SCHEMA {StringExtension.QuoteNameIfNeeded(schemaBefore)};
";
    }

    private readonly Dictionary<string, (string refObject, string remarks)> aliasesDic = new Dictionary<string, (string, string)>();

    async Task AddToaliasesDicAsync(string schema, string aliasName)
    {
        await Task.Run(() =>
        {
            string SQL = @$"select  tabschema
                                    , tabname
                                    , base_tabschema
                                    , base_tabname
                                    , REMARKS
                                from syscat.tables 
                                WHERE type = 'A'
                                with ur";
            using (var conn = new DB2Connection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new DB2Command(SQL, conn))
                {
                    var rdr = cmd.ExecuteReader();
                    aliasesDic.Clear();
                    while (rdr.Read())
                    {
                        var remarks = rdr.GetValue(4);
                        string refSchema = rdr.GetString(2).Trim();
                        string refName = rdr.GetString(3).Trim();
                        string goodAliasName = refName;
                        //QuoteNameIfNeeded(ref refSchema);
                        //QuoteNameIfNeeded(ref goodAliasName);

                        aliasesDic[$"{rdr.GetString(0).Trim()}.{rdr.GetString(1)}"] = ($"{refSchema}.{goodAliasName}", remarks == DBNull.Value ? null : $"{remarks}");
                    }
                }
                conn.Close();
            }
        });
    }

    public override async Task<(string, string)> GetAliasDataAsync(string schema, string aliasName)
    {
        if (!aliasesDic.ContainsKey($"{schema}.{aliasName}"))
        {
            await AddToaliasesDicAsync(schema, aliasName);
        }
        return aliasesDic[$"{schema}.{aliasName}"];
    }

    public override async Task<string> GetCreateAliasTextAsync(string schemaTablename)
    {
        int id = schemaTablename.LastIndexOf(".");
        string schema = schemaTablename.Substring(0, id);
        string tablename = schemaTablename.Substring(id + 1);

        string cleanSchemaName = StringExtension.QuoteNameIfNeeded(schema);
        string cleanTableName = StringExtension.QuoteNameIfNeeded(tablename);
        //QuoteNameIfNeeded(ref validSchemaName);

        if (!aliasesDic.ContainsKey(schemaTablename))
        {
            await AddToaliasesDicAsync(schema, tablename);
        }

        if (!aliasesDic.ContainsKey(schemaTablename))
        {
            return "problem";
        }
        else
        {
            return $"CREATE ALIAS {cleanSchemaName}.{cleanTableName} FOR {aliasesDic[schemaTablename].refObject};";
        }
    }

    private readonly Dictionary<string, (string refObject, string remarks)> synonymsDic = new Dictionary<string, (string, string)>();

    private async Task AddToSynonymsDicAsync(string schema, string aliasName) //nicknames
    {
        await Task.Run(() =>
        {
            string SQL = @$"SELECT A.TABSCHEMA, A.TABNAME, NVL(A.REMOTE_SCHEMA,'ADMIN'), A.REMOTE_TABLE, A.SERVERNAME, A.REMARKS FROM SYSCAT.NICKNAMES A;";
            try
            {
                using (var conn = new DB2Connection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new DB2Command(SQL, conn))
                    {
                        var rdr = cmd.ExecuteReader();
                        aliasesDic.Clear();
                        while (rdr.Read())
                        {
                            var remarks = rdr.GetValue(5);
                            string refSchema = rdr.GetString(2).Trim();
                            string refName = rdr.GetString(3).Trim();
                            string refServer = rdr.GetString(4).Trim();
                            string goodNicknameName = refName;
                            //QuoteNameIfNeeded(ref refSchema);
                            //QuoteNameIfNeeded(ref refName);

                            synonymsDic[$"{rdr.GetString(0).Trim()}.{rdr.GetString(1)}"] = ($"{refServer}.{refSchema}.{goodNicknameName}", remarks == DBNull.Value ? null : $"{remarks}");
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"DB2 synonym metadata refresh failed ({ex.GetType().Name}).");
            }
        });
    }

    public override async Task<(string, string)> GetSynonymDataAsync(string schema, string aliasName)
    {
        if (!synonymsDic.ContainsKey($"{schema}.{aliasName}"))
        {
            await AddToSynonymsDicAsync(schema, aliasName);
        }
        return synonymsDic[$"{schema}.{aliasName}"];
    }


    public override async Task<string> GetCreateSynonymTextAsync(string schemaTablename)
    {
        int id = schemaTablename.LastIndexOf(".");
        string schema = schemaTablename.Substring(0, id);
        string tablename = schemaTablename.Substring(id + 1);

        //QuoteNameIfNeeded(ref validSchemaName);

        if (!synonymsDic.ContainsKey(schemaTablename))
        {
            await AddToSynonymsDicAsync(schema, tablename);
        }

        if (!synonymsDic.ContainsKey(schemaTablename))
        {
            return "problem";
        }
        else
        {
            return $"CREATE NICKNAME {StringExtension.QuoteNameIfNeeded(schema)}.{StringExtension.QuoteNameIfNeeded(tablename)} FOR {synonymsDic[schemaTablename].refObject};";
        }
    }

    private readonly Dictionary<string, string[]> linkedServerObjects = new Dictionary<string, string[]>();

    private async Task getLinkedServerToCacheAsync(string linkedServerName)
    {
        string SQL = $@"SET PASSTHRU {linkedServerName};
             select distinct SCHEMA, OBJNAME
             from INFORMATION_SCHEMA._V_OBJECT_DATA 
             where UPPER(OBJTYPE) in ('TABLE', 'SECURE TABLE','EXTERNAL TABLE','VIEW') AND OBJID IS NOT NULL and 
             OBJDB = current_db
             order by SCHEMA,OBJNAME;";

        bool isOracle = false;
        foreach (DataRow row in _linkedServersDt.Rows)
        {
            if (row["SERVERNAME"].ToString().ToUpper() == linkedServerName.ToUpper()
                && row["SERVERTYPE"].ToString().Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                isOracle = true;
                break;
            }
        }

        if (isOracle)
        {
            SQL = $"SET PASSTHRU {linkedServerName}; SELECT OWNER,NAME FROM ALL_DEPENDENCIES WHERE TYPE IN('VIEW','TABLE') ORDER BY OWNER,NAME;";
        }

        linkedServerObjects.Clear();
        await Task.Run(() =>
        {
            using (var conn = new DB2Connection(ConnectionString))
            {
                conn.Open();
                List<string> ls = new List<string>();
                using (var cmd = new DB2Command(SQL, conn))
                {
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string schema = rdr.GetValue(0) == DBNull.Value ? "ADMIN" : rdr.GetString(0);
                        //QuoteNameIfNeeded(ref schema);
                        ls.Add($"{schema}.{rdr.GetString(1)}");
                    }
                }
                conn.Close();
                linkedServerObjects[linkedServerName] = ls.ToArray();
            }
        });
    }

    public override async Task<string[]> GetLinkedServerTablesAsync(string linkedServerName)
    {
        if (!linkedServerObjects.ContainsKey(linkedServerName))
        {
            try
            {
                await getLinkedServerToCacheAsync(linkedServerName);
            }
            catch (Exception ex)
            {
                return new string[] { ex.Message };
            }
        }

        return linkedServerObjects[linkedServerName];
    }

    public override void BlobQuery(string sql)
    {
        using (var conn = new DB2Connection(ConnectionString))
        {
            conn.Open();
            // ? = parameter -> INSERT INTO TEST_PHOTO VALUES(?);
            int par = sql.IndexOf($"{Environment.NewLine}PATHS{Environment.NewLine}");
            if (par == -1)
            {
                throw new Exception("line with 'PATHS' is required");
            }
            var paths = sql.Substring(par + 7 + Environment.NewLine.Length).Split(Environment.NewLine);
            sql = sql.Substring(0, par);
            using (var cmd = new DB2Command(sql, conn))
            {
                int parCnt = sql.Length - sql.Replace("?", "").Length;
                if (paths.Length != parCnt)
                {
                    throw new Exception("number of paths and '?' mark must equals");
                }
                for (int i = 0; i < parCnt; i++)
                {
                    var arr = File.ReadAllBytes(paths[i]);
                    DB2Parameter param1 = new DB2Parameter($"par{i}", DB2Type.Blob, arr.Length);
                    param1.Value = arr;
                    cmd.Parameters.Add(param1);
                }
                cmd.ExecuteNonQuery();
            }
            conn.Close();
        }
    }

    protected override void DbSpecificImportPart(string randName, DataTable source, int NotifyAfter, Action<string> progress, bool tableExists = false, IDataReader rdr = null)
    {
        using var conn = new DB2Connection(ConnectionString);
        conn.Open();
        bool isProviderType = false;
        DataTable schema = null;
        if (!tableExists)
        {
            string[] headers;
            List<Type> columns = new();
            List<string> columnsNames = new();
            Dictionary<int, string> providerTypes = new();
            Dictionary<int, int> columnSizes = new();
            if (rdr == null)
            {
                for (int i = 0; i < source.Columns.Count; i++)
                {
                    columns.Add(source.Columns[i].DataType);
                    columnsNames.Add(source.Columns[i].ColumnName);
                }
            }
            else
            {
                schema = rdr.GetSchemaTable();
                var rows = schema.Rows;
                isProviderType = schema.Columns.Contains("ProviderType") && rdr is DB2DataReader;

                for (int i = 0; i < rows.Count; i++)
                {
                    columns.Add((Type)rows[i]["DataType"]);
                    columnsNames.Add((string)rows[i]["ColumnName"]);
                    if (isProviderType)
                    {
                        providerTypes[i] = rows[i]["ProviderType"].ToString();
                        columnSizes[i] = (int)rows[i]["ColumnSize"];
                    }
                }
            }

            headers = new string[columns.Count];
            for (int i = 0; i < headers.Length; i++)
            {
                string typeName = "VARCHAR(255)";
                if (isProviderType && _dataTypes.ContainsKey(Int32.Parse(providerTypes[i])))
                {
                    string currTypeName = _dataTypes[Int32.Parse(providerTypes[i])].SQL_TYPE_NAME;
                    string length = "";
                    string forNumber = "";
                    if (_dataTypes[Int32.Parse(providerTypes[i])].CREATE_PARAMS.ToString() == "LENGTH")
                    {
                        length = $"({columnSizes[i]})";
                    }
                    else if (_dataTypes[Int32.Parse(providerTypes[i])].CREATE_PARAMS.ToString() == "PRECISION,SCALE")
                    {
                        forNumber = $"({schema.Rows[i]["NumericPrecision"]},{schema.Rows[i]["NumericScale"]})";
                    }
                    typeName = $"{currTypeName}{length}{forNumber}" + (((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }
                else if (columns[i] == typeof(int))
                {
                    typeName = "INTEGER" + (isProviderType && !((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }
                else if (columns[i] == typeof(long))
                {
                    typeName = "BIGINT" + (isProviderType && !((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }
                else if (columns[i] == typeof(Int16))
                {
                    typeName = "SMALLINT" + (isProviderType && !((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }
                else if (columns[i] == typeof(float) || columns[i] == typeof(double))
                {
                    typeName = "DOUBLE" + (isProviderType && !((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }
                else if (columns[i] == typeof(decimal))
                {
                    typeName = "NUMERIC(20,6)" + (isProviderType && !((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }
                else if (columns[i] == typeof(DateTime))
                {
                    typeName = "TIMESTAMP" + (isProviderType && !((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }
                else if (columns[i] == typeof(TimeSpan))
                {
                    typeName = "TIME" + (isProviderType && !((bool)schema.Rows[i]["AllowDBNull"]) ? " NOT NULL" : "");
                }

                headers[i] = $"{columnsNames[i].NormalizeName(_databaseRuntimeContext.Config.KeyWordsListForColoring1)} {typeName}";
            }

            var cmd = new DB2Command($"CREATE TABLE {randName} ({String.Join(',', headers)}){Environment.NewLine}ORGANIZE BY ROW{Environment.NewLine}COMPRESS YES{Environment.NewLine};", conn);
            cmd.ExecuteNonQuery();
        }

        using DB2BulkCopy cpy = new DB2BulkCopy(conn, DB2BulkCopyOptions.TableLock);
        cpy.BulkCopyTimeout = _databaseRuntimeContext.Config.CommandTimeout;
        cpy.DestinationTableName = randName;

        cpy.NotifyAfter = NotifyAfter;//(int) NotifyAfter;

        cpy.DB2RowsCopied += (o, e) => progress?.Invoke($"Copied {e.RowsCopied.ToString("N0")}");
        if (rdr == null)
        {
            cpy.WriteToServer(source);
        }
        else
        {
            cpy.WriteToServer(rdr);
        }

        if (cpy.Errors.Count > 0)
        {
            if (progress is not null)
            {
                progress?.Invoke($"{cpy.Errors.Count} Errors");
                foreach (DB2Error item in cpy.Errors)
                {
                    progress?.Invoke($"ERROR! Row: {item.RowNumber} Message:{item.Message}");
                }
                cpy.Close();
                conn.Close();
            }
            else
            {
                throw new Exception($"{cpy.Errors.Count} Errors");
            }
        }
        else
        {
            cpy.Close();
            conn.Close();
        }
    }


    //SET CURRENT EXPLAIN MODE YES;
    //db2 CONNECT TO database-name;
    //db2 CALL SYSPROC.SYSINSTALLOBJECTS('EXPLAIN', 'C',      CAST(NULL AS VARCHAR(128)), CAST(NULL AS VARCHAR(128)));
    //db2advis -d<DB_NAME> -N<SCHEMA_NAME> -i db2advis.in -t 5
    //db2advis -d<DB_NAME> -N<SCHEMA_NAME> -s "QUERY" -t 5


    public override DbConnection GetConnection()
    {
        return new DB2Connection(ConnectionString);
    }

    public override string SearchInViewsSource(string txtToSearch)
    {
        txtToSearch = Regex.Escape(txtToSearch);
        string sql = @$"SELECT 
    'Views' as ""Type""
    ,TRIM(VIEWNAME) as ""Name""
    , TRIM(CURRENT_SERVER) as ""Db""
    , '' as ""Desc""
    , TRIM(VIEWSCHEMA) as ""Schema""
FROM
    SYSCAT.VIEWS
WHERE
    REGEXP_LIKE(TEXT, '\b{txtToSearch}\b', 'i')";
        return sql;

    }

    public override string SearchInProcedureSource(string txtToSearch)
    {
        txtToSearch = Regex.Escape(txtToSearch);

        string sql = $@"SELECT 
   'Procedures' as ""Type""
   , PROCNAME as ""Name""
   , TRIM(CURRENT_SERVER) as ""Db""
   , '' as ""Desc""
   , TRIM(PROCSCHEMA) as ""Schema""
    FROM SYSCAT.PROCEDURES P
WHERE 
    REGEXP_LIKE(P.TEXT, '\b{txtToSearch}\b', 'i');
";
        return sql;
    }

    public override DbConnection GetConnection(string databaseName, bool usePool = true)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return GetConnection();

        DB2ConnectionStringBuilder builder = new DB2ConnectionStringBuilder(ConnectionString);
        builder.Remove("Database");
        builder.Add("Database", databaseName);
        return new DB2Connection(builder.ConnectionString);
    }

    private static DataTable LoadCatalogTable(DB2Connection connection, string sql)
    {
        using var command = new DB2Command(sql, connection);
        using DB2DataReader reader = command.ExecuteReader();
        var result = new DataTable();
        result.Load(reader);
        return result;
    }

    private void RebuildObjectInSchema()
    {
        objectInSchema.Clear();

        if (_schemas is not null)
        {
            foreach (DataRow row in _schemas.Rows)
            {
                string schema = GetMetadataName(row, "TABLE_SCHEMA", "SCHEMA_NAME", "TABLE_SCHEM", "SCHEMANAME");
                if (schema.Length > 0)
                    objectInSchema.TryAdd(schema, new Dictionary<string, TypeInDatabase>(StringComparer.OrdinalIgnoreCase));
            }
        }

        AddMetadataObjects(tables, TypeInDatabase.table,
            ["TABLE_SCHEMA", "TABLE_SCHEM", "TABSCHEMA"],
            ["TABLE_NAME", "TABNAME"]);
        AddMetadataObjects(views, TypeInDatabase.view,
            ["TABLE_SCHEMA", "TABLE_SCHEM", "TABSCHEMA"],
            ["TABLE_NAME", "TABNAME"]);
        AddMetadataObjects(_synonyms, TypeInDatabase.synonym,
            ["TABLE_SCHEMA", "TABLE_SCHEM", "TABSCHEMA"],
            ["TABLE_NAME", "TABNAME"]);
        AddMetadataObjects(_nicknames, TypeInDatabase.db2nickname,
            ["TABLE_SCHEMA", "TABLE_SCHEM", "TABSCHEMA"],
            ["TABLE_NAME", "TABNAME"]);
        AddMetadataObjects(_aliases, TypeInDatabase.db2alias,
            ["TABLE_SCHEMA", "TABLE_SCHEM", "TABSCHEMA"],
            ["TABLE_NAME", "TABNAME"]);
        AddMetadataObjects(procedures, TypeInDatabase.procedure,
            ["PROCEDURE_SCHEMA", "PROCEDURE_SCHEM", "PROCSCHEMA", "ROUTINESCHEMA"],
            ["PROCEDURE_NAME", "PROCEDURE", "PROCNAME", "ROUTINENAME"]);
        AddMetadataObjects(functions, TypeInDatabase.function,
            ["PROCEDURE_SCHEMA", "PROCEDURE_SCHEM", "PROCSCHEMA", "ROUTINESCHEMA"],
            ["PROCEDURE_NAME", "PROCEDURE", "PROCNAME", "ROUTINENAME"]);
    }

    private void AddMetadataObjects(
        DataTable? metadata,
        TypeInDatabase objectType,
        string[] schemaColumns,
        string[] objectColumns)
    {
        if (metadata is null)
            return;

        foreach (DataRow row in metadata.Rows)
        {
            string schema = string.Empty;
            string objectName = string.Empty;
            foreach (string schemaColumn in schemaColumns)
            {
                schema = GetMetadataName(row, schemaColumn);
                if (schema.Length == 0)
                    continue;

                foreach (string objectColumn in objectColumns)
                {
                    objectName = GetMetadataName(row, objectColumn);
                    if (objectName.Length > 0)
                        break;
                }

                if (objectName.Length > 0)
                    break;
            }
            if (schema.Length == 0 || objectName.Length == 0)
                continue;

            if (!objectInSchema.TryGetValue(schema, out Dictionary<string, TypeInDatabase>? objects))
            {
                objects = new Dictionary<string, TypeInDatabase>(StringComparer.OrdinalIgnoreCase);
                objectInSchema[schema] = objects;
            }

            // Keep the first type when a provider catalog exposes the same
            // name through more than one metadata collection.
            objects.TryAdd(objectName, objectType);
            AutocompleteSuggestions.TwoWords.Add($"{schema}.{objectName}");
        }
    }

    private IReadOnlyList<Db2CatalogObject> BuildDb2CatalogObjects()
    {
        var objects = new List<Db2CatalogObject>();

        AddTableCatalogObjects(objects, tables, Db2CatalogObjectType.Table, supportsColumns: true);
        AddTableCatalogObjects(objects, views, Db2CatalogObjectType.View, supportsColumns: true);
        AddTableCatalogObjects(objects, _nicknames, Db2CatalogObjectType.Nickname, supportsColumns: true);
        AddTableCatalogObjects(objects, _aliases, Db2CatalogObjectType.Alias, supportsColumns: true);
        AddRoutineCatalogObjects(objects, procedures, Db2CatalogObjectType.Procedure);
        AddRoutineCatalogObjects(objects, functions, Db2CatalogObjectType.Function);

        AddGlobalCatalogObjects(objects);
        return objects;
    }

    private static void AddTableCatalogObjects(
        ICollection<Db2CatalogObject> target,
        DataTable? source,
        Db2CatalogObjectType type,
        bool supportsColumns)
    {
        if (source is null)
            return;

        foreach (DataRow row in source.Rows)
        {
            string schema = GetMetadataName(row, "TABLE_SCHEMA", "TABLE_SCHEM", "TABSCHEMA");
            string name = GetMetadataName(row, "TABLE_NAME", "TABNAME");
            if (schema.Length == 0 || name.Length == 0)
                continue;

            target.Add(new Db2CatalogObject(
                type,
                name,
                schema,
                GetMetadataName(row, "REMARKS"),
                GetMetadataName(row, "OWNER"),
                supportsColumns));
        }
    }

    private static void AddRoutineCatalogObjects(
        ICollection<Db2CatalogObject> target,
        DataTable? source,
        Db2CatalogObjectType type)
    {
        if (source is null)
            return;

        foreach (DataRow row in source.Rows)
        {
            string schema = GetMetadataName(row, "PROCEDURE_SCHEMA", "PROCEDURE_SCHEM", "PROCSCHEMA", "ROUTINESCHEMA");
            string name = GetMetadataName(row, "PROCEDURE_NAME", "PROCEDURE", "PROCNAME", "ROUTINENAME");
            if (schema.Length == 0 || name.Length == 0)
                continue;

            target.Add(new Db2CatalogObject(
                type,
                name,
                schema,
                GetMetadataName(row, "REMARKS"),
                GetMetadataName(row, "OWNER", "DEFINER")));
        }
    }

    private void AddGlobalCatalogObjects(ICollection<Db2CatalogObject> target)
    {
        foreach (DataRow row in _wrappersDt.Rows)
        {
            string name = GetMetadataName(row, "WRAPNAME");
            if (name.Length == 0)
                continue;

            target.Add(new Db2CatalogObject(
                Db2CatalogObjectType.Wrapper,
                name,
                Description: GetMetadataName(row, "REMARKS"),
                Owner: GetMetadataName(row, "WRAPTYPE")));
        }

        foreach (DataRow row in _wrappersOptionsDt.Rows)
        {
            string wrapper = GetMetadataName(row, "WRAPNAME");
            string option = GetMetadataName(row, "OPTION");
            if (wrapper.Length == 0 || option.Length == 0)
                continue;

            target.Add(new Db2CatalogObject(
                Db2CatalogObjectType.WrapperOption,
                $"{wrapper} / {option}",
                Description: GetMetadataName(row, "SETTING"),
                Owner: wrapper));
        }

        foreach (DataRow row in _linkedServersDt.Rows)
        {
            string server = GetMetadataName(row, "SERVERNAME");
            if (server.Length == 0)
                continue;

            target.Add(new Db2CatalogObject(
                Db2CatalogObjectType.Server,
                server,
                Description: GetMetadataName(row, "REMARKS"),
                Owner: GetMetadataName(row, "WRAPNAME")));
        }

        foreach (DataRow row in _linkedServersOptionsDt.Rows)
        {
            string server = GetMetadataName(row, "SERVERNAME");
            string option = GetMetadataName(row, "OPTION");
            if (server.Length == 0 || option.Length == 0)
                continue;

            string wrapper = _linkedServersDt.AsEnumerable()
                .Where(serverRow => string.Equals(GetMetadataName(serverRow, "SERVERNAME"), server, StringComparison.OrdinalIgnoreCase))
                .Select(serverRow => GetMetadataName(serverRow, "WRAPNAME"))
                .FirstOrDefault() ?? string.Empty;

            target.Add(new Db2CatalogObject(
                Db2CatalogObjectType.ServerOption,
                $"{server} / {option}",
                Description: GetMetadataName(row, "SETTING"),
                Owner: wrapper));
        }

        foreach (var group in _userMapingsDt.AsEnumerable()
                     .GroupBy(row => (
                         Server: GetMetadataName(row, "SERVERNAME"),
                         AuthId: GetMetadataName(row, "AUTHID"),
                         AuthType: GetMetadataName(row, "AUTHIDTYPE"))))
        {
            if (group.Key.Server.Length == 0 || group.Key.AuthId.Length == 0)
                continue;

            target.Add(new Db2CatalogObject(
                Db2CatalogObjectType.UserMapping,
                $"{group.Key.Server} / {group.Key.AuthId}",
                Description: group.Select(row => GetMetadataName(row, "SETTING"))
                    .FirstOrDefault(value => value.Length > 0) ?? string.Empty,
                Owner: group.Key.AuthType));
        }

        foreach (DataRow row in _passthruDt.Rows)
        {
            string server = GetMetadataName(row, "SERVERNAME");
            string grantee = GetMetadataName(row, "GRANTEE");
            string grantor = GetMetadataName(row, "GRANTOR");
            if (server.Length == 0 || grantee.Length == 0 || grantor.Length == 0)
                continue;

            string granteeType = GetMetadataName(row, "GRANTEETYPE");
            string grantorType = GetMetadataName(row, "GRANTORTYPE");
            string description = granteeType;
            if (grantorType.Length > 0)
                description = $"{description} -> {grantorType}";

            target.Add(new Db2CatalogObject(
                Db2CatalogObjectType.PassthruAuth,
                $"{server} / {grantee} / {grantor}",
                Description: description,
                Owner: grantee));
        }
    }

    private static string GetMetadataName(DataRow row, params string[] columnNames)
    {
        foreach (string columnName in columnNames)
        {
            if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                continue;

            string value = row[columnName]?.ToString()?.Trim() ?? string.Empty;
            if (value.Length > 0)
                return value;
        }

        return string.Empty;
    }
}

