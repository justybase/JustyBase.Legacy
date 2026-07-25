using AppBase.Common;
using System.Data.Common;
using System.IO.Pipes;
using System.Text.RegularExpressions;

namespace AppBase.Services;

public sealed class FastNetezzaCsvImport : IFastNetezzaCsvImport
{
    public string FilePath { get; set; }
    public string Tablename { get; set; }
    public bool ImportToExisting { get; set; }
    public string ConnectionString { get; set; }
    public long ProgessUnit { get; set; }

    public bool StopOnEmpty { get; set; }

    public bool StopTask { get; set; }
    public event Action<long> Progress;

    public bool TransformRow { get; set; }
    public Regex RxTransform { get; set; }
    public string RelaceValue { get; set; }

    public bool FilterRow { get; set; }
    public Regex RxFilter { get; set; }

    public bool RejectRow { get; set; }
    public Regex RxReject { get; set; }

    public bool SingleColumnMode { get; set; }

    private string _escapedDelimiter;
    public bool Limit1000 { get; set; }

    public event Action ForcedStop;

    public event Action BeforeInsert;
    public event Action AfterInsert;

    public Func<string[]> GetCollumnsFun { get; set; }
    public FastNetezzaCsvImport()
    {
    }

    public bool BufferedVersion { get; set; }
    public string StartServer()
    {
        string serverName = $"JustyBaseLegacy_{_random.Next(0, 9999)}";
        if (BufferedVersion)
        {
            FileServer2(serverName, -1);
        }
        else
        {
            FileServer(serverName, -1);
        }
        Thread.Sleep(100);
        return serverName;
    }

    public string[] GetHeaders()
    {
        return GetCollumnsFun.Invoke();
    }

    public char escapechar { get; set; }
    public char ColumnDelimiter { get; set; }
    public char DECIMALDELIM { get; set; }
    public string RecordDelim { get; set; }
    public string REMOTESOURCE { get; set; }
    public string NULLVALUE { get; set; }
    public string ENCODING { get; set; }
    public string TIMESTYLE { get; set; }
    public string LOGDIR { get; set; }
    public long MAXROWS { get; set; }
    public long SocketBufSize { get; set; }
    public bool TruncString { get; set; }
    public bool CRinString { get; set; }
    public bool LFinString { get; set; }
    public bool CtrlChars { get; set; }
    public bool FillRecord { get; set; }
    public bool IgnoreZero { get; set; }
    public bool IncludeHeader { get; set; }
    public bool IncludeZeroSeconds { get; set; }
    public bool Compress { get; set; }
    public bool RequireQuotes { get; set; }
    public bool TimeRoundNanos { get; set; }

    public long SkipRows { get; set; }

    public (string createSql, string inserSql, string fullCreate) GetCodes(string tablename, string serverName)
    {
        string nl = Environment.NewLine;

        _escapedDelimiter = $"{escapechar}{ColumnDelimiter}";
        var headers = GetHeaders();
        string ColumnDelimiterStr = ColumnDelimiter.ToString();
        if (ColumnDelimiter == '\t')
        {
            ColumnDelimiterStr = "\\t";
        }

        string usingCode = @$"{nl}USING(
                REMOTESOURCE '{REMOTESOURCE}'
                DELIMITER '{ColumnDelimiterStr}'
                DECIMALDELIM '{DECIMALDELIM}'
                --RecordDelim '{RecordDelim}'
                SKIPROWS {SkipRows}
                MAXROWS  {MAXROWS}
                SOCKETBUFSIZE {SocketBufSize}
                NULLVALUE '{NULLVALUE}'
                ENCODING '{ENCODING}'
                TRUNCSTRING {(TruncString ? "True" : "False")}
                CRinString {(CRinString ? "True" : "False")}
                LFinString {(LFinString ? "True" : "False")}
                CtrlChars {(CtrlChars ? "True" : "False")}
                FillRecord {(FillRecord ? "True" : "False")}
                IgnoreZero {(IgnoreZero ? "True" : "False")}
                IncludeHeader {(IncludeHeader ? "True" : "False")}
                IncludeZeroSeconds {(IncludeZeroSeconds ? "True" : "False")}
                Compress {(Compress ? "True" : "False")}
                RequireQuotes {(RequireQuotes ? "True" : "False")}
                TimeRoundNanos {(TimeRoundNanos ? "True" : "False")}
                --ESCAPECHAR '{escapechar}'
                QUOTEDVALUE 'DOUBLE' 
                TIMESTYLE '{TIMESTYLE}'
                MAXERRORS {MAXROWS}
                LOGDIR '{LOGDIR}'
                )";
        string selectCode = @$"SELECT * FROM EXTERNAL '\\.\pipe\{serverName}' ({String.Join(',', headers)})";

        string createSql = $"CREATE TABLE {tablename} ({String.Join(',', headers)}){nl}DISTRIBUTE ON RANDOM;{nl}{nl}";
        string inserSql = @$"INSERT INTO {tablename} {selectCode} {usingCode};";

        string fullCreate = $"CREATE TABLE {tablename} AS {nl} ({selectCode} {usingCode}){nl}DISTRIBUTE ON RANDOM;{nl}{nl}";

        return (createSql, inserSql, fullCreate);
    }

    private Random _random = new Random();
    public void MakeImport(string serverName, int commandTimeout, Func<string, DbConnection> _getConnection)
    {
        StopTask = false;

        var sqls = GetCodes(Tablename, serverName);
        string createSql = sqls.createSql;
        string insertSql = sqls.inserSql;

        using (DbConnection conn = _getConnection(ConnectionString))
        {
            conn.Open();

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = createSql;
            if (!ImportToExisting)
            {
                cmd.CommandTimeout = commandTimeout;
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = insertSql;
            OnPreInsert();
            cmd.ExecuteNonQuery();
            OnAfterInsert();
        }
    }

    private void OnPreInsert()
    {
        BeforeInsert?.Invoke();
    }

    private void OnAfterInsert()
    {
        AfterInsert?.Invoke();
    }

    private void FileServer(string serverName, int RowCounts)
    {
        Task.Run(() =>
        {
            var rdrExt = new StreamReader(FilePath);

            var server = new NamedPipeServerStream(serverName);
            server.WaitForConnection();
            StreamWriter writer = new StreamWriter(server);

            string line;
            long i = 1;
            while ((line = rdrExt.ReadLine()) != null)
            {
                if (line == "")
                {
                    if (this.StopOnEmpty)
                    {
                        break;
                    }
                    continue;
                }
                if (FilterRow && !RxFilter.IsMatch(line))
                {
                    continue;
                }
                if (RejectRow && RxReject.IsMatch(line))
                {
                    continue;
                }

                if (TransformRow)
                {
                    line = RxTransform.Replace(line, RelaceValue);
                }

                if (SingleColumnMode && line.Contains(ColumnDelimiter))
                {
                    line = line.Replace(ColumnDelimiter.ToString(), _escapedDelimiter);
                }

                //writer.WriteLine(line.Trim());
                writer.WriteLine(line);
                if (i % ProgessUnit == 0)
                {
                    Progress?.Invoke(rdrExt.BaseStream.Position);
                }
                if (StopTask || Limit1000 && i >= 1000)
                {
                    ForcedStop?.Invoke();
                    break;
                }
                i++;
            }

            writer.Flush();
            Progress?.Invoke(rdrExt.BaseStream.Length);
            server.Close();
            rdrExt.Close();

        });
    }

    const int bufferSize = 65_536;
    private void FileServer2(string serverName, int RowCounts)
    {
        Task.Run(() =>
        {
            var rdrExt = new StreamReader(FilePath);
            var server = new NamedPipeServerStream(serverName);
            server.WaitForConnection();
            StreamWriter writer = new StreamWriter(server);
            Span<char> buffer = new char[bufferSize];
            int num = 0;
            do
            {
                num = rdrExt.ReadBlock(buffer);
                writer.Write(buffer.Slice(0, num));
            } while (num > 0);

            writer.Flush();
            Progress?.Invoke(rdrExt.BaseStream.Length);
            server.Close();
            rdrExt.Close();
        });
    }

}
