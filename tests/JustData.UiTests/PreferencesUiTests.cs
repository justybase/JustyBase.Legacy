using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
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

    [Fact]
    [Trait("Category", "UI")]
    public void Preferences_NumberFormatPresets_keep_individual_values_and_preview_current_selection()
    {
        Directory.CreateDirectory(_configDirectory);
        string configPath = Path.Combine(_configDirectory, "config.json");
        File.WriteAllText(configPath, "{\"IntegerFormat\":\"N0\",\"DecimalFormat\":\"N4\"}");
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

            var resultsNavigation = window.FindFirstDescendant(
                cf => cf.ByAutomationId("navigationtabResults"))?.AsButton();
            Assert.NotNull(resultsNavigation);
            resultsNavigation.InvokePattern.Invoke();

            var preset = UiTestHelpers.WaitFor(
                () => window.FindFirstDescendant(
                    cf => cf.ByAutomationId("numberFormatPresetComboBox"))?.AsComboBox(),
                "the number format preset");
            var integerFormat = UiTestHelpers.WaitFor(
                () => window.FindFirstDescendant(
                    cf => cf.ByAutomationId("cbIntFormat"))?.AsComboBox(),
                "the integer format");
            var decimalFormat = UiTestHelpers.WaitFor(
                () => window.FindFirstDescendant(
                    cf => cf.ByAutomationId("cbDecimalFormat"))?.AsComboBox(),
                "the decimal format");
            var previewButton = UiTestHelpers.WaitFor(
                () => window.FindFirstDescendant(
                    cf => cf.ByAutomationId("numberFormatPreviewButton"))?.AsButton(),
                "the number format preview button");
            var save = UiTestHelpers.WaitFor(
                () => window.FindFirstDescendant(cf => cf.ByAutomationId("btSave2"))?.AsButton(),
                "the preferences save button");

            preset.Focus();
            Keyboard.Press(VirtualKeyShort.HOME);
            Keyboard.Press(VirtualKeyShort.DOWN);
            preset.Collapse();
            Assert.Equal("N0", integerFormat.EditableText);
            Assert.Equal("G", decimalFormat.EditableText);

            previewButton.Click();
            Thread.Sleep(100);

            var preview = UiTestHelpers.WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(candidate => candidate.AutomationId == "numberFormatPreviewDialog"
                        || candidate.FindFirstDescendant(
                            cf => cf.ByAutomationId("numberFormatPreviewDialog")) is not null)
                ?? automation.GetDesktop().FindFirstDescendant(
                    cf => cf.ByName("Number format preview")),
                "the number format preview dialog");
            Assert.Contains("N0", preview.FindFirstDescendant(
                cf => cf.ByAutomationId("integerFormatPreviewFormat"))?.Name ?? string.Empty);
            Assert.Contains("G", preview.FindFirstDescendant(
                cf => cf.ByAutomationId("decimalFormatPreviewFormat"))?.Name ?? string.Empty);
            Assert.Contains("→", preview.FindFirstDescendant(
                cf => cf.ByAutomationId("decimalFormatPreviewValue"))?.Name ?? string.Empty);

            preview.FindFirstDescendant(
                cf => cf.ByAutomationId("closeNumberFormatPreviewButton"))?.AsButton().InvokePattern.Invoke();

            decimalFormat.EditableText = "0.00";
            integerFormat.EditableText = "D8";

            save.InvokePattern.Invoke();
            using Process process = Process.GetProcessById(application.ProcessId);
            Assert.True(process.WaitForExit(20_000), "The preferences test process did not exit.");
            completed = true;

            using JsonDocument config = JsonDocument.Parse(File.ReadAllText(configPath));
            Assert.Equal("D8", config.RootElement.GetProperty("IntegerFormat").GetString());
            Assert.Equal("0.00", config.RootElement.GetProperty("DecimalFormat").GetString());
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

    public void Dispose()
    {
        if (Directory.Exists(_configDirectory))
        {
            Directory.Delete(_configDirectory, recursive: true);
        }
    }
}
