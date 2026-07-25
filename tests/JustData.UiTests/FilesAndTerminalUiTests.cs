using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.Diagnostics;

namespace JustData.UiTests;

public sealed class FilesAndTerminalUiTests
{
    [Fact]
    [Trait("Category", "UI")]
    public void Files_and_variables_keep_their_automation_ids_and_terminal_is_absent()
    {
        UiTestHelpers.KillExistingInstances();
        using FlaUI.Core.Application application = FlaUI.Core.Application.Launch(new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "JustyBaseLegacy.exe"),
            UseShellExecute = false
        });
        using UIA3Automation automation = new();
        using Process process = Process.GetProcessById(application.ProcessId);

        try
        {
            Window loginWindow = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("connectionSelectorComboBox")) is not null),
                "the Login window");

            WaitFor(
                () => loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("selectDatabaseButton"))?.AsButton(),
                "the Save & Select button").Invoke();

            Window mainWindow = WaitFor(
                () => application.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(window => window.FindFirstDescendant(
                        cf => cf.ByAutomationId("NetezzaSQL_addedFastColored")) is not null),
                "the main JustData window");

            Assert.NotNull(mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("dgvVariables")));
            Assert.NotNull(mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("filesTreeView")));
            Assert.Null(mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("terminalPanelControl")));
            Assert.Null(mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("terminalToolStripMenuItem")));
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
        }
    }

    private static T WaitFor<T>(Func<T?> action, string description) where T : class
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            T? result = action();
            if (result is not null)
                return result;
            Thread.Sleep(100);
        }

        throw new TimeoutException($"Timed out waiting for {description}.");
    }
}
