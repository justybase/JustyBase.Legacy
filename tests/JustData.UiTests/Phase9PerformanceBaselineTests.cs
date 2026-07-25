using FlaUI.Core.AutomationElements;
using System.Diagnostics;

namespace JustData.UiTests;

public sealed class Phase9PerformanceBaselineTests
{
    private const string MainWindowId = "NetezzaSQL_addedFastColored";

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "Baseline")]
    public void Measure_startup_time_to_main_window()
    {
        var sw = Stopwatch.StartNew();
        using var session = UiTestHelpers.LaunchAndLogin();
        sw.Stop();

        long startupMs = sw.ElapsedMilliseconds;
        Assert.True(startupMs < 30_000, $"Startup took {startupMs} ms, limit is 30 s");
        File.AppendAllText(GetResultsPath(), $"Startup,1,{startupMs},,\n");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "Baseline")]
    public void Measure_file_search_baseline()
    {
        string searchDir = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.Perf",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(searchDir);
        try
        {
            for (int i = 0; i < 10_000; i++)
                File.WriteAllText(Path.Combine(searchDir, $"file{i:D5}.sql"), $"-- test file {i}");

            var sw = Stopwatch.StartNew();
            using var session = UiTestHelpers.LaunchAndLogin();
            sw.Stop();

            long startupMs = sw.ElapsedMilliseconds;
            AutomationElement mainWindow = session.MainWindow;
            AutomationElement? tree = mainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("filesTreeView"));
            Assert.NotNull(tree);

            sw.Restart();
            var filesPanel = mainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("textBoxFileSearch"));
            if (filesPanel is not null)
            {
                filesPanel.Focus();
                filesPanel.AsTextBox().Text = "file0";
                Thread.Sleep(2000);
            }
            sw.Stop();

            long searchMs = sw.ElapsedMilliseconds;
            File.AppendAllText(GetResultsPath(), $"FileSearch,1,{searchMs},{searchDir},\n");
        }
        finally
        {
            if (Directory.Exists(searchDir))
                Directory.Delete(searchDir, recursive: true);
        }
    }

    private static string GetResultsPath()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "baseline");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "Phase9Baseline.csv");
    }
}
