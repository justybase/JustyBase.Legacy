using System.Diagnostics;
using JustyBase.Core.Diagnostics;

namespace FastColoredTextBoxNS.Helpers;

/// <summary>
/// Lightweight FCTB-local timing helper. When <see cref="SqlTypingPerfProbe"/> is enabled,
/// measurements are also written to the NDJSON span log.
/// </summary>
public static class SqlTypingPerfLocal
{
    public const int HighlightBudgetMs = 50;
    public const int CommentScanBudgetMs = 50;

    public static bool Enabled { get; set; }

    public static void Phase(string name, long elapsedMs, int chars, int lines, string? extra = null)
    {
        if (!Enabled)
            return;

        string op = name.StartsWith("editor.", StringComparison.Ordinal) ? name : "editor." + name;
        if (SqlTypingPerfProbe.Instance.Enabled)
        {
            SqlTypingPerfProbe.Instance.Emit(op, "end", elapsedMs, chars: chars, lines: lines, meta: extra);
            return;
        }

        bool slow = elapsedMs >= HighlightBudgetMs;
        if (!slow && elapsedMs < 8)
            return;

        string msg = $"[SqlTypingPerf] op={op} phase=end durationMs={elapsedMs} slow={(slow ? "true" : "false")} chars={chars} lines={lines}";
        if (!string.IsNullOrEmpty(extra))
            msg += " meta=" + extra;
        Trace.WriteLine(msg);
    }

    public static IDisposable Measure(string name, int chars, int lines, string? extra = null)
    {
        if (!Enabled && !SqlTypingPerfProbe.Instance.Enabled)
            return Noop.Instance;

        string op = name.StartsWith("editor.", StringComparison.Ordinal) ? name : "editor." + name;
        if (SqlTypingPerfProbe.Instance.Enabled)
            return SqlTypingPerfProbe.Instance.Measure(op, "end", chars: chars, lines: lines, meta: extra);

        return new Scope(name, chars, lines, extra);
    }

    private sealed class Noop : IDisposable
    {
        public static readonly Noop Instance = new();
        public void Dispose() { }
    }

    private sealed class Scope : IDisposable
    {
        private readonly string _name;
        private readonly int _chars;
        private readonly int _lines;
        private readonly string? _extra;
        private readonly long _started;

        public Scope(string name, int chars, int lines, string? extra)
        {
            _name = name;
            _chars = chars;
            _lines = lines;
            _extra = extra;
            _started = Environment.TickCount64;
        }

        public void Dispose()
        {
            Phase(_name, Environment.TickCount64 - _started, _chars, _lines, _extra);
        }
    }
}
