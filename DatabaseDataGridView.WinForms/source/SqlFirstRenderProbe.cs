using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace DatabaseDataGridView.WinForms;

/// <summary>
/// Optional test-only channel for measuring the first paint of a SQL result grid.
/// It is entirely inactive unless the launched process receives the pipe name.
/// </summary>
public sealed class SqlFirstRenderProbeRun
{
    internal const string PipeEnvironmentVariable = "JUSTYBASE_FIRST_RENDER_PIPE";

    private static readonly string? PipeName = Environment.GetEnvironmentVariable(PipeEnvironmentVariable);
    private readonly long _startedTimestamp;
    private int _reported;

    private SqlFirstRenderProbeRun(long startedTimestamp)
    {
        _startedTimestamp = startedTimestamp;
        RunId = Guid.NewGuid().ToString("N");
    }

    public string RunId { get; }

    /// <summary>Creates a run only for a process explicitly launched with the test probe enabled.</summary>
    public static SqlFirstRenderProbeRun? StartSqlExecution() =>
        string.IsNullOrWhiteSpace(PipeName) ? null : new SqlFirstRenderProbeRun(Stopwatch.GetTimestamp());

    internal void ReportFirstPaint(int columnCount)
    {
        if (Interlocked.Exchange(ref _reported, 1) != 0)
        {
            return;
        }

        long elapsedMilliseconds = (long)Stopwatch.GetElapsedTime(_startedTimestamp).TotalMilliseconds;
        string pipeName = PipeName!;
        var data = new FirstRenderProbeData(RunId, columnCount, elapsedMilliseconds);
        string message = JsonSerializer.Serialize(data, FirstRenderProbeJsonContext.Default.FirstRenderProbeData);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(2_000);
                await using var writer = new StreamWriter(pipe) { AutoFlush = true };
                await writer.WriteLineAsync(message);
            }
            catch (IOException)
            {
                // The probe belongs exclusively to an optional UI test. Never affect the application.
            }
            catch (UnauthorizedAccessException)
            {
                // A malformed test environment must remain invisible to normal application behavior.
            }
        });
    }
}
