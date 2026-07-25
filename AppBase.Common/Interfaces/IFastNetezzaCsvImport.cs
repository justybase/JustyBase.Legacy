using System.Data.Common;
using System.Text.RegularExpressions;

namespace AppBase.Common;

public interface IFastNetezzaCsvImport
{
    Func<string[]> GetCollumnsFun { get; set; }
    Regex RxFilter { get; set; }
    Regex RxReject { get; set; }
    Regex RxTransform { get; set; }
    bool BufferedVersion { get; set; }
    char ColumnDelimiter { get; set; }
    bool Compress { get; set; }
    string ConnectionString { get; set; }
    bool CRinString { get; set; }
    bool CtrlChars { get; set; }
    char DECIMALDELIM { get; set; }
    string ENCODING { get; set; }
    char escapechar { get; set; }
    string FilePath { get; set; }
    bool FillRecord { get; set; }
    bool FilterRow { get; set; }
    bool IgnoreZero { get; set; }
    bool ImportToExisting { get; set; }
    bool IncludeHeader { get; set; }
    bool IncludeZeroSeconds { get; set; }
    bool LFinString { get; set; }
    bool Limit1000 { get; set; }
    string LOGDIR { get; set; }
    long MAXROWS { get; set; }
    string NULLVALUE { get; set; }
    long ProgessUnit { get; set; }
    string RecordDelim { get; set; }
    bool RejectRow { get; set; }
    string RelaceValue { get; set; }
    string REMOTESOURCE { get; set; }
    bool RequireQuotes { get; set; }
    bool SingleColumnMode { get; set; }
    long SkipRows { get; set; }
    long SocketBufSize { get; set; }
    bool StopOnEmpty { get; set; }
    bool StopTask { get; set; }
    string Tablename { get; set; }
    bool TimeRoundNanos { get; set; }
    string TIMESTYLE { get; set; }
    bool TransformRow { get; set; }
    bool TruncString { get; set; }

    event Action AfterInsert;
    event Action BeforeInsert;
    event Action ForcedStop;
    event Action<long> Progress;

    (string createSql, string inserSql, string fullCreate) GetCodes(string tablename, string serverName);
    string[] GetHeaders();
    void MakeImport(string serverName, int commandTimeout, Func<string, DbConnection> _getConnection);
    string StartServer();
}
