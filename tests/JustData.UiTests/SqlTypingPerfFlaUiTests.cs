using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace JustData.UiTests;

/// <summary>
/// BIG.SQL at end of document: post-login idle → click editor → Ctrl+End → 10× 'X' every 50 ms.
/// </summary>
public sealed class SqlTypingPerfFlaUiTests
{
    private const string PerfEnv = "JUSTYBASE_SQL_TYPING_PERF";
    private const int KeyCount = 10;
    private const int IntervalMs = 50;
    private const int ExpectedBudgetMs = KeyCount * IntervalMs;
    private const int MinLagEvidenceMs = 1_500;
    private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(120);

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Performance")]
    [Trait("Category", "TypingPerf")]
    public void BigSql_EndOfDocument_TenXEvery50Ms_TakesManySeconds()
    {
        UiTestHelpers.EnsureTestoweProfile();
        try { File.Delete(Path.Combine(Path.GetTempPath(), "justybase-flaui-steps.log")); } catch { }

        string perfDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JustyBase",
            "perf");
        Directory.CreateDirectory(perfDir);
        DateTime startedUtc = DateTime.UtcNow;

        string? logPath;
        long wallMs;
        long[] interKeyMs = new long[KeyCount];
        string typedTail;

        using (UiSession session = UiTestHelpers.LaunchAndLogin(
                   environment: new Dictionary<string, string> { [PerfEnv] = "1" }))
        {
            UiTestHelpers.FlaUiStep("logged in");
            Thread.Sleep(2_000);

            Window main = UiTestHelpers.WaitFor(
                () => UiTestHelpers.TryFindMainWindow(session.Application, session.Automation),
                "main window",
                timeout: LoadTimeout);

            UiTestHelpers.WaitForPostLoginIdle(main);
            UiTestHelpers.BringSessionToForeground(session);
            Thread.Sleep(2_000);

            UiTestHelpers.FocusSqlEditorAtDocumentEnd(main, session);

            UiTestHelpers.FlaUiStep("smoke key Q");
            Keyboard.Type("Q");
            Thread.Sleep(500);

            UiTestHelpers.FocusSqlEditorAtDocumentEnd(main, session);

            UiTestHelpers.FlaUiStep("typing burst X");
            wallMs = TypeXEveryInterval(KeyCount, IntervalMs, interKeyMs);
            Thread.Sleep(500);

            typedTail = ReadEditorTail(session, KeyCount);
            logPath = WaitForNewSpanLog(perfDir, startedUtc, TimeSpan.FromSeconds(10));
        }

        Thread.Sleep(800);

        string reportPath = Path.Combine(perfDir, $"typing-end-x-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        var report = new System.Text.StringBuilder();
        report.AppendLine("scenario=post-login idle + click lower editor + Ctrl+End + 10x X @ 50ms");
        report.AppendLine($"wallMs={wallMs}");
        report.AppendLine($"interKeyMs=[{string.Join(",", interKeyMs)}]");
        report.AppendLine($"typedTail='{typedTail}'");
        report.AppendLine($"stepLog={Path.Combine(Path.GetTempPath(), "justybase-flaui-steps.log")}");
        report.AppendLine($"log={logPath ?? "(none)"}");

        if (!string.IsNullOrWhiteSpace(logPath) && File.Exists(logPath))
            report.AppendLine(SpanRanking.FromFile(logPath).FormatReport(logPath));

        File.WriteAllText(reportPath, report.ToString());

        Assert.Equal(new string('X', KeyCount), typedTail);
        Assert.True(wallMs >= MinLagEvidenceMs,
            $"wallMs={wallMs} expected >= {MinLagEvidenceMs}. Report: {reportPath}");
        Assert.True(wallMs > ExpectedBudgetMs * 2,
            $"wallMs={wallMs} expected noticeably above {ExpectedBudgetMs * 2}ms idle budget. Report: {reportPath}");
    }

    private static string ReadEditorTail(UiSession session, int charCount)
    {
        Window main = UiTestHelpers.WaitFor(
            () => UiTestHelpers.TryFindMainWindow(session.Application, session.Automation),
            "main window",
            timeout: LoadTimeout);
        UiTestHelpers.FocusSqlEditorAtDocumentEnd(main, session);
        AutomationElement editor = UiTestHelpers.FindSqlEditor(main);
        string full = UiTestHelpers.CopySqlEditorText(editor);
        if (full.Length <= charCount)
            return full;
        return full[^charCount..];
    }

    private static long TypeXEveryInterval(int count, int intervalMs, long[] interKeyMs)
    {
        var total = Stopwatch.StartNew();
        long previous = Stopwatch.GetTimestamp();
        for (int i = 0; i < count; i++)
        {
            Keyboard.Type("X");
            Thread.Sleep(intervalMs);

            long now = Stopwatch.GetTimestamp();
            interKeyMs[i] = (long)Stopwatch.GetElapsedTime(previous).TotalMilliseconds;
            previous = now;
            UiTestHelpers.FlaUiStep($"typed X #{i + 1}, interKeyMs={interKeyMs[i]}");
        }

        total.Stop();
        return total.ElapsedMilliseconds;
    }

    private static string? WaitForNewSpanLog(string perfDir, DateTime startedUtc, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            string? newest = Directory.GetFiles(perfDir, "sql-typing-spans-*.ndjson")
                .Select(path => new FileInfo(path))
                .Where(info => info.LastWriteTimeUtc >= startedUtc.AddSeconds(-5))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .Select(info => info.FullName)
                .FirstOrDefault();
            if (newest is not null && new FileInfo(newest).Length > 50)
                return newest;

            Thread.Sleep(250);
        }

        return null;
    }

    private sealed class SpanRanking
    {
        public static SpanRanking FromFile(string path)
        {
            var samples = new Dictionary<string, List<double>>(StringComparer.Ordinal);
            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                try
                {
                    var root = JsonSerializer.Deserialize<JsonElement>(line);
                    if (!root.TryGetProperty("op", out var opEl)
                        || !root.TryGetProperty("phase", out var phaseEl)
                        || phaseEl.GetString() != "end"
                        || !root.TryGetProperty("durationMs", out var durEl))
                        continue;
                    string? op = opEl.GetString();
                    if (string.IsNullOrEmpty(op) || op is "session" or "session_summary")
                        continue;
                    if (!samples.TryGetValue(op, out var list))
                    {
                        list = new List<double>();
                        samples[op] = list;
                    }

                    list.Add(durEl.GetDouble());
                }
                catch (JsonException)
                {
                }
            }

            return new SpanRanking();
        }

        public string FormatReport(string logPath) => "log=" + logPath;
    }
}
