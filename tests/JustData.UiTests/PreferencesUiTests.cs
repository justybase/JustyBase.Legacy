using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Text.Json;

namespace JustData.UiTests;

public sealed class PreferencesUiTests : IDisposable
{
    private readonly string _configDirectory = Path.Combine(Path.GetTempPath(), "JustyBaseLegacy.UiTests", Guid.NewGuid().ToString("N"));

    [Fact]
    [Trait("Category", "StartupCharacterization")]
    public void Legacy_smoke_startup_builds_container_and_exits_cleanly()
    {
        UiTestHelpers.KillExistingInstances();
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            Arguments = "--smoke-test",
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("The smoke-test process could not be started.");
        Assert.True(process.WaitForExit(30_000), "The smoke-test process did not exit.");
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Preferences_ChangingCsvSeparator_PersistsConfiguration()
    {
        Directory.CreateDirectory(_configDirectory);
        File.WriteAllText(Path.Combine(_configDirectory, "config.json"), "{\"SepInExportedCsv\":\";\"}");
        UiTestHelpers.KillExistingInstances();
        using FlaUI.Core.Application application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            Arguments = $"--ui-test-preferences \"{_configDirectory}\"",
            UseShellExecute = false
        });
        using UIA3Automation automation = new();
        bool completed = false;

        try
        {
            var window = application.GetMainWindow(automation, TimeSpan.FromSeconds(20));
            Assert.NotNull(window);
            Assert.Equal("JustyBase Settings", window.Title);

            var separator = window.FindFirstDescendant(cf => cf.ByAutomationId("tbCSVSep"))?.AsTextBox();
            Assert.NotNull(separator);
            separator.Text = "|";

            var save = window.FindFirstDescendant(cf => cf.ByAutomationId("btSave2"))?.AsButton();
            Assert.NotNull(save);
            save.InvokePattern.Invoke();
            using Process process = Process.GetProcessById(application.ProcessId);
            Assert.True(process.WaitForExit(20_000), "The preferences test process did not exit.");
            completed = true;

            using JsonDocument config = JsonDocument.Parse(File.ReadAllText(Path.Combine(_configDirectory, "config.json")));
            Assert.Equal("|", config.RootElement.GetProperty("SepInExportedCsv").GetString());
        }
        finally
        {
            if (!completed)
            {
                try
                {
                    application.Kill();
                }
                catch (InvalidOperationException)
                {
                    // The child process can already have terminated while a UIA call failed.
                }
            }
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Preferences_Cancel_keeps_configuration_and_automation_ids_stable()
    {
        Directory.CreateDirectory(_configDirectory);
        string configPath = Path.Combine(_configDirectory, "config.json");
        File.WriteAllText(configPath, "{\"SepInExportedCsv\":\";\"}");
        byte[] before = File.ReadAllBytes(configPath);
        using FlaUI.Core.Application application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            Arguments = $"--ui-test-preferences \"{_configDirectory}\"",
            UseShellExecute = false
        });
        using UIA3Automation automation = new();
        bool completed = false;

        try
        {
            var window = application.GetMainWindow(automation, TimeSpan.FromSeconds(20));
            Assert.NotNull(window);
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByAutomationId("modernPreferencesRoot")));
            var separator = window.FindFirstDescendant(cf => cf.ByAutomationId("tbCSVSep"))?.AsTextBox();
            Assert.NotNull(separator);
            separator.Text = "|";

            var cancel = window.FindFirstDescendant(cf => cf.ByAutomationId("cancelPreferencesButton"))?.AsButton();
            Assert.NotNull(cancel);
            cancel.InvokePattern.Invoke();
            using Process process = Process.GetProcessById(application.ProcessId);
            Assert.True(process.WaitForExit(20_000), "The preferences test process did not exit.");
            completed = true;
            Assert.Equal(before, File.ReadAllBytes(configPath));
        }
        finally
        {
            if (!completed)
            {
                try { application.Kill(); }
                catch (InvalidOperationException) { }
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_configDirectory))
        {
            Directory.Delete(_configDirectory, recursive: true);
        }
    }
}
