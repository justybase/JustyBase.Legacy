using AppBase.Services;

namespace AppBase.Tests.Logging;

public sealed class FileDiagnosticLogTests : IDisposable
{
    private readonly string _tempRoot;

    public FileDiagnosticLogTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        FileDiagnosticLog.LogDirectoryOverrideForTests = _tempRoot;
    }

    public void Dispose()
    {
        FileDiagnosticLog.LogDirectoryOverrideForTests = null;
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void Write_appends_redacted_line_to_daily_log()
    {
        FileDiagnosticLog.Write(DiagnosticLogLevel.Info, "hello password=Secret");

        string path = Path.Combine(_tempRoot, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
        Assert.True(File.Exists(path));
        string text = File.ReadAllText(path);
        Assert.Contains("[Info] hello", text);
        Assert.DoesNotContain("Secret", text);
    }

    [Fact]
    public void RotateIfNeeded_rolls_file_when_max_size_reached()
    {
        string path = Path.Combine(_tempRoot, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
        File.WriteAllText(path, new string('x', (int)FileDiagnosticLog.MaxBytesPerFile));

        FileDiagnosticLog.RotateIfNeeded(path);

        Assert.True(File.Exists(path + ".1"));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void WriteError_does_not_throw()
    {
        var ex = new InvalidOperationException("password=Secret123");
        FileDiagnosticLog.WriteError("connect password=abc", ex);
    }
}
