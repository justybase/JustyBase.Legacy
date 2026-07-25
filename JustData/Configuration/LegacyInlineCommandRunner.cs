using AppBase.Common;
using AppBase.Common.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI.Configuration;

/// <summary>
/// Runs the legacy external-command syntax used by SQL execution.
/// </summary>
public sealed class LegacyInlineCommandRunner : IInlineCommandRunner
{
    public Color LogErrorStdColor { get; set; } = Color.Empty;

    public async Task DoInlineCommandAsync(
        string connectionString,
        string runCommand,
        ISqlExecutionLog log,
        Stopwatch stopwatch,
        CancellationToken cancellationToken = default)
    {
        Match match = InlineCommandPattern.Regex().Match(runCommand);
        if (!match.Success)
        {
            return;
        }

        string programPath = match.Groups["programPath"].Value.Trim();
        string arguments = match.Groups["arguments"].Value.Trim();
        if (arguments.Contains("#myCredentials", StringComparison.Ordinal))
        {
            arguments = arguments.Replace("#myCredentials", $"\"{connectionString}\"", StringComparison.Ordinal);
        }

        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = programPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    void WriteError()
                    {
                        log?.AppendErrorEntry(DateTime.Now, stopwatch.Elapsed.TotalSeconds.ToString("F1"), "script error", e.Data);
                        if (log?.View.Parent is ISuccesfullTab successfulTab)
                        {
                            successfulTab.IsRunning = false;
                            successfulTab.IsSuccess = false;
                        }
                    }

                    if (log?.View is { InvokeRequired: true } view)
                        view.Invoke(WriteError);
                    else
                        WriteError();
                }
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    log?.AppendEntry(DateTime.Now, stopwatch.Elapsed.TotalSeconds.ToString("F1"), "script output", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

}
