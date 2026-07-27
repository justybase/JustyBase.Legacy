using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;

namespace AppBase.Data.Core.Interfaces;

public interface IGeneralDbService
{
    DatabaseTypeEnum RelatedDatabaseType { get; set; }

    static string ActiveQuerySql(DatabaseTypeEnum databaseType)
    {
        switch (databaseType)
        {
            case DatabaseTypeEnum.DB2:
                return @"SELECT
    t.*,
    'CALL SYSPROC.ADMIN_CMD(''FORCE APPLICATION (' || TRIM(CHAR(t.APPLICATION_HANDLE)) || ')'')' AS DROP_SESSION_SQL
FROM
    SYSIBMADM.MON_CURRENT_SQL t";
            case DatabaseTypeEnum.MsSqlDb:
                break;
            case DatabaseTypeEnum.Oracle:
                break;
            case DatabaseTypeEnum.Postgres:
                return @"select
    a.*,
    'SELECT pg_terminate_backend(' || a.pid::text || ');' AS DROP_SESSION_SQL
from
    pg_stat_activity a
where
    query != ''
    and query is not null
    and state = 'active'";
            case DatabaseTypeEnum.Netezza:
                return @"SELECT
    S.ID
    , 'DROP SESSION ' || ID || ';' AS DROP_SESSION_SQL
    , S.USERNAME
    , S.DBNAME
    , S.CONNTIME
    , S.STATUS
    , S.COMMAND
    , S.TYPE
    , S.IPADDR
    , CASE WHEN Q.QS_TSTART = 'epoch' THEN 0 ELSE ABSTIME 'now' - Q.QS_TSTART END AS ELAPSED_SECS
    , ROUND(Q.QS_ESTCOST/1000.0,0) AS ESTIMATED_SECS
    , ROUND(Q.QS_ESTMEM / 1024.0, 0) AS ESTIMATED_MEMORY_MB
    , Q.QS_ESTDISK / 1024 AS ESTIMATED_DISK_MB
    , Q.QS_SNIPPETS AS SNIPPETS
    , Q.QS_CURSNIPT AS CURRENTSNIPET
    , SUBSTRING(Q.QS_SQL,1,500) AS QS_SQL
    , Q.QS_STATE
    , Q.QS_TSUBMIT
    , Q.QS_TSTART
    , Q.QS_PLANID
    , Q.QS_SESSIONID
    , Q.QS_CLIENTID
    , Q.QS_CLIIPADDR
    , INITCAP(Q.QS_PRITXT) AS PRIORYTY
    , Q.QS_RESROWS AS RESOULTROWS
    , Q.QS_RESBYTES AS RESOULTBYES
FROM
    _V_SESSION S
    LEFT JOIN _V_QRYSTAT Q ON Q.QS_SESSIONID = S.ID
WHERE 
    1 = 1
    --AND S.STATUS = 'active'
    --AND ELAPSED_SECS IS NOT NULL
    --AND ESTIMATED_SECS IS NOT NULL
ORDER BY 
    ELAPSED_SECS DESC NULLS LAST
    , S.CONNTIME ASC;
";
            default:
                break;
        }
        return string.Empty;
    }

    int MinimumNumericPrecision { get; }
    HashSet<string> ReservedWords { get; }
    string[]? ClipToLines(char sepInClipboard, ref string clip, char escapechar);

    string ConnectionStringForDB2(string connectionName);

    string ConnectionStringForOracle(string connectionName);

    string ConnectionStringForMsSql(string connectionName);
    string ConnectionStringForMsSql(string connectionName, string db);
    string ConnectionStringForMsSqlTrusted(string connectionName);
    string ConnectionStringForMsSqlTrusted(string connectionName, string db);
    string ConnectionStringForNz(int timeout, string connectionName, string? db = null);
    string ConnectionStringForPostgreSQL(string connectionName);
    string ConnectionStringOleDbForAccess(string connectionName);
    string ConnectionStringOleDbForNz(int timeout, string connectionName);
    string ConnectionStringOleDbForNz(int timeout, string connectionName, string db);
    string DBname(string connectionName);
    string? DriverName(string connectionName);
    public IGeneralDb? GetGeneralDb(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, IImportExportTasks importExportTasks, string connectionName, out string dbName);
    string? Password(string connectionName);
    string PrepareValue(out DatabaseColumnType nz, string text, bool typeAdn = true, string textQualifier = "'", bool doTrim = true, bool forceTimestamp = true);
    string? Server(string connectionName);
    string SqlLitePath(IDatabaseRuntimeContext databaseRuntimeContext, string connectionName);
    string TypeToName(IGeneralDb type);
    string? UserName(string connectionName);
}
