using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace FastColoredTextBoxNS.Helpers;

/// <summary>
/// Env-gated keystroke span logger for BIG.SQL typing diagnosis.
/// Enable with JUSTYBASE_SQL_TYPING_PERF=1 before process start.
/// Writes NDJSON to %LocalAppData%\JustyBase\perf\sql-typing-spans-*.ndjson.
/// </summary>
public sealed class SqlTypingPerfProbe
{
    public const string EnvVarName = "JUSTYBASE_SQL_TYPING_PERF";
    public const int SlowBudgetMs = 16;
    private const int FlushEvery = 32;

    public static SqlTypingPerfProbe Instance { get; } = new();

    private readonly object _writeLock = new();
    private readonly ConcurrentDictionary<string, OpStats> _stats = new(StringComparer.Ordinal);
    private StreamWriter? _writer;
    private string? _filePath;
    private int _uiThreadId = -1;
    private int _pendingFlush;
    private int _initialized;
    private string? _lastDocumentKey;
    private int _lastChars;
    private int _lastLines;
    private int _lastChangedChars;

    public bool Enabled { get; private set; }

    public string? LogFilePath => _filePath;

    public void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        string? flag = Environment.GetEnvironmentVariable(EnvVarName);
        Enabled = string.Equals(flag, "1", StringComparison.Ordinal)
                  || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
        if (!Enabled)
            return;

        _uiThreadId = Environment.CurrentManagedThreadId;

        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JustyBase",
                "perf");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, $"sql-typing-spans-{DateTime.Now:yyyyMMdd-HHmmss}.ndjson");
            _writer = new StreamWriter(_filePath, append: true, Encoding.UTF8)
            {
                // FlaUI waits on NDJSON lines between keystrokes; must be visible immediately.
                AutoFlush = true
            };
            EmitRaw(new SpanRecord(
                Op: "session",
                Phase: "start",
                DurationMs: 0,
                Slow: false,
                Chars: -1,
                Lines: -1,
                ChangedChars: -1,
                Meta: $"env={EnvVarName}=1",
                DocumentKey: null));
            SqlTypingPerfLocal.Enabled = true;
            Trace.WriteLine($"[SqlTypingPerf] enabled file={_filePath}");
        }
        catch (Exception ex)
        {
            Enabled = false;
            Trace.WriteLine($"[SqlTypingPerf] init failed: {ex.GetType().Name}: {ex.Message}");
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { WriteSessionSummary(); } catch { /* ignore */ }
            try { _writer?.Dispose(); } catch { /* ignore */ }
        };
    }

    public void MarkDocChange(string documentKey, int chars, int lines, int changedChars)
    {
        if (!Enabled)
            return;

        _lastDocumentKey = documentKey;
        _lastChars = chars;
        _lastLines = lines;
        _lastChangedChars = changedChars;
    }

    public IDisposable Measure(
        string op,
        string phase = "end",
        string? documentKey = null,
        int chars = -1,
        int lines = -1,
        int changedChars = -1,
        string? meta = null)
    {
        if (!Enabled)
            return NoopDisposable.Instance;

        return new MeasureScope(this, op, phase, documentKey, chars, lines, changedChars, meta);
    }

    public void Emit(
        string op,
        string phase,
        long durationMs,
        string? documentKey = null,
        int chars = -1,
        int lines = -1,
        int changedChars = -1,
        string? meta = null)
    {
        if (!Enabled)
            return;

        if (chars < 0)
            chars = _lastChars;
        if (lines < 0)
            lines = _lastLines;
        if (changedChars < 0)
            changedChars = _lastChangedChars;
        documentKey ??= _lastDocumentKey;

        bool slow = durationMs >= SlowBudgetMs;
        EmitRaw(new SpanRecord(
            Op: op,
            Phase: phase,
            DurationMs: durationMs,
            Slow: slow,
            Chars: chars,
            Lines: lines,
            ChangedChars: changedChars,
            Meta: meta,
            DocumentKey: documentKey));

        if (string.Equals(phase, "end", StringComparison.Ordinal))
            RecordStat(op, durationMs);
    }

    public void WriteSessionSummary()
    {
        if (!Enabled || _writer is null)
            return;

        lock (_writeLock)
        {
            foreach (var pair in _stats.OrderByDescending(p => p.Value.MaxMs))
            {
                OpStats s = pair.Value;
                double p95 = Percentile(s.Samples, 0.95);
                double median = Percentile(s.Samples, 0.50);
                WriteLineUnlocked(
                    $"{{\"ts\":{UnixMs()},\"op\":\"session_summary\",\"phase\":\"end\",\"opName\":{Json(pair.Key)},\"count\":{s.Count},\"sumMs\":{s.SumMs.ToString(CultureInfo.InvariantCulture)},\"maxMs\":{s.MaxMs.ToString(CultureInfo.InvariantCulture)},\"medianMs\":{median.ToString("0.###", CultureInfo.InvariantCulture)},\"p95Ms\":{p95.ToString("0.###", CultureInfo.InvariantCulture)},\"slowCount\":{s.SlowCount}}}");
            }

            _writer.Flush();
        }
    }

    private void EmitRaw(SpanRecord record)
    {
        if (_writer is null)
            return;

        int threadId = Environment.CurrentManagedThreadId;
        bool isUi = _uiThreadId < 0 || threadId == _uiThreadId;
        var sb = new StringBuilder(256);
        sb.Append("{\"ts\":").Append(UnixMs());
        sb.Append(",\"op\":").Append(Json(record.Op));
        sb.Append(",\"phase\":").Append(Json(record.Phase));
        sb.Append(",\"durationMs\":").Append(record.DurationMs.ToString(CultureInfo.InvariantCulture));
        sb.Append(",\"slow\":").Append(record.Slow ? "true" : "false");
        sb.Append(",\"chars\":").Append(record.Chars);
        sb.Append(",\"lines\":").Append(record.Lines);
        sb.Append(",\"changedChars\":").Append(record.ChangedChars);
        sb.Append(",\"threadId\":").Append(threadId);
        sb.Append(",\"isUiThread\":").Append(isUi ? "true" : "false");
        if (!string.IsNullOrEmpty(record.DocumentKey))
            sb.Append(",\"documentKey\":").Append(Json(record.DocumentKey));
        if (!string.IsNullOrEmpty(record.Meta))
            sb.Append(",\"meta\":").Append(Json(record.Meta));
        sb.Append('}');

        lock (_writeLock)
        {
            WriteLineUnlocked(sb.ToString());
            if (Interlocked.Increment(ref _pendingFlush) >= FlushEvery)
            {
                _pendingFlush = 0;
                _writer.Flush();
            }
        }

        if (record.Slow || record.DurationMs >= 8)
        {
            Trace.WriteLine(
                $"[SqlTypingPerf] op={record.Op} phase={record.Phase} durationMs={record.DurationMs} slow={(record.Slow ? "true" : "false")} chars={record.Chars} lines={record.Lines}"
                + (string.IsNullOrEmpty(record.Meta) ? string.Empty : " meta=" + record.Meta));
        }
    }

    private void WriteLineUnlocked(string line)
    {
        _writer!.WriteLine(line);
    }

    private void RecordStat(string op, long durationMs)
    {
        OpStats stats = _stats.GetOrAdd(op, static _ => new OpStats());
        lock (stats)
        {
            stats.Count++;
            stats.SumMs += durationMs;
            if (durationMs > stats.MaxMs)
                stats.MaxMs = durationMs;
            if (durationMs >= SlowBudgetMs)
                stats.SlowCount++;
            if (stats.Samples.Count < 4096)
                stats.Samples.Add(durationMs);
        }
    }

    private static long UnixMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string Json(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            + "\"";
    }

    private static double Percentile(List<long> samples, double p)
    {
        if (samples.Count == 0)
            return 0;
        var sorted = samples.OrderBy(x => x).ToArray();
        double idx = (sorted.Length - 1) * p;
        int lo = (int)Math.Floor(idx);
        int hi = (int)Math.Ceiling(idx);
        if (lo == hi)
            return sorted[lo];
        double w = idx - lo;
        return sorted[lo] * (1 - w) + sorted[hi] * w;
    }

    private readonly record struct SpanRecord(
        string Op,
        string Phase,
        long DurationMs,
        bool Slow,
        int Chars,
        int Lines,
        int ChangedChars,
        string? Meta,
        string? DocumentKey);

    private sealed class OpStats
    {
        public int Count;
        public long SumMs;
        public long MaxMs;
        public int SlowCount;
        public List<long> Samples { get; } = new();
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }

    private sealed class MeasureScope : IDisposable
    {
        private readonly SqlTypingPerfProbe _probe;
        private readonly string _op;
        private readonly string _phase;
        private readonly string? _documentKey;
        private readonly int _chars;
        private readonly int _lines;
        private readonly int _changedChars;
        private readonly string? _meta;
        private readonly long _started;
        private int _disposed;

        public MeasureScope(
            SqlTypingPerfProbe probe,
            string op,
            string phase,
            string? documentKey,
            int chars,
            int lines,
            int changedChars,
            string? meta)
        {
            _probe = probe;
            _op = op;
            _phase = phase;
            _documentKey = documentKey;
            _chars = chars;
            _lines = lines;
            _changedChars = changedChars;
            _meta = meta;
            _started = Environment.TickCount64;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            _probe.Emit(
                _op,
                _phase,
                Environment.TickCount64 - _started,
                _documentKey,
                _chars,
                _lines,
                _changedChars,
                _meta);
        }
    }
}
