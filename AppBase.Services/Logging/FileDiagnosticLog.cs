namespace AppBase.Services;

/// <summary>
/// Append-only diagnostic log under %LOCALAPPDATA%\JustyBaseLegacy\logs.
/// User-facing dialogs stay on <see cref="LoggerLoud"/>; this is for support and auditing.
/// </summary>
public static class FileDiagnosticLog
{
    public const long MaxBytesPerFile = 5 * 1024 * 1024;

    private static readonly object Gate = new();

    /// <summary>Used by unit tests to redirect output away from LocalAppData.</summary>
    internal static string? LogDirectoryOverrideForTests { get; set; }

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JustyBaseLegacy",
        "logs");

    private static string ActiveLogDirectory => LogDirectoryOverrideForTests ?? LogDirectory;

    public static void Write(DiagnosticLogLevel level, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string safe = SensitiveDataRedactor.Redact(message);
        string line = $"{DateTimeOffset.UtcNow:O} [{level}] {safe}{Environment.NewLine}";

        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(ActiveLogDirectory);
                string path = Path.Combine(ActiveLogDirectory, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
                RotateIfNeeded(path);
                File.AppendAllText(path, line);
            }
            catch
            {
                // Logging must never take down the UI.
            }
        }
    }

    public static void WriteError(string message, Exception? exception = null)
    {
        string combined = exception is null
            ? message
            : $"{message}{Environment.NewLine}{SensitiveDataRedactor.RedactException(exception)}";
        Write(DiagnosticLogLevel.Error, combined);
    }

    internal static void RotateIfNeeded(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var info = new FileInfo(path);
        if (info.Length < MaxBytesPerFile)
        {
            return;
        }

        string rolled = path + ".1";
        if (File.Exists(rolled))
        {
            File.Delete(rolled);
        }

        File.Move(path, rolled);
    }
}

public enum DiagnosticLogLevel
{
    Info,
    Warn,
    Error,
}
