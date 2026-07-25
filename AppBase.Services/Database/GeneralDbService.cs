using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Data;
using AppBase.Data.Core.Core;
using AppBase.Data.Core.Enums;
using AppBase.Data.Core.Interfaces;


#if INCLUDE_ORACLE
using Oracle.ManagedDataAccess.Client;
#endif
using System.Globalization;

namespace AppBase.Services;

public partial class GeneralDbService : IGeneralDbService
{
    public int MinimumNumericPrecision => 6;

    private readonly NumberStyles _style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowExponent;
    private readonly CultureInfo _cultureUS = CultureInfo.CreateSpecificCulture("en-US");

    private readonly NumberFormatInfo _nfi = new NumberFormatInfo()
    {
        NumberDecimalSeparator = ".",
        NumberDecimalDigits = 6
    };

    public HashSet<string> ReservedWords { get; } = [
        "ABORT", "ALL", "ALLOCATE", "ANALYSE", "ANALYZE", "AND", "ANY", "AS", "ASC", "BETWEEN", "BINARY", "BIT", "BOTH",
        "CASE", "CAST", "CHAR", "CHARACTER", "CHECK", "CLUSTER", "COALESCE", "COLLATE", "COLLATION", "COLUMN", "CONSTRAINT",
        "COPY", "CROSS", "CURRENT", "CURRENT_CATALOG", "CURRENT_DATE", "CURRENT_DB", "CURRENT_SCHEMA", "CURRENT_SID",
        "CURRENT_TIME", "CURRENT_TIMESTAMP", "CURRENT_USER", "CURRENT_USERID", "CURRENT_USEROID", "DEALLOCATE", "DEC",
        "DECIMAL", "DECODE", "DEFAULT", "DESC", "DISTINCT", "DISTRIBUTE", "DO", "ELSE", "END", "EXCEPT", "EXCLUDE",
        "EXISTS", "EXPLAIN", "EXPRESS", "EXTEND", "EXTERNAL", "EXTRACT", "FALSE", "FIRST", "FLOAT", "FOLLOWING", "FOR",
        "FOREIGN", "FROM", "FULL", "FUNCTION", "GENSTATS", "GLOBAL", "GROUP", "HAVING", "IDENTIFIER_CASE", "ILIKE", "IN",
        "INDEX", "INITIALLY", "INNER", "INOUT", "INTERSECT", "INTERVAL", "INTO", "LEADING", "LEFT", "LIKE", "LIMIT",
        "LOAD", "LOCAL", "LOCK", "MINUS", "MOVE", "NATURAL", "NCHAR", "NEW", "NOT", "NOTNULL", "NULL", "NULLS", "NUMERIC",
        "NVL", "NVL2", "OFF", "OFFSET", "OLD", "ON", "ONLINE", "ONLY", "OR", "ORDER", "OTHERS", "OUT", "OUTER", "OVER",
        "OVERLAPS", "PARTITION", "POSITION", "PRECEDING", "PRECISION", "PRESERVE", "PRIMARY", "RESET", "REUSE", "RIGHT",
        "ROWS", "SELECT", "SESSION_USER", "SETOF", "SHOW", "SOME", "TABLE", "THEN", "TIES", "TIME", "TIMESTAMP", "TO",
        "TRAILING", "TRANSACTION", "TRIGGER", "TRIM", "TRUE", "UNBOUNDED", "UNION", "UNIQUE", "USER", "USING", "VACUUM",
        "VARCHAR", "VERBOSE", "VERSION", "VIEW", "WHEN", "WHERE", "WITH", "WRITE", "RESET", "REUSE"
    ];

    private int _intNumber;
    private decimal _decimalNumber;
    private DateTime _dateTimeValue;

    private readonly ILogger _logger;
    private readonly IDatabaseProviderFactory _providerFactory;

    public Dictionary<string, LoginData> LoginDataDic { get; set; } = [];

    public GeneralDbService(ILogger logger, IDatabaseProviderFactory providerFactory = null)
    {
        _logger = logger;
        _providerFactory = providerFactory ?? new DatabaseProviderFactory();
        _nfi.NumberDecimalDigits = MinimumNumericPrecision;
        RelatedDatabaseType = DatabaseTypeEnum.Netezza;
    }

    public string[] ClipToLines(char sepInClipboard, ref string clip, char escapechar)
    {
        List<int> l1 = new List<int>();

        clip = clip.Replace("\r", "");
        int n = clip.Length;
        for (int i = 1; i < n - 1; i++)
        {
            if (clip[i] == sepInClipboard && clip[i + 1] == '"')
            {
                i += 2;
                while (clip[i] != '"')
                {
                    i++;
                }
            }
            else if (clip[i] == '\n')
            {
                l1.Add(i);
            }
        }
        if (l1.Count == 0)
        {
            _logger.Log("no data");
            return null;
        }

        string[] q1 = new string[l1.Count + 1];
        q1[0] = clip.Substring(0, l1[0]);
        for (int i = 1; i < l1.Count; i++)
        {
            q1[i] = clip.Substring(l1[i - 1] + 1, l1[i] - l1[i - 1] - 1).Replace("\n", $"{escapechar}\n");
        }
        q1[l1.Count] = clip.Substring(l1[l1.Count - 1] + 1).Replace("\n", $"{escapechar}\n");

        return q1;
    }

    public string ConnectionStringForDB2(string connectionName)
    {
        return $"Server={Server(connectionName)};Database={DBname(connectionName)};Connect Timeout=10;UID={UserName(connectionName)};PWD={Password(connectionName)}";
    }

    public string ConnectionStringForMsSql(string connectionName)
    {
        return $"Server={Server(connectionName)};Database={DBname(connectionName)};User Id={UserName(connectionName)};Password={Password(connectionName)};";
    }

    public string ConnectionStringForMsSql(string connectionName, string db)
    {
        return $"Server={Server(connectionName)};Database={db};User Id={UserName(connectionName)};Password={Password(connectionName)};";
    }

    public string ConnectionStringForMsSqlTrusted(string connectionName)
    {
        return $"Server={Server(connectionName)};Database={DBname(connectionName)};Trusted_Connection=True;";
    }

    public string ConnectionStringForMsSqlTrusted(string connectionName, string db)
    {
        return $"Server={Server(connectionName)};Database={db};Trusted_Connection=True;";
    }

    public string ConnectionStringForNz(int timeout, string connectionName, string db = null)
    {
        string fullServer = Server(connectionName);
        string server = fullServer;
        string port = "5480";
        int indx = fullServer.LastIndexOf(':');
        if (indx != -1 && indx < server.Length - 1)
        {
            server = fullServer.Substring(0, indx);
            port = fullServer.Substring(indx + 1);
        }
        string username = UserName(connectionName);
        string password = Password(connectionName);
        string dbName = "";
        if (db is not null)
        {
            dbName = db;
        }
        else
        {
            dbName = DBname(connectionName);
        }
        return $"USERNAME={username};PASSWORD={password};PORT={port};HOST={server};DATABASE={dbName};TIMEOUT={timeout};";
    }

    public string ConnectionStringForOracle(string connectionName)
    {
        string server = Server(connectionName);
        if (server.StartsWith("Tns:"))
        {
#if INCLUDE_ORACLE
            if (OracleConfiguration.TnsAdmin != server[4..])
            {
                OracleConfiguration.TnsAdmin = server[4..];
            }

            if (OracleConfiguration.WalletLocation != OracleConfiguration.TnsAdmin)
            {
                OracleConfiguration.WalletLocation = OracleConfiguration.TnsAdmin;
            }

            return $"User Id={UserName(connectionName)};Password={Password(connectionName)};Data Source={DBname(connectionName)};Connection Timeout=30;";
#endif
            return "not supported";
        }
        else if (server.StartsWith("TLS:"))
        {
            return $"User Id={UserName(connectionName)};Password={Password(connectionName)};Data Source={server[4..]};Connection Timeout=30;";
        }
        else
        {
            return $"User Id={UserName(connectionName)};Password={Password(connectionName)};Data Source={Server(connectionName)}/{DBname(connectionName)}";
        }
    }

    public string ConnectionStringForPostgreSQL(string connectionName)
    {
        string fullServer = Server(connectionName);
        string server = fullServer;
        string port = "5432";
        int indx = fullServer.LastIndexOf(':');
        if (indx != -1 && indx < server.Length - 1)
        {
            server = fullServer.Substring(0, indx);
            port = fullServer.Substring(indx + 1);
        }

        return $"Host={server};Port={port};Username={UserName(connectionName)};Password={Password(connectionName)};Database={DBname(connectionName)}";
    }

    public string ConnectionStringOleDbForAccess(string connectionName)
    {
        return $"Provider={DriverName(connectionName)};Data Source={Server(connectionName)}\\{DBname(connectionName)};Persist Security Info=False;";
    }

    public string ConnectionStringOleDbForNz(int timeout, string connectionName)
    {
        return $"Provider=NZOLEDB;Password='{Password(connectionName)}';User ID='{UserName(connectionName)}';Data Source={Server(connectionName)};Initial Catalog={DBname(connectionName)};Persist Security Info=True;Logging Level=0;Connect Timeout={timeout};";
    }

    public string ConnectionStringOleDbForNz(int timeout, string connectionName, string db)
    {
        return $"Provider=NZOLEDB;Password='{Password(connectionName)}';User ID='{UserName(connectionName)}';Data Source={Server(connectionName)};Initial Catalog={db};Persist Security Info=True;Logging Level=0;Connect Timeout={timeout};";
    }

    public string DBname(string connectionName)
    {
        return LoginDataDic[connectionName].Database;
    }

    public string DriverName(string connectionName)
    {
        if (!LoginDataDic.ContainsKey(connectionName))
            return null;
        return LoginDataDic[connectionName].Driver;
    }


    public DatabaseTypeEnum RelatedDatabaseType { get; set; }

    public IGeneralDb GetGeneralDb(IDatabaseRuntimeContext databaseRuntimeContext, ILogger logger, IImportExportTasks importExportTasks, string connectionName, out string dbName)
    {
        IGeneralDb gdb = null;
        try
        {
            DatabaseProviderFactoryResult result = _providerFactory.Create(
                databaseRuntimeContext,
                logger,
                importExportTasks,
                this,
                connectionName,
                databaseRuntimeContext is null ? Color.Empty : databaseRuntimeContext.LogErrorStdColor);

            dbName = result.DatabaseName;
            gdb = result.Database;
            if (result.DatabaseType.HasValue)
            {
                RelatedDatabaseType = result.DatabaseType.Value;
            }
        }
        catch (FileNotFoundException ex)
        {
            dbName = "problem";
            if (ex.Message.Contains("Could not load file or assembly"))
            {
                logger.LogError("Error", ex);
            }
        }

        return gdb;
    }

    public string? Password(string connectionName)
    {
        if (!LoginDataDic.TryGetValue(connectionName, out LoginData? value))
            return null;

        return value.Password;
    }

    public string PrepareValue(out DatabaseColumnType nz, string text, bool typeAdn = true, string textQualifier = "'", bool doTrim = true, bool forceTimestamp = true)
    {
        string res = doTrim ? text.Trim() : text;

        if (res == "" || res.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            nz = DatabaseColumnType.noinfo;
            return "";
        }

        bool integerTest = int.TryParse(res, _style, CultureInfo.CurrentCulture, out _intNumber);

        bool decimalTest = decimal.TryParse(res, _style, CultureInfo.CurrentCulture, out _decimalNumber);
        if (!decimalTest)
        {
            decimalTest = decimal.TryParse(res, _style, _cultureUS, out _decimalNumber);
        }

        if (integerTest && (int)_decimalNumber == _intNumber)//"simple" number
        {
            if (res.StartsWith('0') && res != "0")
            {
                nz = DatabaseColumnType.nvarchar;
                return $"{textQualifier}{res}{textQualifier}";
            }
            else
            {
                nz = DatabaseColumnType.integer;
                return _intNumber.ToString();
            }
        }

        if (decimalTest && !res.Contains('.') && !res.Contains(',') && res.Length >= 9)//REGON, IBAN, etc.
        {
            nz = DatabaseColumnType.nvarchar;
            return $"{textQualifier}{res}{textQualifier}";
        }
        else if (decimalTest)//"simple" number
        {
            nz = DatabaseColumnType.numeric;
            return Math.Round(_decimalNumber, MinimumNumericPrecision).ToString(_nfi);
        }

        if (res.EndsWith('%'))
        {
            decimalTest = decimal.TryParse(res.Substring(0, res.Length - 1), _style, CultureInfo.CurrentCulture, out _decimalNumber);
            if (!decimalTest)
            {
                decimalTest = decimal.TryParse(res, _style, _cultureUS, out _decimalNumber);
            }
            if (decimalTest)
            {
                nz = DatabaseColumnType.numeric;
                return Math.Round(_decimalNumber * 0.01m, MinimumNumericPrecision).ToString(_nfi);
            }
        }

        bool dataTimeTest = DateTime.TryParse(res, out _dateTimeValue);
        if (!dataTimeTest)
        {
            dataTimeTest = DateTime.TryParse(res, _cultureUS, DateTimeStyles.None, out _dateTimeValue);
        }

        if (dataTimeTest)// "simple" date
        {
            if (_dateTimeValue.TimeOfDay.Ticks == 0 && !forceTimestamp)
            {
                nz = DatabaseColumnType.date;
                string type = typeAdn ? "date " : "";
                return $"{type}{textQualifier}{_dateTimeValue.ToString("yyyy-MM-dd")}{textQualifier}";
            }
            else
            {
                nz = DatabaseColumnType.timestamp;
                string type = typeAdn ? "timestamp " : "";
                return $"{type}{textQualifier}{_dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss")}{textQualifier}";
            }
        }
        else
        {
            nz = DatabaseColumnType.nvarchar;
            return $"{textQualifier}{res}{textQualifier}";
        }
    }



    public string? Server(string connectionName)
    {
        if (!LoginDataDic.ContainsKey(connectionName))
            return null;
        return LoginDataDic[connectionName].Server;
    }

    public string SqlLitePath(IDatabaseRuntimeContext databaseRuntimeContext, string connectionName)
    {
        return $"{databaseRuntimeContext.ConfigDirectory}\\schema_{connectionName}.db";
    }

    public string TypeToName(IGeneralDb type)
    {
        string res = "";

        switch (type.DatabaseType)
        {
            case DatabaseTypeEnum.Netezza:
                res = "NetezzaSQL";
                break;
#if INCLUDE_DB2
            case DatabaseTypeEnum.DB2:
                res = "DB2";
                break;
#endif
#if INCLUDE_ORACLE
            case DatabaseTypeEnum.Oracle:
                res = "Oracle";
                break;
#endif
            case DatabaseTypeEnum.Postgres:
                res = "Postgres";
                break;
            case DatabaseTypeEnum.MsSqlDb:
                res = "MsSql";
                break;
            default:
                res = "Unknown";
                break;
        }
        return res;
    }

    public string UserName(string connectionName)
    {
        if (!LoginDataDic.TryGetValue(connectionName, out LoginData? value))
            return null;
        return value.UserName;
    }

}
